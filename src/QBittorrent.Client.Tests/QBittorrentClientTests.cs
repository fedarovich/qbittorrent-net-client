using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace QBittorrent.Client.Tests
{
    [Collection(DockerCollection.Name)]
    public class QBittorrentClientTests : IAsyncLifetime, IDisposable
    {
        private string ContainerId { get; set; }

        private DockerFixture DockerFixture { get; }

        private QBittorrentClient Client { get; }

        #region Lifetime

        public QBittorrentClientTests(DockerFixture dockerFixture)
        {
            DockerFixture = dockerFixture;
            Client = new QBittorrentClient(new Uri("http://localhost:8080"));
        }

        public void Dispose()
        {
            Client?.Dispose();
        }

        public async Task InitializeAsync()
        {
            var createContainerParameters = new CreateContainerParameters
            {
                Image = DockerFixture.ImageName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    ["8080/tcp"] = new EmptyStruct()
                },
            };

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                createContainerParameters.HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        ["8080/tcp"] = new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = "0.0.0.0",
                                HostPort = "8080"
                            }
                        }
                    }
                };
            }

            Console.WriteLine("\tCreating container from image...");
            var result = await DockerFixture.Client.Containers.CreateContainerAsync(
                createContainerParameters);
            ContainerId = result.ID;
            Assert.False(string.IsNullOrEmpty(ContainerId), "string.IsNullOrEmpty(ContainerId)");
            Console.WriteLine($"\tCreated container {ContainerId}.");
            
            Console.WriteLine($"\tStarting container {ContainerId}...");
            var started = await DockerFixture.Client.Containers.StartContainerAsync(ContainerId,
                new ContainerStartParameters());
            Assert.True(started, "started");
            Console.WriteLine($"\tStarted container {ContainerId}.");
        }

        public async Task DisposeAsync()
        {
            Console.WriteLine($"\tStopping container {ContainerId}...");
            await DockerFixture.Client.Containers.StopContainerAsync(ContainerId,
                new ContainerStopParameters {WaitBeforeKillSeconds = 10u});
            Console.WriteLine($"\tDeleting container {ContainerId}...");
            await DockerFixture.Client.Containers.RemoveContainerAsync(ContainerId,
                new ContainerRemoveParameters {Force = true});
        }

        #endregion

        #region Login/Logout
        
        [Fact]
        public async Task LoginCorrect()
        {
            await Client.LoginAsync("admin", "adminadmin");
            var list = await Client.GetTorrentListAsync();
            Assert.Empty(list);
        }
        
        [Fact]
        public async Task LoginIncorrect()
        {
            await Client.LoginAsync("admin", "incorrect");
            await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
        }
        
        [Fact]
        public async Task NoLogin()
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
        }

        [Fact]
        public async Task Logout()
        {
            await Client.LoginAsync("admin", "adminadmin");
            var list = await Client.GetTorrentListAsync();
            Assert.Empty(list);
            await Client.LogoutAsync();
            await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
        }
        
        #endregion
        
        #region Add Torrent

        [Fact]
        public async Task AddTorrentsFromFiles()
        {
            await Client.LoginAsync("admin", "adminadmin");
            var list = await Client.GetTorrentListAsync();
            Assert.Empty(list);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var addRequest = new AddTorrentFilesRequest(filesToAdd);
            await Client.AddTorrentsAsync(addRequest);

            list = await Client.GetTorrentListAsync();
            Assert.Equal(2, list.Count);
        }
        
        [Fact]
        public async Task AddTorrentsFromMagnetLinks()
        {
            await Client.LoginAsync("admin", "adminadmin");
            var list = await Client.GetTorrentListAsync();
            Assert.Empty(list);

            var parser = new BencodeParser();
            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var magnets = filesToAdd
                .Select(path => parser.Parse<Torrent>(path).GetMagnetLink())
                .Select(marnet => new Uri(marnet));
     
            var addRequest = new AddTorrentUrlsRequest(magnets);
            await Client.AddTorrentsAsync(addRequest);

            list = await Client.GetTorrentListAsync();
            Assert.Equal(2, list.Count);
        }
        
        [Fact]
        public async Task AddTorrentsFromHttpLinks()
        {
            await Client.LoginAsync("admin", "adminadmin");
            var list = await Client.GetTorrentListAsync();
            Assert.Empty(list);

            var links = new Uri[]
            {
                new Uri(
                    "http://releases.ubuntu.com/17.10/ubuntu-17.10.1-desktop-amd64.iso.torrent?_ga=2.234486046.1639235846.1523865053-1922367372.1523865053"),
                new Uri(
                    "http://releases.ubuntu.com/16.04/ubuntu-16.04.4-desktop-amd64.iso.torrent?_ga=2.234486046.1639235846.1523865053-1922367372.1523865053"),
            };
            var addRequest = new AddTorrentUrlsRequest(links);
            await Client.AddTorrentsAsync(addRequest);

            await Task.Delay(5000);
            
            list = await Client.GetTorrentListAsync();
            Assert.Equal(2, list.Count);
        }

        #endregion
    }
}
