using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Xunit;

namespace QBittorrent.Client.Tests
{
    public class DockerFixture : IAsyncLifetime
    {
        public string ImageName { get; private set; }

        public DockerClient Client { get; private set;}
        
        public Env Env { get; private set; }

        public async Task InitializeAsync()
        {
            ImageName = Environment.GetEnvironmentVariable("QBT_IMAGE") ?? DefaultImageName;
            var os = Environment.GetEnvironmentVariable("QBT_OS") ?? DefaulOS;

            var sourceDir = Path.Combine(Utils.StartupFolder, "docker", ImageName.Replace(':', '-'), os);
            var env = File.ReadAllText(Path.Combine(sourceDir, "env.json"));
            Console.WriteLine("Test Environment:");
            Console.WriteLine(env);
            Env = JsonConvert.DeserializeObject<Env>(env);
            await CopyRssFiles();
            await DownloadBinaries();
            
            var config = new DockerClientConfiguration(new Uri("http://localhost:2375"));
            Client = config.CreateClient();

            Console.WriteLine($"\tSearching docker image {ImageName}...");
            var images = await Client.Images.ListImagesAsync(
                new ImagesListParameters { MatchName = ImageName });
            if (!images.Any())
            {
                Console.WriteLine("\tImage not found.");
                Console.WriteLine($"\tCreating image {ImageName}");
                
                var fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tgz");
                
                Utils.CreateTarGz(fileName, sourceDir);

                Stream inputStream = null;
                try
                {
                    inputStream = File.OpenRead(fileName);
                    var progressStream = await Client.Images.BuildImageFromDockerfileAsync(
                        inputStream,
                        new ImageBuildParameters
                        {
                            Tags = new List<string> { ImageName },
                        });

                    using (var reader = new StreamReader(progressStream))
                    {
                        while (true)
                        {
                            var text = await reader.ReadLineAsync();
                            if (text == null)
                                break;

                            Console.WriteLine($"\t\t{text}");
                        }
                    }                   
                }
                finally
                {
                    Console.WriteLine($"\tFinished creating image {ImageName}.");
                    inputStream?.Dispose();
                    File.Delete(fileName);
                } 
            }

            async Task CopyRssFiles()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly
                    .GetManifestResourceNames()
                    .Where(n => Path.GetExtension(n) == ".rss")
                    .ToList();

                foreach (var resourceName in resourceNames)
                {
                    var parts = resourceName.Split('.');
                    var name = parts[parts.Length - 2] + ".rss";
                    var outputName = Path.Combine(sourceDir, name);
                    using (var inStream = assembly.GetManifestResourceStream(resourceName))
                    using (var outStream = File.Open(outputName, FileMode.Create, FileAccess.Write))
                    {
                        await inStream.CopyToAsync(outStream);
                    }
                }
            }
            
            async Task DownloadBinaries()
            {
                if (Env.Binaries?.Any() != true)
                    return;

                Console.WriteLine("Downloading binaries...");
                using (var httpClient = new HttpClient())
                {
                    foreach (var pair in Env.Binaries)
                    {
                        var filename = Path.Combine(sourceDir, pair.Key);
                        if (File.Exists(filename))
                        {
                            Console.WriteLine($"\t\tFile {pair.Key} already exists. Skipped downloading.");
                            continue;
                        }

                        var uri = string.IsNullOrEmpty(pair.Value)
                            ? new Uri($"https://fedarovich.blob.core.windows.net/qbittorrent-test/{ImageName.Replace(':', '-')}/{os}/{pair.Key}")
                            : new Uri(pair.Value);

                        Console.WriteLine($"\t\tDownloading {pair.Key} from {uri}...");
                        using (var inStream = await httpClient.GetStreamAsync(uri))
                        using (var outStream = File.OpenWrite(filename))
                        {
                            await inStream.CopyToAsync(outStream);
                        }
                        Console.WriteLine($"\t\tDownloaded {pair.Key} to {filename}.");
                    }
                }
            }
        }      

        public async Task DisposeAsync()
        {
            Console.WriteLine($"\t Deleting image {ImageName}...");
            await Client.Images.DeleteImageAsync(ImageName,
                new ImageDeleteParameters()
                {
                    PruneChildren = true,
                    Force = true
                });
        }

        private string DefaultImageName => "qbt:4.1.3";

        private string DefaulOS
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        return "win";
                    case PlatformID.Unix:
                        return "xenial";
                    default:
                        throw new NotSupportedException("The current OS is not supported.");
                }
            }
        }
    }
}

