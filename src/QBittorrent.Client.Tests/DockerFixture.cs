using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public async Task InitializeAsync()
        {
            ImageName = Environment.GetEnvironmentVariable("QBT_IMAGE") ?? DefaultImageName;
            var os = Environment.GetEnvironmentVariable("QBT_OS") ?? DefaulOS;

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
                var sourceDir = Path.Combine(Utils.StartupFolder, "docker", ImageName.Replace(':', '-'), os);
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

        private string DefaultImageName => "qbt:4.0.4";

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

