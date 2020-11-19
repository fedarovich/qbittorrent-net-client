using System;
using System.IO;
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
            var version = Environment.GetEnvironmentVariable("QBT_VERSION")?.Replace(':', '-') ?? "4.2.1";
            ImageName = "ghcr.io/fedarovich/qbt-net-test:" + version;
            var sourceDir = Path.Combine(Utils.StartupFolder, "docker", "qbt-" + version);
            var env = File.ReadAllText(Path.Combine(sourceDir, "env.json"));
            Console.WriteLine("Test Environment:");
            Console.WriteLine(env);
            Env = JsonConvert.DeserializeObject<Env>(env);

            var config = new DockerClientConfiguration(new Uri("http://localhost:2375"));
            Client = config.CreateClient();

            await Client.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = ImageName
                },
                new AuthConfig(),
                new Progress<JSONMessage>());
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

            Console.WriteLine("\t Clearing dangling volumes...");
            await Client.Volumes.PruneAsync(new VolumesPruneParameters());
        }
    }
}

