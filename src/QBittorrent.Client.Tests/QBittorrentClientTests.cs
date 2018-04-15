using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace QBittorrent.Client.Tests
{
    [Collection(DockerCollection.Name)]
    public class QBittorrentClientTests : IAsyncLifetime, IDisposable
    {
        public string ContainerId { get; set; }

        public DockerFixture DockerFixture { get; }

        public QBittorrentClient Client;

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

            var result = await DockerFixture.Client.Containers.CreateContainerAsync(
                createContainerParameters);
            ContainerId = result.ID;
            Assert.False(string.IsNullOrEmpty(ContainerId), "string.IsNullOrEmpty(ContainerId)");
            var started = await DockerFixture.Client.Containers.StartContainerAsync(ContainerId,
                new ContainerStartParameters());
            Assert.True(started, "started");
            await Task.Delay(10000);
        }

        public async Task DisposeAsync()
        {
            await DockerFixture.Client.Containers.StopContainerAsync(ContainerId,
                new ContainerStopParameters {WaitBeforeKillSeconds = 10u});
            await DockerFixture.Client.Containers.RemoveContainerAsync(ContainerId,
                new ContainerRemoveParameters {Force = true});
        }

        #endregion

        [Fact]
        public async Task GetApiVersion()
        {
            var version = await Client.GetApiVersionAsync();
            Assert.Equal(17, version);
        }
    }
}
