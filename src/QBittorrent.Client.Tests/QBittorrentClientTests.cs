﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Docker.DotNet.Models;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;

namespace QBittorrent.Client.Tests
{
    [Collection(DockerCollection.Name)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class QBittorrentClientTests : IAsyncLifetime, IDisposable
    {
        private const string UserName = "admin";
        private const string Password = "adminadmin";

        private string ContainerId { get; set; }

        private DockerFixture DockerFixture { get; }

        private QBittorrentClient Client { get; }

        private long? DefaultTorrentRateLimit => ApiVersionLessThan(2, 9) ? default(long?) : default(long);

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
                    ["8080/tcp"] = new EmptyStruct(),
                    ["9090/tcp"] = new EmptyStruct(),
                    ["6881/tcp"] = new EmptyStruct()
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        ["8080/tcp"] = new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = Utils.IsWindows ? null : "0.0.0.0",
                                HostPort = "8080"
                            }
                        },
                        ["9090/tcp"] = new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = Utils.IsWindows ? null : "0.0.0.0",
                                HostPort = "9090"
                            }
                        },
                        ["6881/tcp"] = new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = Utils.IsWindows ? null : "0.0.0.0",
                                HostPort = "6881"
                            }
                        }
                    }
                }
            };

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

            try
            {
                Console.WriteLine("\tEnsuring qBittorrent availability...");
                using (var tempClient = new QBittorrentClient(new Uri("http://localhost:8080")))
                {
                    await Utils.Retry(() => tempClient.LoginAsync(UserName, Password), delayMs: 500, attempts: 50);
                }

                Console.WriteLine("\tqBittorrent is available!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\tqBittorrent is not available!");
                Console.WriteLine($"\t{ex}");
                Console.WriteLine($"\tStopping container {ContainerId}...");
                await DockerFixture.Client.Containers.StopContainerAsync(ContainerId,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 10u });
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            Console.WriteLine($"\tStopping container {ContainerId}...");
            await DockerFixture.Client.Containers.StopContainerAsync(ContainerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10u });
            await DockerFixture.Client.Containers.WaitContainerAsync(ContainerId);
            await DockerFixture.Client.Containers.RemoveContainerAsync(ContainerId,
                new ContainerRemoveParameters { Force = true });
        }

        #endregion

        #region Versions

        [Fact]
        [PrintTestName]
        public async Task GetApiVersion()
        {
            await Client.LoginAsync(UserName, Password);
            var version = await Client.GetApiVersionAsync();
            version.Should().Be(DockerFixture.Env.ApiVersion);
        }

        [Fact]
        [PrintTestName]
        public async Task GetLegacyApiVersion()
        {
            var version = await Client.GetLegacyApiVersionAsync();
            version.Should().Be(DockerFixture.Env.LegacyApiVersion);
        }

        [Fact]
        [PrintTestName]
        public async Task GetLegacyMinApiVersion()
        {
            var version = await Client.GetLegacyMinApiVersionAsync();
            version.Should().Be(DockerFixture.Env.LegacyMinApiVersion);
        }

        [Fact]
        [PrintTestName]
        public async Task GetQBittorrentVersion()
        {
            await Client.LoginAsync(UserName, Password);
            var version = await Client.GetQBittorrentVersionAsync();
            version.Should().Be(DockerFixture.Env.QBittorrentVersion);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetBuildInfo()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var buildInfo = await Client.GetBuildInfoAsync();
            buildInfo.Should().BeEquivalentTo(DockerFixture.Env.BuildInfo);
        }

        #endregion

        #region Login/Logout

        [Fact]
        [PrintTestName]
        public async Task LoginCorrect()
        {
            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();
        }

        [Fact]
        [PrintTestName]
        public async Task LoginIncorrect()
        {
            await Client.LoginAsync(UserName, "incorrect");
            await Assert.ThrowsAsync<QBittorrentClientRequestException>(() => Client.GetTorrentListAsync());
        }

        [Fact]
        [PrintTestName]
        public async Task NoLogin()
        {
            await Assert.ThrowsAsync<QBittorrentClientRequestException>(() => Client.GetTorrentListAsync());
        }

        [Fact]
        [PrintTestName]
        public async Task Logout()
        {
            try
            {
                await Client.LoginAsync(UserName, Password);
                var list = await Client.GetTorrentListAsync();
                list.Should().BeEmpty();

                await Task.Delay(1000);

                await Client.LogoutAsync();

                await Task.Delay(1000);

                await Assert.ThrowsAsync<QBittorrentClientRequestException>(() => Client.GetTorrentListAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region Add Torrent

        [Fact]
        [PrintTestName]
        public async Task AddTorrentsFromFiles()
        {
            string[] tags = { "Tag1", "Tag2" };
            const double ratioLimit = 4;
            var seedingTimeLimit = TimeSpan.FromMinutes(48);

            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var parser = new BencodeParser();
            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var torrents = filesToAdd
                .Select(path => parser.Parse<Torrent>(path))
                .ToList();
            var hashes = torrents.Select(t => t.OriginalInfoHash.ToLower());

            var addRequest = new AddTorrentFilesRequest(filesToAdd) { Paused = true };
            if (ApiVersionMoreThan(2, 6, 1))
            {
                addRequest.Tags = tags;
            }

            if (ApiVersionMoreThan(2, 8))
            {
                addRequest.RatioLimit = ratioLimit;
                addRequest.SeedingTimeLimit = seedingTimeLimit;
            }

            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Hash).Should().BeEquivalentTo(hashes);
                if (ApiVersionMoreThan(2, 6, 1))
                {
                    list.Select(t => t.Tags).Should().AllBeEquivalentTo(tags);
                }
                if (ApiVersionMoreThan(2, 8))
                {
                    list.Select(t => t.RatioLimit).Should().AllBeEquivalentTo(4);
                }
            });

            if (ApiVersionMoreThan(2, 8))
            {
                await Utils.Retry(async () =>
                {
                    var partialData = await Client.GetPartialDataAsync();
                    partialData.TorrentsChanged.Values.Select(x => (x.SeedingTimeLimit, x.RatioLimit))
                        .Should().AllBeEquivalentTo((seedingTimeLimit, ratioLimit));
                });
            }
        }

        [Fact]
        [PrintTestName]
        public async Task AddTorrentsFromMagnetLinks()
        {
            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var parser = new BencodeParser();
            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var torrents = filesToAdd
                .Select(path => parser.Parse<Torrent>(path))
                .ToList();
            var magnets = torrents.Select(t => new Uri(t.GetMagnetLink()));

            var addRequest = new AddTorrentUrlsRequest(magnets) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(filesToAdd.Length);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task AddTorrentsFromHttpLinks()
        {
            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var links = new[]
            {
                new Uri(
                    "https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-17.10.1-desktop-amd64.iso.torrent"),
                new Uri(
                    "https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-16.04.4-desktop-amd64.iso.torrent"),
            };
            var addRequest = new AddTorrentUrlsRequest(links) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Task.Delay(1000);

            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(2);
                list.Should().Contain(t => t.Hash == "f07e0b0584745b7bcb35e98097488d34e68623d0");
                list.Should().Contain(t => t.Hash == "778ce280b595e57780ff083f2eb6f897dfa4a4ee");
            });
        }

        [Fact]
        [PrintTestName]
        public async Task AddTorrentsFromFilesAndHttpLinks()
        {
            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var links = new[]
            {
                new Uri(
                    "https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-17.10.1-desktop-amd64.iso.torrent"),
                new Uri(
                    "https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-16.04.4-desktop-amd64.iso.torrent"),
            };

            var torrentFile = Path.Combine(Utils.TorrentsFolder, "ubuntu-14.04-pack.torrent");
            var parser = new BencodeParser();
            var hash = parser.Parse<Torrent>(torrentFile).OriginalInfoHash.ToLower();

            var addRequest = new AddTorrentsRequest(new[] { torrentFile }, links) { Paused = true };

            if (ApiVersionLessThan(2))
            {
                var exception =
                    await Assert.ThrowsAsync<ApiNotSupportedException>(() => Client.AddTorrentsAsync(addRequest));
                exception.RequiredApiLevel.Should().Be(ApiLevel.V2);
            }
            else
            {
                await Client.AddTorrentsAsync(addRequest);

                await Task.Delay(1000);

                await Utils.Retry(async () =>
                {
                    list = await Client.GetTorrentListAsync();
                    list.Should().HaveCount(3);
                    list.Should().Contain(t => t.Hash == "f07e0b0584745b7bcb35e98097488d34e68623d0");
                    list.Should().Contain(t => t.Hash == "778ce280b595e57780ff083f2eb6f897dfa4a4ee");
                    list.Should().Contain(t => t.Hash == hash);
                });
            }
        }

        #endregion

        #region GetTorrentPropertiesAsync

        [Fact]
        [PrintTestName]
        public async Task GetTorrentProperties()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var props = await Client.GetTorrentPropertiesAsync(torrent.OriginalInfoHash.ToLower());
                props.Should().NotBeNull();
                props.AdditionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
                props.Comment.Should().Be(torrent.Comment);
                props.CreatedBy.Should().Be(torrent.CreatedBy ?? string.Empty);
                props.CreationDate.Should().Be(torrent.CreationDate);
                props.PieceSize.Should().Be(torrent.PieceSize);
                props.Size.Should().Be(torrent.TotalSize);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentPropertiesUnknown()
        {
            await Client.LoginAsync(UserName, Password);
            await Utils.Retry(async () =>
            {
                var props = await Client.GetTorrentPropertiesAsync("0000000000000000000000000000000000000000");
                props.Should().BeNull(because: "torrent with this hash has not been added");
            });
        }

        #endregion

        #region GetTorrentContentsAsync

        [Fact]
        [PrintTestName]
        public async Task GetTorrentContentsSingle()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(1);

                var content = contents.Single();
                content.Name.Should().Be(torrent.File.FileName);
                content.Size.Should().Be(torrent.File.FileSize);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentContentsMulti()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-14.04-pack.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { CreateRootFolder = false, Paused = true, ContentLayout = TorrentContentLayout.NoSubfolder };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(torrent.Files.Count);

                var pairs =
                    (from content in contents
                     join file in torrent.Files on content.Name equals file.FullPath
                     select (content, file))
                    .ToList();

                pairs.Should().HaveCount(torrent.Files.Count);
                foreach (var (content, file) in pairs)
                {
                    content.Size.Should().Be(file.FileSize);
                }
            });
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentContentsUnknown()
        {
            await Client.LoginAsync(UserName, Password);
            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync("0000000000000000000000000000000000000000");
                contents.Should().BeNull(because: "torrent with this hash has not been added");
            });
        }

        #endregion

        #region GetTorrentTrackersAsync

        [Fact]
        [PrintTestName]
        public async Task GetTorrentTrackers()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var trackers = await Client.GetTorrentTrackersAsync(torrent.OriginalInfoHash.ToLower());
                trackers.Should().NotBeNull();

                var trackerUrls = trackers.Where(t => t.Url.IsAbsoluteUri).Select(t => t.Url.AbsoluteUri).ToList();
                trackerUrls.Should().BeEquivalentTo(torrent.Trackers.SelectMany(x => x));

                trackers.Select(t => t.Status).Should().NotContainNulls();
                trackers.Select(t => t.TrackerStatus).Should().NotContainNulls();
            });
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentTrackersUnknown()
        {
            await Client.LoginAsync(UserName, Password);
            await Utils.Retry(async () =>
            {
                var trackers = await Client.GetTorrentTrackersAsync("0000000000000000000000000000000000000000");
                trackers.Should().BeNull(because: "torrent with this hash has not been added");
            });
        }

        #endregion

        #region GetTorrentWebSeedsAsync

        [Fact]
        [PrintTestName]
        public async Task GetTorrentWebSeeds()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var webSeeds = await Client.GetTorrentWebSeedsAsync(torrent.OriginalInfoHash.ToLower());
                webSeeds.Should().BeEmpty();
            });
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentWebSeedsUnknown()
        {
            await Client.LoginAsync(UserName, Password);
            await Utils.Retry(async () =>
            {
                var webSeeds = await Client.GetTorrentWebSeedsAsync("0000000000000000000000000000000000000000");
                webSeeds.Should().BeNull(because: "torrent with this hash has not been added");
            });
        }

        #endregion

        #region GetTorrentPiecesStatesAsync/GetTorrentPiecesHashesAsync

        [Fact]
        [PrintTestName]
        public async Task GetTorrentPiecesAndStates()
        {
            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var torrentHash = torrent.OriginalInfoHash.ToLower();
            await Utils.Retry(async () =>
            {
                var hashes = await Client.GetTorrentPiecesHashesAsync(torrentHash);
                hashes.Should().NotBeNull().And.HaveCount(torrent.NumberOfPieces);
                hashes.Should().Equal(GetHashes());
            });

            await Utils.Retry(async () =>
            {
                var states = await Client.GetTorrentPiecesStatesAsync(torrentHash);
                states.Should().NotBeNull().And.HaveCount(torrent.NumberOfPieces);
            });

            IEnumerable<string> GetHashes()
            {
                var piecesAsHex = torrent.PiecesAsHexString;
                var length = piecesAsHex.Length / torrent.NumberOfPieces;
                for (int offset = 0; offset < piecesAsHex.Length; offset += length)
                {
                    yield return piecesAsHex.Substring(offset, length).ToLower();
                }
            }
        }

        [Fact]
        [PrintTestName]
        public async Task GetTorrentPiecesAndStatesUnknown()
        {
            await Client.LoginAsync(UserName, Password);
            await Utils.Retry(async () =>
            {
                var hashes = await Client.GetTorrentPiecesHashesAsync("0000000000000000000000000000000000000000");
                hashes.Should().BeNull(because: "torrent with this hash has not been added");
            });
            await Utils.Retry(async () =>
            {
                var states = await Client.GetTorrentPiecesStatesAsync("0000000000000000000000000000000000000000");
                states.Should().BeNull(because: "torrent with this hash has not been added");
            });
        }

        #endregion

        #region GetGlobalTransferInfoAsync

        [Fact]
        [PrintTestName]
        public async Task GetGlobalTransferInfo()
        {
            await Client.LoginAsync(UserName, Password);
            var info = await Client.GetGlobalTransferInfoAsync();
            info.ConnectionStatus.Should().NotBe(ConnectionStatus.Disconnected);
            info.Should().BeEquivalentTo(
                new GlobalTransferInfo
                {
                    DownloadedData = 0,
                    DownloadSpeed = 0,
                    DownloadSpeedLimit = 0,
                    UploadedData = 0,
                    UploadSpeed = 0,
                    UploadSpeedLimit = 0
                },
                options => options
                    .Excluding(g => g.ConnectionStatus)
                    .Excluding(g => g.AdditionalData)
                    .Excluding(g => g.DhtNodes));
        }

        #endregion

        #region GetPartialDataAsync/AddCategoryAsync/DeleteCategoryAsync/DeleteAsync

#pragma warning disable 618

        [SkippableFact]
        [PrintTestName]
        public async Task GetPartialData()
        {
            Skip.IfNot(ApiVersionLessThan(2, 1), $"API 2.1+ is tested with {nameof(GetPartialData_API_2_1)} test.");

            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var parser = new BencodeParser();
            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var torrents = filesToAdd
                .Select(path => parser.Parse<Torrent>(path))
                .ToList();
            var hashes = torrents.Select(t => t.OriginalInfoHash.ToLower()).ToList();

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd[0]) { Paused = true });

            await Task.Delay(1000);

            int responseId = 0;
            var partialData = await Client.GetPartialDataAsync(responseId);
            partialData.Should().NotBeNull();
            partialData.FullUpdate.Should().BeTrue();
            partialData.TorrentsChanged.Should().HaveCount(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0]);
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeEmpty();
            partialData.CategoriesChanged.Should().BeEmpty();
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;
            var refreshInterval = partialData.ServerState.RefreshInterval ?? 1000;

            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.Should().NotBeNull();
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().BeNull();
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd.Skip(1)) { Paused = true });
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCount(2);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[1]);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[2]);
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.AddCategoryAsync("a");
            await Client.SetTorrentCategoryAsync(hashes[0], "b");
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCount(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0] && p.Value.Category == "b");
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeEquivalentTo("a", "b");
            partialData.CategoriesChanged.Should().BeEquivalentTo(
                new Dictionary<string, Category>
                {
                    ["a"] = new Category { Name = "a", SavePath = "" },
                    ["b"] = new Category { Name = "b", SavePath = "" }
                });
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.DeleteCategoryAsync("b");
            await Client.DeleteAsync(hashes[1]);
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCountGreaterOrEqualTo(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0] && p.Value.Category == "");
            partialData.TorrentsRemoved.Should().HaveCount(1);
            partialData.TorrentsRemoved.Should().Contain(hashes[1]);
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeEquivalentTo("b");
        }


        [SkippableFact]
        [PrintTestName]
        public async Task GetPartialData_API_2_1()
        {
            Skip.If(ApiVersionLessThan(2, 1), $"API prior to 2.1 is tested with {nameof(GetPartialData)} test.");

            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var parser = new BencodeParser();
            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var torrents = filesToAdd
                .Select(path => parser.Parse<Torrent>(path))
                .ToList();
            var hashes = torrents.Select(t => t.OriginalInfoHash.ToLower()).ToList();

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd[0]) { Paused = true });

            await Task.Delay(1000);

            int responseId = 0;
            var partialData = await Client.GetPartialDataAsync(responseId);
            partialData.Should().NotBeNull();
            partialData.FullUpdate.Should().BeTrue();
            partialData.TorrentsChanged.Should().HaveCount(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0]);
            partialData.TorrentsRemoved.Should().BeNull();
            if (ApiVersionLessThan(2, 9))
            {
                partialData.CategoriesAdded.Should().BeEmpty();
                partialData.CategoriesChanged.Should().BeEmpty();
            }
            else
            {
                partialData.CategoriesAdded.Should().BeNull();
                partialData.CategoriesChanged.Should().BeNull();
            }
            partialData.CategoriesRemoved.Should().BeNull();

            if (!ApiVersionLessThan(2, 1, 1) && !ApiVersionIs(2, 9, 2))
            {
                partialData.ServerState.FreeSpaceOnDisk.Should().BeGreaterThan(0);
            }

            responseId = partialData.ResponseId;
            var refreshInterval = partialData.ServerState.RefreshInterval ?? 1000;

            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.Should().NotBeNull();
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().BeNull();
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd.Skip(1)) { Paused = true });
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCount(2);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[1]);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[2]);
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.AddCategoryAsync("a");
            await Client.AddCategoryAsync("b", "/tmp");
            await Client.SetTorrentCategoryAsync(hashes[0], "b");
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCount(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0] && p.Value.Category == "b");
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeEquivalentTo(
                new Dictionary<string, Category>
                {
                    ["a"] = new Category { Name = "a", SavePath = "" },
                    ["b"] = new Category { Name = "b", SavePath = "/tmp" }
                });
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.DeleteCategoryAsync("b");
            await Client.DeleteAsync(hashes[1]);
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCountGreaterOrEqualTo(1);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[0] && p.Value.Category == "");
            partialData.TorrentsRemoved.Should().HaveCount(1);
            partialData.TorrentsRemoved.Should().Contain(hashes[1]);
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeNull();
            partialData.CategoriesRemoved.Should().BeEquivalentTo("b");
        }

#pragma warning restore 618

        #endregion

        #region Pause/Resume

        [Fact]
        [PrintTestName]
        public async Task Pause()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd));
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);

            var hash = list[1].Hash;
            await Client.PauseAsync(hash);
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().ContainSingle(t => t.State == TorrentState.PausedDownload)
                    .Which.Hash.Should().Be(hash);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task Resume()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd) { Paused = true });
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);

            var hash = list[1].Hash;
            await Client.ResumeAsync(hash);

            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().ContainSingle(t => t.State != TorrentState.PausedDownload)
                    .Which.Hash.Should().Be(hash);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task PauseAll()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd));
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);

            await Client.PauseAsync();
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ResumeAll()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd) { Paused = true });

            IReadOnlyList<TorrentInfo> list = null;

            do
            {
                await Task.Delay(1000);
                list = await Client.GetTorrentListAsync();
            }
            while (list.Any(t => t.State == TorrentState.CheckingResumeData));

            list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);

            await Client.ResumeAsync();
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task PauseAllLegacy()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd));
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);

#pragma warning disable CS0618 // Type or member is obsolete
            await Client.PauseAllAsync();
#pragma warning restore CS0618 // Type or member is obsolete
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ResumeAllLegacy()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd) { Paused = true });
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);

#pragma warning disable CS0618 // Type or member is obsolete
            await Client.ResumeAllAsync();
#pragma warning restore CS0618 // Type or member is obsolete
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);
            });
        }

        #endregion

        #region AddCategoryAsync/EditCategoryAsync/GetCategoriesAsync

        [SkippableFact]
        [PrintTestName]
        public async Task Categories()
        {
            Skip.If(ApiVersionLessThan(2, 1, 1), "API 2.1.1+ required for this test.");

            await Client.LoginAsync(UserName, Password);
            var categories = await Client.GetCategoriesAsync();
            categories.Should().BeEmpty();

            await Client.AddCategoryAsync("Default");
            await Client.AddCategoryAsync("Test", "/downloads/test1");

            await Utils.Retry(async () =>
            {
                categories = await Client.GetCategoriesAsync();
                categories.Should().NotBeEmpty();
                categories.Should().BeEquivalentTo(
                    new Dictionary<string, Category>()
                    {
                        ["Default"] = new Category { Name = "Default", SavePath = "" },
                        ["Test"] = new Category { Name = "Test", SavePath = "/downloads/test1" },
                    },
                    config => config.Excluding(x => x.Value.AdditionalData));
            });

            await Client.EditCategoryAsync("Test", "/downloads/test2");

            await Utils.Retry(async () =>
            {
                categories = await Client.GetCategoriesAsync();
                categories.Should().NotBeEmpty();
                categories.Should().BeEquivalentTo(
                    new Dictionary<string, Category>()
                    {
                        ["Default"] = new Category { Name = "Default", SavePath = "" },
                        ["Test"] = new Category { Name = "Test", SavePath = "/downloads/test2" },
                    },
                    config => config.Excluding(x => x.Value.AdditionalData));
            });
        }

        #endregion

        #region SetTorrentCategoryAsync

        [Fact]
        [PrintTestName]
        public async Task SetTorrentCategory()
        {
            await Client.LoginAsync(UserName, Password);

            if (!ApiVersionLessThan(2, 1))
            {
                // API 2.1+ requires category to exist before adding to the torrent.
                await Client.AddCategoryAsync("test");
                await Client.AddCategoryAsync("a/b");
            }

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) { Paused = true });
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().BeEmpty();
            var hash = list.Single().Hash;

            await Client.SetTorrentCategoryAsync(hash, "test");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().Be("test");

            await Client.SetTorrentCategoryAsync(hash, "a/b");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().Be("a/b");

            await Client.SetTorrentCategoryAsync(hash, "");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().BeEmpty();
        }

        [Fact]
        [PrintTestName]
        public async Task SetTorrentCategoryWithPreset()
        {
            await Client.LoginAsync(UserName, Password);

            if (!ApiVersionLessThan(2, 1))
            {
                // API 2.1+ requires category to exist before adding to the torrent.
                await Client.AddCategoryAsync("test");
                await Client.AddCategoryAsync("a/b");
            }

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) { Paused = true, Category = "xyz" });
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().Be("xyz");
            var hash = list.Single().Hash;

            await Client.SetTorrentCategoryAsync(hash, "test");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().Be("test");

            await Client.SetTorrentCategoryAsync(hash, "a/b");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().Be("a/b");

            await Client.SetTorrentCategoryAsync(hash, "");
            await Task.Delay(1000);
            list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle().Which.Category.Should().BeEmpty();
        }

        #endregion

        #region Limits

        [Fact]
        [PrintTestName]
        public async Task GlobalLimits()
        {
            const long downLimit = 2048 * 1024;
            const long upLimit = 1024 * 1024;

            await Client.LoginAsync(UserName, Password);

            var (down, up, info) = await Utils.WhenAll(
                Client.GetGlobalDownloadLimitAsync(),
                Client.GetGlobalUploadLimitAsync(),
                Client.GetGlobalTransferInfoAsync());
            down.Should().Be(0);
            up.Should().Be(0);
            info.DownloadSpeedLimit.Should().Be(0);
            info.UploadSpeedLimit.Should().Be(0);

            await Client.SetGlobalDownloadLimitAsync(downLimit);
            await Task.Delay(1000);
            (down, up, info) = await Utils.WhenAll(
                Client.GetGlobalDownloadLimitAsync(),
                Client.GetGlobalUploadLimitAsync(),
                Client.GetGlobalTransferInfoAsync());
            down.Should().Be(downLimit);
            up.Should().Be(0);
            info.DownloadSpeedLimit.Should().Be(downLimit);
            info.UploadSpeedLimit.Should().Be(0);

            await Client.SetGlobalUploadLimitAsync(upLimit);
            await Task.Delay(1000);
            (down, up, info) = await Utils.WhenAll(
                Client.GetGlobalDownloadLimitAsync(),
                Client.GetGlobalUploadLimitAsync(),
                Client.GetGlobalTransferInfoAsync());
            down.Should().Be(downLimit);
            up.Should().Be(upLimit);
            info.DownloadSpeedLimit.Should().Be(downLimit);
            info.UploadSpeedLimit.Should().Be(upLimit);

            await Client.SetGlobalDownloadLimitAsync(0);
            await Task.Delay(1000);
            (down, up, info) = await Utils.WhenAll(
                Client.GetGlobalDownloadLimitAsync(),
                Client.GetGlobalUploadLimitAsync(),
                Client.GetGlobalTransferInfoAsync());
            down.Should().Be(0);
            up.Should().Be(upLimit);
            info.DownloadSpeedLimit.Should().Be(0);
            info.UploadSpeedLimit.Should().Be(upLimit);

            await Client.SetGlobalUploadLimitAsync(0);
            await Task.Delay(1000);
            (down, up, info) = await Utils.WhenAll(
                Client.GetGlobalDownloadLimitAsync(),
                Client.GetGlobalUploadLimitAsync(),
                Client.GetGlobalTransferInfoAsync());
            down.Should().Be(0);
            up.Should().Be(0);
            info.DownloadSpeedLimit.Should().Be(0);
            info.UploadSpeedLimit.Should().Be(0);
        }

        [Fact]
        [PrintTestName]
        public async Task TorrentLimitsInitiallyNotSet()
        {
            const long downLimit = 2048 * 1024;
            const long upLimit = 1024 * 1024;

            await Client.LoginAsync(UserName, Password);

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) { Paused = true });
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle();
            var torrent = list.Single();
            var hash = torrent.Hash;

            var (down, up, props) = await Utils.WhenAll(
                Client.GetTorrentDownloadLimitAsync(hash),
                Client.GetTorrentUploadLimitAsync(hash),
                Client.GetTorrentPropertiesAsync(hash));
            down.Should().Be(DefaultTorrentRateLimit);
            up.Should().Be(DefaultTorrentRateLimit);
            props.DownloadLimit.Should().Be(null);
            props.UploadLimit.Should().Be(null);

            await Client.SetTorrentDownloadLimitAsync(hash, downLimit);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(downLimit);
                up.Should().Be(0);
                props.DownloadLimit.Should().Be(downLimit);
                props.UploadLimit.Should().Be(null);
            });

            await Client.SetTorrentUploadLimitAsync(hash, upLimit);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(downLimit);
                up.Should().Be(upLimit);
                props.DownloadLimit.Should().Be(downLimit);
                props.UploadLimit.Should().Be(upLimit);
            });

            await Client.SetTorrentDownloadLimitAsync(hash, 0);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(0);
                up.Should().Be(upLimit);
                props.DownloadLimit.Should().Be(null);
                props.UploadLimit.Should().Be(upLimit);
            });

            await Client.SetTorrentUploadLimitAsync(hash, 0);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(0);
                up.Should().Be(0);
                props.DownloadLimit.Should().Be(null);
                props.UploadLimit.Should().Be(null);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task TorrentLimitsInitiallySet()
        {
            const int downPreset = 4 * 1024 * 1024;
            const int upPreset = 3 * 1024 * 1024;
            const long downLimit = 2 * 1024 * 1024;
            const long upLimit = 1024 * 1024;

            await Client.LoginAsync(UserName, Password);

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file)
            {
                Paused = true,
                DownloadLimit = downPreset,
                UploadLimit = upPreset
            });
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle();
            var torrent = list.Single();
            var hash = torrent.Hash;

            var (down, up, props) = await Utils.WhenAll(
                Client.GetTorrentDownloadLimitAsync(hash),
                Client.GetTorrentUploadLimitAsync(hash),
                Client.GetTorrentPropertiesAsync(hash));
            down.Should().Be(downPreset);
            up.Should().Be(upPreset);
            props.DownloadLimit.Should().Be(downPreset);
            props.UploadLimit.Should().Be(upPreset);

            await Client.SetTorrentDownloadLimitAsync(hash, downLimit);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(downLimit);
                up.Should().Be(upPreset);
                props.DownloadLimit.Should().Be(downLimit);
                props.UploadLimit.Should().Be(upPreset);
            });

            await Client.SetTorrentUploadLimitAsync(hash, upLimit);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(downLimit);
                up.Should().Be(upLimit);
                props.DownloadLimit.Should().Be(downLimit);
                props.UploadLimit.Should().Be(upLimit);
            });

            await Client.SetTorrentDownloadLimitAsync(hash, 0);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(0);
                up.Should().Be(upLimit);
                props.DownloadLimit.Should().Be(null);
                props.UploadLimit.Should().Be(upLimit);
            });

            await Client.SetTorrentUploadLimitAsync(hash, 0);
            await Utils.Retry(async () =>
            {
                (down, up, props) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hash),
                    Client.GetTorrentUploadLimitAsync(hash),
                    Client.GetTorrentPropertiesAsync(hash));
                down.Should().Be(0);
                up.Should().Be(0);
                props.DownloadLimit.Should().Be(null);
                props.UploadLimit.Should().Be(null);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AllTorrentLimitsInitiallyNotSet()
        {
            Skip.If(ApiVersionLessThan(2));

            const long downLimit = 2048 * 1024;
            const long upLimit = 1024 * 1024;

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            var parser = new BencodeParser();
            var hashes = files.Select(f => parser.Parse<Torrent>(f).OriginalInfoHash.ToLower()).ToArray();

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(files) { Paused = true });
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().HaveCount(files.Length);

            var (down, up) = await Utils.WhenAll(
                Client.GetTorrentDownloadLimitAsync(hashes),
                Client.GetTorrentUploadLimitAsync(hashes));
            down.Should().HaveCount(files.Length);
            down.Values.Should().AllBeEquivalentTo(DefaultTorrentRateLimit);
            up.Should().HaveCount(files.Length);
            up.Values.Should().AllBeEquivalentTo(DefaultTorrentRateLimit);

            await Client.SetTorrentDownloadLimitAsync(downLimit);
            await Utils.Retry(async () =>
            {
                (down, up) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hashes),
                    Client.GetTorrentUploadLimitAsync(hashes));
                down.Should().HaveCount(files.Length);
                down.Values.Should().AllBeEquivalentTo(downLimit);
                up.Should().HaveCount(files.Length);
                up.Values.Should().AllBeEquivalentTo(0L);
            });

            await Client.SetTorrentUploadLimitAsync(upLimit);
            await Utils.Retry(async () =>
            {
                (down, up) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hashes),
                    Client.GetTorrentUploadLimitAsync(hashes));
                down.Should().HaveCount(files.Length);
                down.Values.Should().AllBeEquivalentTo(downLimit);
                up.Should().HaveCount(files.Length);
                up.Values.Should().AllBeEquivalentTo(upLimit);
            });

            await Client.SetTorrentDownloadLimitAsync(0);
            await Utils.Retry(async () =>
            {
                (down, up) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hashes),
                    Client.GetTorrentUploadLimitAsync(hashes));
                down.Should().HaveCount(files.Length);
                down.Values.Should().AllBeEquivalentTo(0L);
                up.Should().HaveCount(files.Length);
                up.Values.Should().AllBeEquivalentTo(upLimit);
            });

            await Client.SetTorrentUploadLimitAsync(0);
            await Utils.Retry(async () =>
            {
                (down, up) = await Utils.WhenAll(
                    Client.GetTorrentDownloadLimitAsync(hashes),
                    Client.GetTorrentUploadLimitAsync(hashes));
                down.Should().HaveCount(files.Length);
                down.Values.Should().AllBeEquivalentTo(0L);
                up.Should().HaveCount(files.Length);
                up.Values.Should().AllBeEquivalentTo(0L);
            });
        }

        #endregion

        #region ChangeTorrentPriorityAsync

        [Fact]
        [PrintTestName]
        public async Task ChangeTorrentPriorityIncrease()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            var addRequest = new AddTorrentFilesRequest(filesToAdd) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                return list;
            });

            await Client.ChangeTorrentPriorityAsync(torrents[2].Hash, TorrentPriorityChange.Increase);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                list.Select(t => t.Hash).Should().Equal(torrents[0].Hash, torrents[2].Hash, torrents[1].Hash);
                return list;
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ChangeTorrentPriorityDecrease()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            var addRequest = new AddTorrentFilesRequest(filesToAdd) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                return list;
            });

            await Client.ChangeTorrentPriorityAsync(torrents[0].Hash, TorrentPriorityChange.Decrease);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                list.Select(t => t.Hash).Should().Equal(torrents[1].Hash, torrents[0].Hash, torrents[2].Hash);
                return list;
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ChangeTorrentPriorityMaximal()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            var addRequest = new AddTorrentFilesRequest(filesToAdd) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                return list;
            });

            await Client.ChangeTorrentPriorityAsync(torrents[2].Hash, TorrentPriorityChange.Maximal);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                list.Select(t => t.Hash).Should().Equal(torrents[2].Hash, torrents[0].Hash, torrents[1].Hash);
                return list;
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ChangeTorrentPriorityMinimal()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");

            var addRequest = new AddTorrentFilesRequest(filesToAdd) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                return list;
            });

            await Client.ChangeTorrentPriorityAsync(torrents[0].Hash, TorrentPriorityChange.Minimal);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync(new TorrentListQuery { SortBy = "priority" });
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Priority).Should().Equal(1, 2, 3);
                list.Select(t => t.Hash).Should().Equal(torrents[1].Hash, torrents[2].Hash, torrents[0].Hash);
                return list;
            });
        }

        #endregion

        #region DeleteAsync

        [Fact]
        [PrintTestName]
        public async Task Delete()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

            var hash = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single().Hash;
            });

            await Client.DeleteAsync(hash, true);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Should().BeEmpty();
            });
        }

        [Fact]
        [PrintTestName]
        public async Task DeleteAll()
        {
            await Client.LoginAsync(UserName, Password);

            var filesToAdd = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd));

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(filesToAdd.Length);
            });


            if (ApiVersionLessThan(2))
            {
                var exception = await Assert.ThrowsAsync<ApiNotSupportedException>(() => Client.DeleteAsync(true));
                exception.RequiredApiLevel.Should().Be(ApiLevel.V2);
                return;
            }

            await Client.DeleteAsync(true);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Should().BeEmpty();
            });
        }

        #endregion

        #region SetLocationAsync

        [SkippableFact]
        [PrintTestName]
        public async Task SetLocation()
        {
            Skip.If(Environment.OSVersion.Platform != PlatformID.Unix,
                "This test is supported only on linux at the moment.");

            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

            var defaultDir = await Client.GetDefaultSavePathAsync();

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.SavePath.Should().Be(defaultDir);

            await Client.SetLocationAsync(torrent.Hash, "/tmp/");

            await Utils.Retry(async () =>
            {
                var props = await Client.GetTorrentPropertiesAsync(torrent.Hash);
                Path.TrimEndingDirectorySeparator(props.SavePath).Should().Be("/tmp");
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetLocationForAll()
        {
            //Skip.If(Environment.OSVersion.Platform != PlatformID.Unix,
            //    "This test is supported only on linux at the moment.");
            Skip.If(ApiVersionLessThan(2), "API version 2+ is required.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(files));

            var defaultDir = await Client.GetDefaultSavePathAsync();

            var torrentList = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrentList.Select(t => t.SavePath)
                .Should().BeEquivalentTo(Enumerable.Repeat(defaultDir, files.Length));

            await Client.SetLocationAsync("/tmp/");

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                list.Select(t => Path.TrimEndingDirectorySeparator(t.SavePath))
                    .Should().BeEquivalentTo(Enumerable.Repeat("/tmp", files.Length));
            });
        }

        #endregion

        #region RenameAsync

        [Fact]
        [PrintTestName]
        public async Task Rename()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.Name.Should().Be("ubuntu-16.04.4-desktop-amd64.iso");

            var newName = Guid.NewGuid().ToString("N");
            await Client.RenameAsync(torrent.Hash, newName);

            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().Name.Should().Be(newName);
            });
        }

        #endregion

        #region AddTrackerAsync

        [Fact]
        [PrintTestName]
        public async Task AddTracker()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            var tracker1 = new Uri("http://torrent.ubuntu.com:6969/announce");
            var tracker2 = new Uri("http://ipv6.torrent.ubuntu.com:6969/announce");

            var trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
            trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                .Should().BeEquivalentTo(tracker1, tracker2);

            var newTracker = new Uri("http://retracker.mgts.by:80/announce");
            await Client.AddTrackerAsync(torrent.Hash, newTracker);

            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                    .Should().BeEquivalentTo(tracker1, tracker2, newTracker);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AddEditDeleteTracker()
        {
            Skip.If(ApiVersionLessThan(2, 2), "API 2.2+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            var tracker1 = new Uri("http://torrent.ubuntu.com:6969/announce");
            var tracker2 = new Uri("http://ipv6.torrent.ubuntu.com:6969/announce");

            var trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
            trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                .Should().BeEquivalentTo(tracker1, tracker2);

            var newTracker = new Uri("http://retracker.mgts.by:80/announce");
            await Client.AddTrackerAsync(torrent.Hash, newTracker);

            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                    .Should().BeEquivalentTo(tracker1, tracker2, newTracker);
            });

            await Client.DeleteTrackersAsync(torrent.Hash, new[] { tracker1, newTracker });

            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                    .Should().BeEquivalentTo(tracker2);
            });

            await Client.EditTrackerAsync(torrent.Hash, tracker2, tracker1);

            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                    .Should().BeEquivalentTo(tracker1);
            });

            await Client.DeleteTrackerAsync(torrent.Hash, tracker1);
            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Where(url => url.IsAbsoluteUri)
                    .Should().BeEmpty();
            });
        }

        #endregion

        #region GetLogAsync

        [Fact]
        [PrintTestName]
        public async Task GetLog()
        {
            await Client.LoginAsync(UserName, Password);

            var log = await Client.GetLogAsync();
            log.Should().NotBeEmpty();
            log.Select(x => x.Severity).Distinct().Count().Should().BeGreaterThan(1);
        }

        [Theory]
        [InlineData(TorrentLogSeverity.Critical)]
        [InlineData(TorrentLogSeverity.Info)]
        [InlineData(TorrentLogSeverity.Normal)]
        [InlineData(TorrentLogSeverity.Warning)]
        [PrintTestName]
        public async Task GetLogBySeverity(TorrentLogSeverity severity)
        {
            await Client.LoginAsync(UserName, Password);

            var log = await Client.GetLogAsync(severity: severity);
            log.All(l => l.Severity == severity).Should().BeTrue();
        }

        [Fact]
        [PrintTestName]
        public async Task GetLogAfterId()
        {
            await Client.LoginAsync(UserName, Password);

            var log = await Client.GetLogAsync(afterId: 3);
            log.Min(l => l.Id).Should().Be(4);
        }

        [Fact]
        [PrintTestName]
        public async Task GetPeerLog()
        {
            await Client.LoginAsync(UserName, Password);

            if (ApiVersionLessThan(2))
            {
                var exception = await Assert.ThrowsAsync<ApiNotSupportedException>(
                    () => Client.GetPeerLogAsync());
                exception.RequiredApiLevel.Should().Be(ApiLevel.V2);
            }
            else
            {
                var log = await Client.GetPeerLogAsync();
                log.Should().BeEmpty();
            }
        }

        #endregion

        #region Alternative Speed Limits

        [Fact]
        [PrintTestName]
        public async Task AlternativeSpeedLimits()
        {
            await Client.LoginAsync(UserName, Password);

            var asl = await Client.GetAlternativeSpeedLimitsEnabledAsync();
            asl.Should().BeFalse();

            await Client.ToggleAlternativeSpeedLimitsAsync();
            await Task.Delay(1000);
            asl = await Client.GetAlternativeSpeedLimitsEnabledAsync();
            asl.Should().BeTrue();

            await Client.ToggleAlternativeSpeedLimitsAsync();
            await Task.Delay(1000);
            asl = await Client.GetAlternativeSpeedLimitsEnabledAsync();
            asl.Should().BeFalse();
        }

        #endregion

        #region Torrent Download Options

        [Fact]
        [PrintTestName]
        public async Task AutomaticTorrentManagement()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.AutomaticTorrentManagement.Should().Be(false);

            await Client.SetAutomaticTorrentManagementAsync(torrent.Hash, true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().AutomaticTorrentManagement.Should().Be(true);
            });

            await Client.SetAutomaticTorrentManagementAsync(torrent.Hash, false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().AutomaticTorrentManagement.Should().Be(false);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AutomaticTorrentManagementForAll()
        {
            Skip.If(ApiVersionLessThan(2), "API 2.0+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(files)));

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrents.Select(t => t.AutomaticTorrentManagement)
                .Should().AllBeEquivalentTo(false);

            await Client.SetAutomaticTorrentManagementAsync(true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.AutomaticTorrentManagement)
                    .Should().AllBeEquivalentTo(true);
            });

            await Client.SetAutomaticTorrentManagementAsync(false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.AutomaticTorrentManagement)
                    .Should().AllBeEquivalentTo(false);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AutomaticTorrentManagementOnAdd()
        {
            Skip.If(ApiVersionLessThan(2, 2), "API 2.2+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(
                new AddTorrentFilesRequest(fileToAdd)
                {
                    AutomaticTorrentManagement = true
                }));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.AutomaticTorrentManagement.Should().Be(true);

            await Client.SetAutomaticTorrentManagementAsync(torrent.Hash, false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().AutomaticTorrentManagement.Should().Be(false);
            });

            await Client.SetAutomaticTorrentManagementAsync(torrent.Hash, true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().AutomaticTorrentManagement.Should().Be(true);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task ForceStart()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.ForceStart.Should().Be(false);

            await Client.SetForceStartAsync(torrent.Hash, true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().ForceStart.Should().Be(true);
            });

            await Client.SetForceStartAsync(torrent.Hash, false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().ForceStart.Should().Be(false);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task ForceStartForAll()
        {
            Skip.If(ApiVersionLessThan(2), "API 2.0+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(files)));

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrents.Select(t => t.ForceStart)
                .Should().AllBeEquivalentTo(false);

            await Client.SetForceStartAsync(true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.ForceStart)
                    .Should().AllBeEquivalentTo(true);
            });

            await Client.SetForceStartAsync(false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.ForceStart)
                    .Should().AllBeEquivalentTo(false);
            });
        }

        [Fact]
        [PrintTestName]
        public async Task SuperSeeding()
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.SuperSeeding.Should().Be(false);

            await Client.SetSuperSeedingAsync(torrent.Hash, true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().SuperSeeding.Should().Be(true);
            });

            await Client.SetSuperSeedingAsync(torrent.Hash, false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().SuperSeeding.Should().Be(false);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SuperSeedingForAll()
        {
            Skip.If(ApiVersionLessThan(2), "API 2.0+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(files)));

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrents.Select(t => t.SuperSeeding)
                .Should().AllBeEquivalentTo(false);

            await Client.SetSuperSeedingAsync(true);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.SuperSeeding)
                    .Should().AllBeEquivalentTo(true);
            });

            await Client.SetSuperSeedingAsync(false);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.SuperSeeding)
                    .Should().AllBeEquivalentTo(false);
            });
        }

        [SkippableTheory]
        [InlineData(false)]
        [InlineData(true)]
        [PrintTestName]
        public async Task FirstLastPiecePrioritized(bool initial)
        {
            Skip.If(DockerFixture.Env.QBittorrentVersion == new System.Version(4, 0, 4),
                "FirstLastPiecePrioritized is broken in qBittorrent 4.0.4");

            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)
            { FirstLastPiecePrioritized = initial }));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.FirstLastPiecePrioritized.Should().Be(initial);

            await Client.ToggleFirstLastPiecePrioritizedAsync(torrent.Hash);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().FirstLastPiecePrioritized.Should().Be(!initial);
            });

            await Client.ToggleFirstLastPiecePrioritizedAsync(torrent.Hash);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().FirstLastPiecePrioritized.Should().Be(initial);
            });
        }

        [SkippableTheory]
        [InlineData(false)]
        [InlineData(true)]
        [PrintTestName]
        public async Task FirstLastPiecePrioritizedForAll(bool initial)
        {
            Skip.If(ApiVersionLessThan(2), "API 2.0+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Utils.Retry(() =>
                Client.AddTorrentsAsync(new AddTorrentFilesRequest(files) { FirstLastPiecePrioritized = initial }));

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrents.Select(t => t.FirstLastPiecePrioritized)
                .Should().AllBeEquivalentTo(initial);

            await Client.ToggleFirstLastPiecePrioritizedAsync();
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.FirstLastPiecePrioritized)
                    .Should().AllBeEquivalentTo(!initial);
            });

            await Client.ToggleFirstLastPiecePrioritizedAsync();
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.FirstLastPiecePrioritized)
                    .Should().AllBeEquivalentTo(initial);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PrintTestName]
        public async Task SequentialDownload(bool initial)
        {
            await Client.LoginAsync(UserName, Password);

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() =>
                Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd) { SequentialDownload = initial }));

            var torrent = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            torrent.SequentialDownload.Should().Be(initial);

            await Client.ToggleSequentialDownloadAsync(torrent.Hash);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().SequentialDownload.Should().Be(!initial);
            });

            await Client.ToggleSequentialDownloadAsync(torrent.Hash);
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Single().SequentialDownload.Should().Be(initial);
            });
        }

        [SkippableTheory]
        [InlineData(false)]
        [InlineData(true)]
        [PrintTestName]
        public async Task SequentialDownloadForAll(bool initial)
        {
            Skip.If(ApiVersionLessThan(2), "API 2.0+ required for this test.");

            await Client.LoginAsync(UserName, Password);

            var files = Directory.GetFiles(Utils.TorrentsFolder, "*.torrent");
            await Utils.Retry(() =>
                Client.AddTorrentsAsync(new AddTorrentFilesRequest(files) { SequentialDownload = initial }));

            var torrents = await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(files.Length);
                return list;
            });

            torrents.Select(t => t.SequentialDownload)
                .Should().AllBeEquivalentTo(initial);

            await Client.ToggleSequentialDownloadAsync();
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.SequentialDownload)
                    .Should().AllBeEquivalentTo(!initial);
            });

            await Client.ToggleSequentialDownloadAsync();
            await Utils.Retry(async () =>
            {
                var list = await Client.GetTorrentListAsync();
                list.Select(t => t.SequentialDownload)
                    .Should().AllBeEquivalentTo(initial);
            });
        }

        #endregion

        #region Set Share Limits

        [SkippableFact]
        [PrintTestName]
        public async Task SetShareLimit()
        {
            Skip.If(ApiVersionLessThan(2, 0, 1) || ApiVersionMoreThan(2, 9, 1));

            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)));

            var torrent = await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            int responseId = 0;
            var partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            var info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Global);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);

            await Client.SetShareLimitsAsync(torrent.Hash, ShareLimits.Ratio.Unlimited, TimeSpan.FromHours(1));
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Unlimited);
            info.SeedingTimeLimit.Should().Be(TimeSpan.FromHours(1));

            await Client.SetShareLimitsAsync(torrent.Hash, 10, ShareLimits.SeedingTime.Unlimited);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(10.0);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Unlimited);

            await Client.SetShareLimitsAsync(torrent.Hash, 0, ShareLimits.SeedingTime.Global);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(0);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);

            await Client.SetShareLimitsAsync(torrent.Hash, ShareLimits.Ratio.Global, TimeSpan.Zero);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Global);
            info.SeedingTimeLimit.Should().Be(TimeSpan.Zero);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetShareLimit_API_2_9_2()
        {
            Skip.If(ApiVersionLessThan(2, 9, 2));

            await Client.LoginAsync(UserName, Password);
            var list = await Client.GetTorrentListAsync();
            list.Should().BeEmpty();

            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Utils.Retry(() => Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd)));

            var torrent = await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                return list.Single();
            });

            int responseId = 0;
            var partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            var info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Global);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);
            info.InactiveSeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);

            await Client.SetShareLimitsAsync(torrent.Hash, ShareLimits.Ratio.Unlimited, TimeSpan.FromHours(1), TimeSpan.FromHours(0.5));
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Unlimited);
            info.SeedingTimeLimit.Should().Be(TimeSpan.FromHours(1));
            info.InactiveSeedingTimeLimit.Should().Be(TimeSpan.FromHours(0.5));

            await Client.SetShareLimitsAsync(torrent.Hash, 10, ShareLimits.SeedingTime.Unlimited, ShareLimits.SeedingTime.Global);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(10.0);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Unlimited);
            info.InactiveSeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);

            await Client.SetShareLimitsAsync(torrent.Hash, 0, ShareLimits.SeedingTime.Global, ShareLimits.SeedingTime.Unlimited);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(0);
            info.SeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Global);
            info.InactiveSeedingTimeLimit.Should().Be(ShareLimits.SeedingTime.Unlimited);

            await Client.SetShareLimitsAsync(torrent.Hash, ShareLimits.Ratio.Global, TimeSpan.Zero, TimeSpan.Zero);
            await Task.Delay(1000);
            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.TorrentsChanged.Should().HaveCount(1);

            info = partialData.TorrentsChanged[torrent.Hash];
            info.RatioLimit.Should().Be(ShareLimits.Ratio.Global);
            info.SeedingTimeLimit.Should().Be(TimeSpan.Zero);
            info.InactiveSeedingTimeLimit.Should().Be(TimeSpan.Zero);
        }

        #endregion

        #region Preferences

        [Fact]
        [PrintTestName]
        public async Task GetPreferences()
        {
            await Client.LoginAsync(UserName, Password);
            var prefs = await Client.GetPreferencesAsync();
        }

        [SkippableTheory]
        [InlineData(nameof(Preferences.Locale), "C", "de")]
        [InlineData(nameof(Preferences.TempPathEnabled), false, true)]
        [InlineData(nameof(Preferences.ExportDirectory), "", "/tmp")]
        [InlineData(nameof(Preferences.ExportDirectoryForFinished), "", "/tmp")]
        [InlineData(nameof(Preferences.MailNotificationEnabled), false, true)]
        [InlineData(nameof(Preferences.MailNotificationEmailAddress), "", "test@example.com")]
        [InlineData(nameof(Preferences.MailNotificationSmtpServer), "smtp.changeme.com", "smtp.example.com")]
        [InlineData(nameof(Preferences.MailNotificationSslEnabled), false, true)]
        [InlineData(nameof(Preferences.MailNotificationAuthenticationEnabled), false, true)]
        [InlineData(nameof(Preferences.MailNotificationUsername), "", "testuser")]
        [InlineData(nameof(Preferences.MailNotificationPassword), "", "testpassword")]
        [InlineData(nameof(Preferences.AutorunEnabled), false, true)]
        [InlineData(nameof(Preferences.AutorunProgram), "", "/bin/ls")]
        [InlineData(nameof(Preferences.PreallocateAll), false, true)]
        [InlineData(nameof(Preferences.QueueingEnabled), true, false)]
        [InlineData(nameof(Preferences.MaxActiveDownloads), 3, 6)]
        [InlineData(nameof(Preferences.MaxActiveUploads), 3, 7)]
        [InlineData(nameof(Preferences.MaxActiveTorrents), 5, 10)]
        [InlineData(nameof(Preferences.DoNotCountSlowTorrents), false, true)]
        [InlineData(nameof(Preferences.MaxRatioAction), MaxRatioAction.Pause, MaxRatioAction.Remove)]
        [InlineData(nameof(Preferences.AppendExtensionToIncompleteFiles), false, true)]
        [InlineData(nameof(Preferences.ListenPort), 6881, 8888)]
        [InlineData(nameof(Preferences.UpnpEnabled), true, false)]
        [InlineData(nameof(Preferences.RandomPort), false, true, new[] { nameof(Preferences.ListenPort) })]
        [InlineData(nameof(Preferences.DownloadLimit), 0, 40960)]
        [InlineData(nameof(Preferences.UploadLimit), 0, 40960)]
        [InlineData(nameof(Preferences.MaxConnections), 500, 600)]
        [InlineData(nameof(Preferences.MaxConnectionsPerTorrent), 100, 200)]
        [InlineData(nameof(Preferences.LimitUTPRate), true, false)]
        [InlineData(nameof(Preferences.LimitTcpOverhead), false, true)]
        [InlineData(nameof(Preferences.AlternativeDownloadLimit), 10240, 20480)]
        [InlineData(nameof(Preferences.AlternativeUploadLimit), 10240, 20480)]
        [InlineData(nameof(Preferences.BittorrentProtocol), BittorrentProtocol.Both, BittorrentProtocol.Tcp)]
        [InlineData(nameof(Preferences.BittorrentProtocol), BittorrentProtocol.Both, BittorrentProtocol.uTP)]
        [InlineData(nameof(Preferences.SchedulerEnabled), false, true)]
        [InlineData(nameof(Preferences.SchedulerDays), SchedulerDay.Every, SchedulerDay.Weekday)]
        [InlineData(nameof(Preferences.DHT), true, false)]
        [InlineData(nameof(Preferences.PeerExchange), true, false)]
        [InlineData(nameof(Preferences.LocalPeerDiscovery), true, false)]
        [InlineData(nameof(Preferences.Encryption), Encryption.Prefer, Encryption.ForceOn)]
        [InlineData(nameof(Preferences.AnonymousMode), false, true)]
        [InlineData(nameof(Preferences.ProxyType), nameof(GetPreferenceDefault), ProxyType.Http)]
        [InlineData(nameof(Preferences.ProxyAddress), nameof(GetPreferenceDefault), "192.168.254.200")]
        [InlineData(nameof(Preferences.ProxyPort), 8080, 8888)]
        [InlineData(nameof(Preferences.ProxyPeerConnections), false, true)]
        [InlineData(nameof(Preferences.ForceProxy), true, false)]
        [InlineData(nameof(Preferences.ProxyTorrentsOnly), false, true)]
        [InlineData(nameof(Preferences.ProxyUsername), "", "testuser")]
        [InlineData(nameof(Preferences.ProxyPassword), "", "testpassword")]
        [InlineData(nameof(Preferences.ProxyBittorrent), true, false)]
        [InlineData(nameof(Preferences.ProxyMisc), true, false)]
        [InlineData(nameof(Preferences.ProxyRss), true, false)]
        [InlineData(nameof(Preferences.IpFilterEnabled), false, true)]
        [InlineData(nameof(Preferences.IpFilterPath), "", "/tmp/ipfilter.dat")]
        [InlineData(nameof(Preferences.IpFilterTrackers), false, true)]
        [InlineData(nameof(Preferences.WebUIUpnp), nameof(GetPreferenceDefault), false)]
        [InlineData(nameof(Preferences.WebUIClickjackingProtection), true, false)]
        [InlineData(nameof(Preferences.WebUICsrfProtection), true, false)]
        [InlineData(nameof(Preferences.WebUIHostHeaderValidation), true, false)]
        [InlineData(nameof(Preferences.DynamicDnsEnabled), false, true)]
        [InlineData(nameof(Preferences.DynamicDnsService), DynamicDnsService.DynDNS, DynamicDnsService.NoIP)]
        [InlineData(nameof(Preferences.DynamicDnsDomain), "changeme.dyndns.org", "test.example.com")]
        [InlineData(nameof(Preferences.DynamicDnsUsername), "", "testuser")]
        [InlineData(nameof(Preferences.DynamicDnsPassword), "", "testpassword")]
        [InlineData(nameof(Preferences.BannedIpAddresses), new string[0],
            new[] { "192.168.254.201", "2001:db8::ff00:42:8329" })]
        [InlineData(nameof(Preferences.AdditinalTrackers), new string[0],
            new[] { "http://test1.example.com", "http://test2.example.com" })]
        // API 2.2
        [InlineData(nameof(Preferences.CreateTorrentSubfolder), true, false)]
        [InlineData(nameof(Preferences.AddTorrentPaused), false, true)]
        [InlineData(nameof(Preferences.TorrentFileAutoDeleteMode), TorrentFileAutoDeleteMode.Never, TorrentFileAutoDeleteMode.Always)]
        [InlineData(nameof(Preferences.TorrentFileAutoDeleteMode), TorrentFileAutoDeleteMode.Never, TorrentFileAutoDeleteMode.IfAdded)]
        [InlineData(nameof(Preferences.AutoTMMEnabledByDefault), false, true)]
        [InlineData(nameof(Preferences.AutoTMMRetainedWhenCategoryChanges), true, false)]
        [InlineData(nameof(Preferences.AutoTMMRetainedWhenCategorySavePathChanges), false, true)]
        [InlineData(nameof(Preferences.AutoTMMRetainedWhenDefaultSavePathChanges), false, true)]
        [InlineData(nameof(Preferences.MailNotificationSender), "qBittorrent_notification@example.com", "test@example.com")]
        [InlineData(nameof(Preferences.SlowTorrentDownloadRateThreshold), 2, 50)]
        [InlineData(nameof(Preferences.SlowTorrentUploadRateThreshold), 2, 50)]
        [InlineData(nameof(Preferences.SlowTorrentInactiveTime), 60, 120)]
        [InlineData(nameof(Preferences.AlternativeWebUIEnabled), false, true)]
        [InlineData(nameof(Preferences.AlternativeWebUIPath), "", "/tmp/alt-ui")]
        // API 2.3
        [InlineData(nameof(Preferences.WebUISessionTimeout), 3600, 7200)]
        [InlineData(nameof(Preferences.ListenOnIPv6Address), false, true)]
        [InlineData(nameof(Preferences.SaveResumeDataInterval), 60, 30)]
        [InlineData(nameof(Preferences.RecheckCompletedTorrents), false, true)]
        [InlineData(nameof(Preferences.ResolvePeerCountries), true, false)]
        [InlineData(nameof(Preferences.LibtorrentDiskCacheExpiryInterval), 60, 30)]
        [InlineData(nameof(Preferences.LibtorrentUseOSCache), true, false)]
        [InlineData(nameof(Preferences.LibtorrentCoalesceReadsAndWrites), false, true)]
        [InlineData(nameof(Preferences.LibtorrentSendUploadPieceSuggestions), false, true)]
        [InlineData(nameof(Preferences.LibtorrentSendBufferWatermark), 500, 400)]
        [InlineData(nameof(Preferences.LibtorrentSendBufferLowWatermark), 10, 20)]
        [InlineData(nameof(Preferences.LibtorrentSendBufferWatermarkFactor), 50, 75)]
        [InlineData(nameof(Preferences.LibtorrentSocketBacklogSize), 30, 20)]
        [InlineData(nameof(Preferences.LibtorrentOutgoingPortsMin), 0, 20_000)]
        [InlineData(nameof(Preferences.LibtorrentOutgoingPortsMax), 0, 40_000)]
        [InlineData(nameof(Preferences.LibtorrentUtpTcpMixedModeAlgorithm), UtpTcpMixedModeAlgorithm.PreferTcp, UtpTcpMixedModeAlgorithm.PeerProportional)]
        [InlineData(nameof(Preferences.LibtorrentAllowMultipleConnectionsFromSameIp), false, true)]
        [InlineData(nameof(Preferences.LibtorrentEnableEmbeddedTracker), false, true)]
        [InlineData(nameof(Preferences.LibtorrentEmbeddedTrackerPort), 9000, 10000)]
        [InlineData(nameof(Preferences.LibtorrentUploadSlotsBehavior), ChokingAlgorithm.FixedSlots, ChokingAlgorithm.RateBased)]
        [InlineData(nameof(Preferences.LibtorrentUploadChokingAlgorithm), SeedChokingAlgorithm.FastestUpload, SeedChokingAlgorithm.AntiLeech)]
        [InlineData(nameof(Preferences.LibtorrentStrictSuperSeeding), false, true)]
        [InlineData(nameof(Preferences.LibtorrentAnnounceToAllTrackers), false, true)]
        [InlineData(nameof(Preferences.LibtorrentAnnounceToAllTiers), true, false)]
        [InlineData(nameof(Preferences.LibtorrentAnnounceIp), "", "127.0.0.1")]
        // API 2.4.1
        [InlineData(nameof(Preferences.LibtorrentPieceExtentAffinity), false, true)]
        [InlineData(nameof(Preferences.WebUISecureCookie), true, false)]
        [InlineData(nameof(Preferences.WebUIMaxAuthenticationFailures), 5, 3)]
        [InlineData(nameof(Preferences.WebUIBanDuration), 3600, 1800)]
        [InlineData(nameof(Preferences.LibtorrentStopTrackerTimeout), nameof(GetPreferenceDefault), 10)]
        // API 2.5.1
        [InlineData(nameof(Preferences.WebUICustomHttpHeadersEnabled), false, true)]
        [InlineData(nameof(Preferences.WebUICustomHttpHeaders),
            new string[] { },
            new[] { "X-FORWARDED-HOST:localhost", "X-FORWARDED-PROTO:HTTP" })]
        [InlineData(nameof(Preferences.RssDownloadRepackProperEpisodes), true, false)]
        [InlineData(nameof(Preferences.RssSmartEpisodeFilters),
            new[] { @"s(\d+)e(\d+)", @"(\d+)x(\d+)", @"(\d{4}[.\-]\d{1,2}[.\-]\d{1,2})", @"(\d{1,2}[.\-]\d{1,2}[.\-]\d{4})" },
            new[] { @"s(\d+)e(\d+)", @"(\d{4}[.\-]\d{1,3}[.\-]\d{1,3})" })]
        // API 2.6.0
        [InlineData(nameof(Preferences.LibtorrentMaxConcurrentHttpAnnounces), 50, 30)]
        // API 2.7.0
        [InlineData(nameof(Preferences.TorrentContentLayout), TorrentContentLayout.Original, TorrentContentLayout.Subfolder)]
        [PrintTestName]
        public async Task SetPreference(string name, object oldValue, object newValue,
            string[] ignoredProperties = null)
        {
            var prop = typeof(Preferences).GetProperty(name);
            ignoredProperties ??= Array.Empty<string>();

            var apiLevelAttr = prop.GetCustomAttribute<ApiLevelAttribute>();
            if (apiLevelAttr != null && apiLevelAttr.Level >= ApiLevel.V2)
            {
                var minVersion = string.IsNullOrEmpty(apiLevelAttr.MinVersion)
                    ? new ApiVersion(2)
                    : ApiVersion.Parse(apiLevelAttr.MinVersion);
                Skip.If(DockerFixture.Env.ApiVersion < minVersion, $"API version is less than {minVersion}.");

                if (!string.IsNullOrEmpty(apiLevelAttr.MaxVersion))
                {
                    var maxVersion = ApiVersion.Parse(apiLevelAttr.MaxVersion);
                    Skip.If(DockerFixture.Env.ApiVersion > maxVersion, $"API version is greater than {maxVersion}.");
                }
            }

            var deprecatedAttr = prop.GetCustomAttribute<DeprecatedAttribute>();
            if (deprecatedAttr != null)
            {
                var deprecatedFromVersion = ApiVersion.Parse(deprecatedAttr.FromVersion);
                Skip.If(DockerFixture.Env.ApiVersion >= deprecatedFromVersion, $"Deprecated starting from API {deprecatedAttr.FromVersion}");
            }

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            prop.GetValue(oldPrefs).Should().BeEquivalentTo(oldValue.Equals(nameof(GetPreferenceDefault)) ? GetPreferenceDefault(name) : oldValue);

            var setPrefs = new Preferences();
            prop.SetValue(setPrefs, newValue);
            await Utils.Retry(() => Client.SetPreferencesAsync(setPrefs));

            await Utils.Retry(async () =>
            {
                var newPrefs = await Client.GetPreferencesAsync();
                prop.GetValue(newPrefs).Should().BeEquivalentTo(newValue);
                newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                    .Excluding(ctx => ctx.SelectedMemberPath == name)
                    .Excluding(ctx => ignoredProperties.Contains(ctx.SelectedMemberPath)));
            });
        }

        private object GetPreferenceDefault(string name) => name switch 
        {
            nameof(Preferences.LibtorrentStopTrackerTimeout) => ApiVersionLessThan(2, 9) ? 5 : 2,
            nameof(Preferences.ProxyAddress) => ApiVersionLessThan(2, 9) ? "0.0.0.0" : "",
            nameof(Preferences.ProxyType) => ApiVersionLessThan(2, 9) ? 0 : ProxyType.None,
            nameof(Preferences.WebUIUpnp) => ApiVersionLessThan(2, 9) ? true : false,
            _ => throw new NotImplementedException(name)
        };

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceScanDir()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.ScanDirectories.Should().BeEmpty();

            var setPrefs = new Preferences
            {
                ScanDirectories = new Dictionary<string, SaveLocation>
                {
                    ["/scan/1"] = new SaveLocation("/root/Downloads/from_scan1"),
                    ["/scan/2"] = new SaveLocation(StandardSaveLocation.Default),
                    ["/scan/3"] = new SaveLocation(StandardSaveLocation.MonitoredFolder)
                }
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.ScanDirectories.Should().BeEquivalentTo(setPrefs.ScanDirectories);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.ScanDirectories));
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceMaxRatio()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.MaxRatio.Should().Be(-1.0);
            oldPrefs.MaxRatioEnabled.Should().BeFalse();

            var setPrefs = new Preferences
            {
                MaxRatioEnabled = true,
                MaxRatio = 1.0
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.MaxRatio.Should().Be(1.0);
            newPrefs.MaxRatioEnabled.Should().BeTrue();
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.MaxRatio)
                .Excluding(p => p.MaxRatioEnabled));
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceMaxSeedingTime()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.MaxSeedingTime.Should().Be(-1);
            oldPrefs.MaxSeedingTimeEnabled.Should().BeFalse();

            var setPrefs = new Preferences
            {
                MaxSeedingTimeEnabled = true,
                MaxSeedingTime = 600
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.MaxSeedingTime.Should().Be(600);
            newPrefs.MaxSeedingTimeEnabled.Should().BeTrue();
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.MaxSeedingTime)
                .Excluding(p => p.MaxSeedingTimeEnabled));
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceSchedule()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.ScheduleFromHour.Should().Be(8);
            oldPrefs.ScheduleFromMinute.Should().Be(0);
            oldPrefs.ScheduleToHour.Should().Be(20);
            oldPrefs.ScheduleToMinute.Should().Be(0);
            oldPrefs.SchedulerEnabled.Should().BeFalse();

            var setPrefs = new Preferences
            {
                ScheduleFromHour = 7,
                ScheduleFromMinute = 45,
                ScheduleToHour = 21,
                ScheduleToMinute = 15,
                SchedulerEnabled = true
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.ScheduleFromHour.Should().Be(7);
            newPrefs.ScheduleFromMinute.Should().Be(45);
            newPrefs.ScheduleToHour.Should().Be(21);
            newPrefs.ScheduleToMinute.Should().Be(15);
            newPrefs.SchedulerEnabled.Should().BeTrue();
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(ctx => ctx.SelectedMemberPath.StartsWith("Schedule")));
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceBypassAuthentication()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.BypassLocalAuthentication.Should().BeFalse();
            oldPrefs.BypassAuthenticationSubnetWhitelistEnabled.Should().BeFalse();
            oldPrefs.BypassAuthenticationSubnetWhitelist.Should().BeEmpty();

            var setPrefs = new Preferences
            {
                BypassLocalAuthentication = true,
                BypassAuthenticationSubnetWhitelistEnabled = true,
                BypassAuthenticationSubnetWhitelist = GetNetworks().ToList()
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newClient = new QBittorrentClient(new Uri("http://localhost:8080/"));

            try
            {
                var newPrefs = await newClient.GetPreferencesAsync();
                newPrefs.BypassLocalAuthentication.Should().BeTrue();
                newPrefs.BypassAuthenticationSubnetWhitelistEnabled.Should().BeTrue();
                newPrefs.BypassAuthenticationSubnetWhitelist.Should()
                    .BeEquivalentTo(setPrefs.BypassAuthenticationSubnetWhitelist);
                newPrefs.Should().BeEquivalentTo(newPrefs, options => options
                    .Excluding(p => p.BypassLocalAuthentication)
                    .Excluding(p => p.BypassAuthenticationSubnetWhitelist)
                    .Excluding(p => p.BypassAuthenticationSubnetWhitelistEnabled));
            }
            catch (QBittorrentClientRequestException ex) when (ApiVersionMoreThan(2, 5))
            {
                ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
            

            IEnumerable<string> GetNetworks()
            {
                var addresses =
                    from ni in NetworkInterface.GetAllNetworkInterfaces()
                    where ni.OperationalStatus == OperationalStatus.Up
                    from unicast in ni.GetIPProperties().UnicastAddresses
                    where unicast.Address.AddressFamily == AddressFamily.InterNetwork
                    select IPNetwork.Parse(unicast.Address, unicast.IPv4Mask).ToString();
                return addresses.Distinct();
            }
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceWebUICredentials()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIUsername.Should().Be(UserName);
            oldPrefs.WebUIPasswordHash.Should().Be(ApiVersionLessThan(2, 3) ? Hash(Password) : null);

            var setPrefs = new Preferences
            {
                WebUIUsername = "testuser",
                WebUIPassword = "testpassword"
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newClient = new QBittorrentClient(new Uri("http://localhost:8080/"));
            await newClient.LoginAsync("testuser", "testpassword");

            var newPrefs = await newClient.GetPreferencesAsync();
            newPrefs.WebUIUsername.Should().Be(setPrefs.WebUIUsername);
            newPrefs.WebUIPasswordHash.Should().Be(ApiVersionLessThan(2, 3) ? Hash(setPrefs.WebUIPassword) : null);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.WebUIUsername)
                .Excluding(p => p.WebUIPasswordHash));

            string Hash(string password)
            {
                using (var md5 = MD5.Create())
                {
                    return string.Concat(md5.ComputeHash(Encoding.ASCII.GetBytes(password))
                        .Select(b => b.ToString("x2")));
                }
            }
        }

        [Fact]
        [PrintTestName]
        public async Task SetPreferenceWebUIDomainAndPort()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIDomain.Should().Be("*");
            oldPrefs.WebUIPort.Should().Be(8080);

            var setPrefs = new Preferences
            {
                WebUIDomain = "localhost",
                WebUIPort = 9090
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newClient = new QBittorrentClient(new Uri("http://localhost:9090/"));
            await newClient.LoginAsync(UserName, Password);

            var newPrefs = await newClient.GetPreferencesAsync();
            newPrefs.WebUIDomain.Should().Be(setPrefs.WebUIDomain);
            newPrefs.WebUIPort.Should().Be(setPrefs.WebUIPort);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.WebUIDomain)
                .Excluding(p => p.WebUIPort));
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceWebUIHttps()
        {
            Skip.IfNot(ApiVersionLessThan(2, 3), "QBittorrent 4.2.0 has changed the procedure of setting certificate.");

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIHttps.Should().BeFalse();
            oldPrefs.WebUISslCertificate.Should().BeEmpty();
            oldPrefs.WebUISslKey.Should().BeEmpty();
            oldPrefs.WebUISslCertificatePath.Should().BeNull();
            oldPrefs.WebUISslKeyPath.Should().BeNull();

            var (cert, key) = CreateCertificate();

            var setPrefs = new Preferences
            {
                WebUIHttps = true,
                WebUISslCertificate = cert,
                WebUISslKey = key,
                WebUIPort = 9090
            };
            await Client.SetPreferencesAsync(setPrefs);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = delegate { return true; }
            };
            var newClient = new QBittorrentClient(new Uri("https://localhost:9090/"), handler, true);
            await newClient.LoginAsync(UserName, Password);

            var newPrefs = await newClient.GetPreferencesAsync();
            newPrefs.WebUIHttps.Should().BeTrue();
            newPrefs.WebUISslCertificate.Should().Be(setPrefs.WebUISslCertificate);
            newPrefs.WebUISslKey.Should().Be(setPrefs.WebUISslKey);
            newPrefs.WebUIPort.Should().Be(setPrefs.WebUIPort);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.WebUIHttps)
                .Excluding(p => p.WebUISslCertificate)
                .Excluding(p => p.WebUISslKey)
                .Excluding(p => p.WebUIPort));
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceWebUIHttps_2_3()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIHttps.Should().BeFalse();
            oldPrefs.WebUISslCertificate.Should().BeNull();
            oldPrefs.WebUISslKey.Should().BeNull();
            oldPrefs.WebUISslCertificatePath.Should().BeEmpty();
            oldPrefs.WebUISslKeyPath.Should().BeEmpty();

            var setPrefs = new Preferences
            {
                WebUIHttps = true,
                WebUISslCertificatePath = "/cert/cert.pem",
                WebUISslKeyPath = "/cert/key.pem",
                WebUIPort = 9090
            };
            await Client.SetPreferencesAsync(setPrefs);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = delegate { return true; }
            };
            var newClient = new QBittorrentClient(new Uri("https://localhost:9090/"), handler, true);
            await newClient.LoginAsync(UserName, Password);

            var newPrefs = await newClient.GetPreferencesAsync();
            newPrefs.WebUIHttps.Should().BeTrue();
            newPrefs.WebUISslCertificatePath.Should().Be(setPrefs.WebUISslCertificatePath);
            newPrefs.WebUISslKeyPath.Should().Be(setPrefs.WebUISslKeyPath);
            newPrefs.WebUIPort.Should().Be(setPrefs.WebUIPort);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.WebUIHttps)
                .Excluding(p => p.WebUISslCertificatePath)
                .Excluding(p => p.WebUISslKeyPath)
                .Excluding(p => p.WebUIPort));
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceWebUIHttpsWithInvalidCertificate()
        {
            Skip.IfNot(ApiVersionLessThan(2, 3), "QBittorrent 4.2.0 has changed the procedure of setting certificate.");

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIHttps.Should().BeFalse();
            oldPrefs.WebUISslCertificate.Should().BeEmpty();
            oldPrefs.WebUISslKey.Should().BeEmpty(); 
            oldPrefs.WebUISslCertificatePath.Should().BeNull();
            oldPrefs.WebUISslKeyPath.Should().BeNull();

            var (cert, key) = CreateCertificate();

            var setPrefs = new Preferences
            {
                WebUIHttps = true,
                WebUISslCertificate = cert,
                WebUISslKey = key,
                WebUIPort = 9090
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newClient = new QBittorrentClient(new Uri("https://localhost:9090/"));
            await Assert.ThrowsAsync<HttpRequestException>(() => newClient.LoginAsync(UserName, Password));
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceWebUIHttpsWithInvalidCertificate_2_3()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.WebUIHttps.Should().BeFalse();
            oldPrefs.WebUISslCertificate.Should().BeNull();
            oldPrefs.WebUISslKey.Should().BeNull();
            oldPrefs.WebUISslCertificatePath.Should().BeEmpty();
            oldPrefs.WebUISslKeyPath.Should().BeEmpty();


            var setPrefs = new Preferences
            {
                WebUIHttps = true,
                WebUISslCertificatePath = "/cert/cert.pem",
                WebUISslKeyPath = "/cert/key.pem",
                WebUIPort = 9090
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newClient = new QBittorrentClient(new Uri("https://localhost:9090/"));
            await Assert.ThrowsAsync<HttpRequestException>(() => newClient.LoginAsync(UserName, Password));
        }

        private (string cert, string key) CreateCertificate()
        {
            var secureRandom = new SecureRandom(new CryptoApiRandomGenerator());

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(secureRandom, 1024));
            var keyPair = keyPairGenerator.GenerateKeyPair();

            var certGenerator = new X509V3CertificateGenerator();

            var certName = new X509Name("CN=localhost");
            var serialNo = BigInteger.ProbablePrime(120, new Random());

            certGenerator.SetSerialNumber(serialNo);
            certGenerator.SetSubjectDN(certName);
            certGenerator.SetIssuerDN(certName);
            certGenerator.SetNotAfter(DateTime.Now.AddYears(100));
            certGenerator.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
            certGenerator.SetPublicKey(keyPair.Public);

            certGenerator.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public),
                    new GeneralNames(new GeneralName(certName)),
                    serialNo));

            certGenerator.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id,
                false,
                new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", keyPair.Private, secureRandom);
            var newCert = certGenerator.Generate(signatureFactory);
            return (WritePem(newCert), WritePem(keyPair.Private));

            string WritePem<T>(T data)
            {
                var stringWriter = new StringWriter();
                var pemWriter = new PemWriter(stringWriter);
                pemWriter.WriteObject(data);
                return stringWriter.ToString();
            }
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceCurrentNetworkInterfaceAndAddress()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.CurrentNetworkInterface.Should().BeEmpty();
            oldPrefs.CurrentInterfaceAddress.Should().BeEmpty();

            var iface = "eth0";
            var address = (await Client.GetNetworkInterfaceAddressesAsync(iface)).Single();

            var setPrefs = new Preferences
            {
                CurrentNetworkInterface = iface,
                CurrentInterfaceAddress = address
            };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.CurrentNetworkInterface.Should().Be(iface);
            newPrefs.CurrentInterfaceAddress.Should().Be(address);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(p => p.CurrentNetworkInterface)
                .Excluding(p => p.CurrentInterfaceAddress));
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceLimitLAN()
        {
            Skip.If(ApiVersionLessThan(2, 2));
            Skip.If(ApiVersionMoreThan(2, 3), "Impossible to test on API > 2.3.");

            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.LimitLAN.Should().BeFalse();

            var setPrefs = new Preferences { LimitLAN = true };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.LimitLAN.Should().Be(true);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceLibtorrentAsynchronousIOThreads()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 6) ? 4 : 10;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.LibtorrentAsynchronousIOThreads.Should().Be(initial);

            var setPrefs = new Preferences { LibtorrentAsynchronousIOThreads = 8 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.LibtorrentAsynchronousIOThreads.Should().Be(8);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceLibtorrentFilePoolSize()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 8) ? 40 : ApiVersionLessThan(2, 9) ? 5000 : 100;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.LibtorrentFilePoolSize.Should().Be(initial);

            var setPrefs = new Preferences { LibtorrentFilePoolSize = 8 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.LibtorrentFilePoolSize.Should().Be(8);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceLibtorrentDiskCache()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 6) ? 64 : -1;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.LibtorrentDiskCache.Should().Be(initial);

            var setPrefs = new Preferences { LibtorrentDiskCache = 32 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.LibtorrentDiskCache.Should().Be(32);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceLibtorrentOutstandingMemoryWhenCheckingTorrent()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 6) ? 16 : 32;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.LibtorrentOutstandingMemoryWhenCheckingTorrent.Should().Be(initial);

            var setPrefs = new Preferences { LibtorrentOutstandingMemoryWhenCheckingTorrent = 64 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.LibtorrentOutstandingMemoryWhenCheckingTorrent.Should().Be(64);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceMaxUploads()
        {
            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 6) ? -1 : 20;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.MaxUploads.Should().Be(initial);

            var setPrefs = new Preferences { MaxUploads = 10 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.MaxUploads.Should().Be(10);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceMaxUploadsPerTorrent()
        {
            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 6) ? -1 : 4;

            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.MaxUploadsPerTorrent.Should().Be(initial);

            var setPrefs = new Preferences { MaxUploadsPerTorrent = 5 };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.MaxUploadsPerTorrent.Should().Be(5);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceSavePath()
        {
            await Client.LoginAsync(UserName, Password);

            var oldPrefs = await Client.GetPreferencesAsync();
            Path.TrimEndingDirectorySeparator(oldPrefs.SavePath).Should().Be("/downloads");

            var setPrefs = new Preferences { SavePath = "/tmp" };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            Path.TrimEndingDirectorySeparator(newPrefs.SavePath).Should().Be("/tmp");
        }

        [SkippableFact]
        [PrintTestName]
        public async Task SetPreferenceTempPath()
        {
            await Client.LoginAsync(UserName, Password);

            var initial = ApiVersionLessThan(2, 8) || ApiVersionMoreThan(2, 9)
                ? "/downloads/temp"
                : "/home/qbittorrent/Downloads/temp";

            var oldPrefs = await Client.GetPreferencesAsync();
            Path.TrimEndingDirectorySeparator(oldPrefs.TempPath).Should().Be(initial);

            var setPrefs = new Preferences { TempPath = "/tmp" };
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            Path.TrimEndingDirectorySeparator(newPrefs.TempPath).Should().Be("/tmp");
        }

        #endregion

        #region Network Interfaces

        [SkippableFact]
        [PrintTestName]
        public async Task GetNetworkInterfaces()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var interfaces = await Client.GetNetworkInterfacesAsync();
            interfaces.Should().BeEquivalentTo(
                new NetInterface { Id = "lo", Name = "lo" },
                new NetInterface { Id = "eth0", Name = "eth0"});
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetNetworkInterfaceAddressesForAll()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var addresses = await Client.GetNetworkInterfaceAddressesAsync();
            addresses.Should().HaveCount(2);
            addresses.Should().Contain("127.0.0.1");
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetNetworkInterfaceAddressesForLoopback()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var addresses = await Client.GetNetworkInterfaceAddressesAsync("lo");
            addresses.Should().HaveCount(1);
            addresses.Should().Contain("127.0.0.1");
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetNetworkInterfaceAddressesForEth()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var addresses = await Client.GetNetworkInterfaceAddressesAsync(new NetInterface { Id = "eth0", Name = "eth0" });
            addresses.Should().HaveCount(1);
            addresses.Should().NotContain("127.0.0.1");
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetNetworkInterfaceAddressesForUnknown()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var addresses = await Client.GetNetworkInterfaceAddressesAsync("fake");
            addresses.Should().BeEmpty();
        }

        #endregion

        #region Peers

        [SkippableFact]
        public async Task AddTorrentPeers()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);
            var hash = torrent.OriginalInfoHash.ToLower();

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var origProps = await Client.GetTorrentPropertiesAsync(hash);
            origProps.TotalPeers.Should().Be(0);

            var result = await Client.AddTorrentPeerAsync(hash, "127.0.0.1:12345");
            result.Should().NotBe(0);
            result.Added.Should().Be(1);
            result.Failed.Should().Be(0);

            await Utils.Retry(async () =>
            {
                var newProps = await Client.GetTorrentPropertiesAsync(hash);
                newProps.TotalPeers.Should().Be(1);
            });
        }

        [SkippableFact]
        public async Task BanPeers()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);
            var oldPrefs = await Client.GetPreferencesAsync();
            oldPrefs.BannedIpAddresses.Should().BeEmpty();

            await Client.BanPeerAsync("192.168.253.1:12345");

            var newPrefs = await Client.GetPreferencesAsync();
            newPrefs.BannedIpAddresses.Should().Contain("192.168.253.1");
            newPrefs.Should().BeEquivalentTo(oldPrefs, opt => opt.Excluding(x => x.BannedIpAddresses));
        }

        #endregion

        #region Tags

        [SkippableFact]
        public async Task TagsCrud()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var tags = await Client.GetTagsAsync();
            tags.Should().BeEmpty();

            await Client.CreateTagsAsync(new[] {"Tag 1", "Tag 2"});
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2");

            await Client.CreateTagsAsync(new[] { "Tag 2", "Tag 3", "Tag 4" });
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 4");

            await Client.DeleteTagsAsync(new[] {"Tag 5", "Tag 4", "Tag 2"});
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 3");

            await Client.DeleteTagsAsync(tags);
            tags = await Client.GetTagsAsync();
            tags.Should().BeEmpty();
        }

        [SkippableFact]
        public async Task TorrentTagsCrud()
        {
            Skip.If(ApiVersionLessThan(2, 3));

            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);
            var hash = torrent.OriginalInfoHash.ToLower();
            var hashes = new[] { hash };

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            var tags = await Client.GetTagsAsync();
            tags.Should().BeEmpty();

            int responseId = 0;

            await Client.CreateTagsAsync(new[] { "Tag 1", "Tag 2", "Tag 3", "Tag 4" });
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 4");
            var syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeEquivalentTo(tags);
            syncData.TagsRemoved.Should().BeNullOrEmpty();
            responseId = syncData.ResponseId;

            await Client.AddTorrentTagsAsync(hashes, new [] { "Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6" });
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            var torrentTags = (await Client.GetTorrentListAsync()).Single().Tags;
            torrentTags.Should().BeEquivalentTo("Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeEquivalentTo("Tag 5", "Tag 6");
            syncData.TagsRemoved.Should().BeNullOrEmpty();
            syncData.TorrentsChanged[hash].Tags.Should().BeEquivalentTo("Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            responseId = syncData.ResponseId;

            await Client.AddTorrentTagAsync(hash, "Tag 6");
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            torrentTags = (await Client.GetTorrentListAsync()).Single().Tags;
            torrentTags.Should().BeEquivalentTo("Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeNullOrEmpty();
            syncData.TagsRemoved.Should().BeNullOrEmpty();
            if (syncData.TorrentsChanged != null && syncData.TorrentsChanged.TryGetValue(hash, out var torrentPartialInfo))
            {
                torrentPartialInfo.Tags.Should().BeNullOrEmpty();
            }
            responseId = syncData.ResponseId;

            await Client.DeleteTorrentTagsAsync(hashes, new[] {"Tag 2", "Tag 6"});
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 4", "Tag 5", "Tag 6");
            torrentTags = (await Client.GetTorrentListAsync()).Single().Tags;
            torrentTags.Should().BeEquivalentTo("Tag 3", "Tag 4", "Tag 5");
            syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeNullOrEmpty();
            syncData.TagsRemoved.Should().BeNullOrEmpty();
            syncData.TorrentsChanged[hash].Tags.Should().BeEquivalentTo("Tag 3", "Tag 4", "Tag 5");
            responseId = syncData.ResponseId;

            await Client.DeleteTagsAsync(new[] {"Tag 4"});
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 5", "Tag 6");
            torrentTags = (await Client.GetTorrentListAsync()).Single().Tags;
            torrentTags.Should().BeEquivalentTo("Tag 3", "Tag 5");
            syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeNullOrEmpty();
            syncData.TagsRemoved.Should().BeEquivalentTo("Tag 4");
            syncData.TorrentsChanged[hash].Tags.Should().BeEquivalentTo("Tag 3", "Tag 5");
            responseId = syncData.ResponseId;

            await Client.ClearTorrentTagsAsync();
            tags = await Client.GetTagsAsync();
            tags.Should().BeEquivalentTo("Tag 1", "Tag 2", "Tag 3", "Tag 5", "Tag 6");
            torrentTags = (await Client.GetTorrentListAsync()).Single().Tags;
            torrentTags.Should().BeEmpty();
            syncData = await Client.GetPartialDataAsync(responseId);
            syncData.TagsAdded.Should().BeNullOrEmpty();
            syncData.TagsRemoved.Should().BeNullOrEmpty();
            syncData.TorrentsChanged[hash].Tags.Should().BeNullOrEmpty();
        }

        #endregion

        #region RenameFile

        [SkippableFact]
        [PrintTestName]
        public async Task RenameFileByIndex()
        {
            Skip.If(ApiVersionLessThan(2, 4));
            Skip.If(ApiVersionMoreThan(2, 6, 2), "Impossible to test on API > 2.6.2");

            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(1);

                var content = contents.Single();
                content.Name.Should().Be(torrent.File.FileName);
                content.Size.Should().Be(torrent.File.FileSize);
            });

            var newName = Guid.NewGuid().ToString("N");
            await Client.RenameFileAsync(torrent.OriginalInfoHash.ToLower(), 0, newName);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(1);

                var content = contents.Single();
                content.Name.Should().Be(newName);
                content.Size.Should().Be(torrent.File.FileSize);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task RenameFileByName()
        {
            Skip.If(ApiVersionLessThan(2, 8));

            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(1);

                var content = contents.Single();
                content.Name.Should().Be(torrent.File.FileName);
                content.Size.Should().Be(torrent.File.FileSize);
            });

            var newName = Guid.NewGuid().ToString("N");
            await Client.RenameFileAsync(torrent.OriginalInfoHash.ToLower(), torrent.File.FileName, newName);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(1);

                var content = contents.Single();
                content.Name.Should().Be(newName);
                content.Size.Should().Be(torrent.File.FileSize);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task RenameFolderByName()
        {
            Skip.If(ApiVersionLessThan(2, 8));

            await Client.LoginAsync(UserName, Password);

            var torrentPath = Path.Combine(Utils.TorrentsFolder, "ubuntu-14.04-pack.torrent");
            var parser = new BencodeParser();
            var torrent = parser.Parse<Torrent>(torrentPath);

            var addRequest = new AddTorrentFilesRequest(torrentPath) { Paused = true };
            await Client.AddTorrentsAsync(addRequest);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(2);

                contents.Select(x => x.Name).Should().BeEquivalentTo(
                    torrent.Files.Select(x => $"{torrent.Files.DirectoryName}/{x.FileName}"));
            });

            var newName = Guid.NewGuid().ToString("N");
            await Client.RenameFolderAsync(torrent.OriginalInfoHash.ToLower(), torrent.Files.DirectoryName, newName);

            await Utils.Retry(async () =>
            {
                var contents = await Client.GetTorrentContentsAsync(torrent.OriginalInfoHash.ToLower());
                contents.Should().NotBeNull();
                contents.Should().HaveCount(2);

                contents.Select(x => x.Name).Should().BeEquivalentTo(
                    torrent.Files.Select(x => $"{newName}/{x.FileName}"));
            });
        }

        #endregion

        #region RSS

        [SkippableFact]
        [PrintTestName]
        public async Task AddRssFeed()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"));

            var items = await Client.GetRssItemsAsync();
            items.Folders.Should().BeEmpty();
            items.Feeds.Should().HaveCount(1);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AddRssFolder()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.AddRssFolderAsync("Test");

            var items = await Client.GetRssItemsAsync();
            items.Feeds.Should().BeEmpty();
            items.Folders.Should().HaveCount(1);
            items.Folders.Single().Name.Should().Be("Test");
        }

        
        [SkippableFact]
        [PrintTestName]
        public async Task AddRssFeedWithFolder()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.AddRssFolderAsync("Test");
            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"), "Test\\Ubuntu");

            var items = await Client.GetRssItemsAsync();
            items.Folders.Should().HaveCount(1);
            items.Folders.Single().Name.Should().Be("Test");
            items.Feeds.Should().BeEmpty();
        }
        
        [SkippableFact]
        [PrintTestName]
        public async Task AddAndProcessRssItems()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.SetPreferencesAsync(new Preferences {RssProcessingEnabled = true});
            await Task.Delay(1000);
            
            await Client.AddRssFolderAsync("Test folder");
            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"), "Ubuntu");
            await Client.AddRssFeedAsync(new Uri("file:///rss/rutracker.rss"), "Test folder\\Rutracker");
            await Task.Delay(1000);
            
            var items = await Client.GetRssItemsAsync(true);
            var expected = new RssFolder(
                new RssItem[]
                {
                    new RssFeed
                    {
                        Name = "Ubuntu",
                        Title = "Ubuntu downloads",
                        Uid = Guid.Parse("{80858e34-897e-48bb-bfe3-ed0fec030861}"),
                        Url = new Uri("http://example.com/ubuntu.rss"),
                        LastBuildDate = new DateTimeOffset(2018, 11, 29, 9, 34, 01, TimeSpan.FromHours(-5)),
                        IsLoading = false,
                        HasError = false,
                        Articles = new List<RssArticle>
                        {
                            new RssArticle
                            {
                                Date = new DateTimeOffset(2018, 11, 29, 10, 56, 14, TimeSpan.Zero),
                                Id = "http://releases.ubuntu.com/16.04/ubuntu-16.04.4-desktop-amd64.iso.torrent",
                                Link = new Uri("https://www.ubuntu.com/download/desktop"),
                                Title = "Ubuntu 16.04.4",
                                TorrentUri = new Uri("https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-16.04.4-desktop-amd64.iso.torrent"),
                                IsRead = false,
                                AdditionalData = new Dictionary<string, JToken>
                                {
                                    ["fileName"] = "ubuntu-16.04.4-desktop-amd64.iso",
                                    ["infoHash"] = "778CE280B595E57780FF083F2EB6F897DFA4A4EE",
                                    ["magnetURI"] = "magnet:?xt=urn:btih:778ce280b595e57780ff083f2eb6f897dfa4a4ee&dn=ubuntu-16.04.4-desktop-amd64.iso&tr=http://torrent.ubuntu.com:6969/announce&tr=http://ipv6.torrent.ubuntu.com:6969/announce",
                                    ["peers"] = 0,
                                    ["seeds"] = 0,
                                    ["verified"] = 0
                                }
                            },
                            new RssArticle
                            {
                                Date = new DateTimeOffset(2018, 11, 29, 11, 52, 49, TimeSpan.Zero),
                                Id = "http://releases.ubuntu.com/17.10/ubuntu-17.10.1-desktop-amd64.iso.torrent",
                                Link = new Uri("https://www.ubuntu.com/download/desktop"),
                                Title = "Ubuntu 17.10.1",
                                TorrentUri = new Uri("https://fedarovich.blob.core.windows.net/qbittorrent-test/torrents/ubuntu-17.10.1-desktop-amd64.iso.torrent"),
                                IsRead = false,
                                AdditionalData = new Dictionary<string, JToken>
                                {
                                    ["fileName"] = "ubuntu-17.10.1-desktop-amd64.iso",
                                    ["infoHash"] = "F07E0B0584745B7BCB35E98097488D34E68623D0",
                                    ["magnetURI"] = "magnet:?xt=urn:btih:f07e0b0584745b7bcb35e98097488d34e68623d0&dn=ubuntu-17.10.1-desktop-amd64.iso&tr=http://torrent.ubuntu.com:6969/announce&tr=http://ipv6.torrent.ubuntu.com:6969/announce9",
                                    ["peers"] = 0,
                                    ["seeds"] = 0,
                                    ["verified"] = 0
                                }
                            }
                        }
                    }, 
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed
                            {
                                Name = "Rutracker",
                                Title = "Rutracker feed",
                                Uid = Guid.Parse("{d4c0a303-7aeb-4199-997c-1ca420501b56}"),
                                Url = new Uri("http://example.com/rutracker.rss"),
                                LastBuildDate = null,
                                IsLoading = false,
                                HasError = false,
                                Articles = new List<RssArticle>
                                {
                                    new RssArticle
                                    {
                                        Date = new DateTimeOffset(2018, 11, 18, 19, 56, 41, TimeSpan.Zero),
                                        Id = "428325a2b3b846759bf7b1089b42bfa2",
                                        Title = "Ubuntu 14.04",
                                        TorrentUri = new Uri("magnet:?xt=urn:btih:9fadb1dcf775fe9a88a7ff0e8150b979d3803398&dn=ubuntu-pack-14.04-unity&tr=http://bt.t-ru.org/ann&tr=http://retracker.local/announce"),
                                        IsRead = false,
                                        Description = "Ubuntu 14.04 i386 and AMD64",
                                        AdditionalData = new Dictionary<string, JToken>
                                        {
                                            ["info_hash"] = "8B0D024760C412F5E0DCBCFAB59B267CBDD895C6",
                                            ["raw_title"] = "ubuntu-pack-14.04-unity"
                                        }
                                    }
                                }
                            }
                        })
                }
            );

            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".AdditionalData"))
            );
        }
        
        [SkippableFact]
        [PrintTestName]
        public async Task DeleteRssItems()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.AddRssFolderAsync("Test folder");
            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"), "Ubuntu");
            await Client.AddRssFeedAsync(new Uri("file:///rss/rutracker.rss"), "Test folder\\Rutracker");
            
            var items = await Client.GetRssItemsAsync();
            var expected = new RssFolder(
                new RssItem[]
                {
                    new RssFeed { Name = "Ubuntu" },
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Rutracker" }
                        })
                }
            );

            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );

            // Delete "Ubuntu"
            
            await Client.DeleteRssItemAsync("Ubuntu");
            
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder(
                new RssItem[]
                {
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Rutracker" }
                        })
                }
            );
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
            
            // Delete "Test folder\Rutracker"
            
            await Client.DeleteRssItemAsync("Test folder\\Rutracker");
            
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder(
                new RssItem[]
                {
                    new RssFolder("Test folder", null)
                }
            );
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
            
            // Delete "Test folder"
            
            await Client.DeleteRssItemAsync("Test folder");
            
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder();
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
        }
        
        [SkippableFact]
        [PrintTestName]
        public async Task MoveRssItems()
        {
            Skip.If(ApiVersionLessThan(2, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.AddRssFolderAsync("Test folder");
            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"), "Ubuntu");
            await Client.AddRssFeedAsync(new Uri("file:///rss/rutracker.rss"), "Test folder\\Rutracker");
            
            var items = await Client.GetRssItemsAsync();
            var expected = new RssFolder(
                new RssItem[]
                {
                    new RssFeed { Name = "Ubuntu" },
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Rutracker" }
                        })
                }
            );

            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );

            // Move "Ubuntu" to "Test folder\Ubuntu"
            
            await Client.MoveRssItemAsync("Ubuntu", "Test folder\\Ubuntu");
            
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder(
                new RssItem[]
                {
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Rutracker" },
                            new RssFeed { Name = "Ubuntu" },
                        })
                }
            );
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
            
            // Rename "Test folder\Rutracker" to "Test folder\Rutracker Ubuntu"
            
            await Client.MoveRssItemAsync("Test folder\\Rutracker","Test folder\\Rutracker Ubuntu");
            
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder(
                new RssItem[]
                {
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Rutracker Ubuntu" },
                            new RssFeed { Name = "Ubuntu" },
                        })
                }
            );
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
            
            // Move and rename "Test folder\Rutracker Ubuntu" to "Rutracker"
            
            await Client.MoveRssItemAsync("Test folder\\Rutracker Ubuntu","Rutracker");
            
            items = await Client.GetRssItemsAsync();
            items = await Client.GetRssItemsAsync();
            expected = new RssFolder(
                new RssItem[]
                {
                    new RssFeed { Name = "Rutracker" },
                    new RssFolder("Test folder",
                        new RssItem[]
                        {
                            new RssFeed { Name = "Ubuntu" },
                        })
                }
            );
            
            items.Should().BeEquivalentTo(expected, cfg => cfg
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Url"))
                .Excluding(m => m.SelectedMemberPath.EndsWith(".Uid"))
            );
        }

        [SkippableFact]
        [PrintTestName]
        public async Task AddRssAutoDownloadingRule()
        {
            Skip.If(ApiVersionLessThan(2, 1));
            
            await Client.LoginAsync(UserName, Password);

            var rule = new RssAutoDownloadingRule()
            {
                AssignedCategory = "Linux",
                MustContain = "Ubuntu",
                AddPaused =  true,
                LastMatch = new DateTimeOffset(2019, 1, 16, 17, 45, 0, TimeSpan.FromHours(3))
            };

            await Client.SetRssAutoDownloadingRuleAsync("Ubuntu rule", rule);

            var rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().HaveCount(1);
            rules["Ubuntu rule"].Should().BeEquivalentTo(rule, cfg => cfg.Excluding(r => r.AdditionalData));
        }
        
        [SkippableFact]
        [PrintTestName]
        public async Task UpdateRssAutoDownloadingRule()
        {
            Skip.If(ApiVersionLessThan(2, 1));
            
            await Client.LoginAsync(UserName, Password);

            var rule = new RssAutoDownloadingRule()
            {
                AssignedCategory = "Linux",
                MustContain = "KUbuntu",
                AddPaused =  true
            };

            await Client.SetRssAutoDownloadingRuleAsync("Ubuntu rule", rule);

            var rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().HaveCount(1);
            rules["Ubuntu rule"].Should().BeEquivalentTo(rule, cfg => cfg.Excluding(r => r.AdditionalData));

            rule.MustContain = "Ubuntu";
            
            await Client.SetRssAutoDownloadingRuleAsync("Ubuntu rule", rule);

            rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().HaveCount(1);
            rules["Ubuntu rule"].Should().BeEquivalentTo(rule, cfg => cfg.Excluding(r => r.AdditionalData));
        }
        
        [SkippableFact(Skip = "This functionality does not work in current versions of qBittorrent")]
        [PrintTestName]
        public async Task RenameRssAutoDownloadingRule()
        {
            Skip.If(ApiVersionLessThan(2, 1));
            
            await Client.LoginAsync(UserName, Password);

            var rule = new RssAutoDownloadingRule()
            {
                AssignedCategory = "Linux",
                MustContain = "KUbuntu",
                AddPaused =  true
            };

            await Client.SetRssAutoDownloadingRuleAsync("Ubuntu rule", rule);

            var rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().HaveCount(1);
            rules.Single().Key.Should().Be("Ubuntu rule");

            await Client.RenameRssAutoDownloadingRuleAsync("Ubuntu rule", "KUbuntu rule");

            await Utils.Retry(async () =>
            {
                rules = await Client.GetRssAutoDownloadingRulesAsync();
                rules.Should().HaveCount(1);
                rules.Single().Key.Should().Be("KUbuntu rule");
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task DeleteRssAutoDownloadingRule()
        {
            Skip.If(ApiVersionLessThan(2, 1));
            
            await Client.LoginAsync(UserName, Password);

            var rule = new RssAutoDownloadingRule()
            {
                AssignedCategory = "Linux",
                MustContain = "Ubuntu",
                AddPaused =  true
            };

            await Client.SetRssAutoDownloadingRuleAsync("Ubuntu rule", rule);

            var rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().HaveCount(1);
            rules["Ubuntu rule"].Should().BeEquivalentTo(rule, cfg => cfg.Excluding(r => r.AdditionalData));

            await Client.DeleteRssAutoDownloadingRuleAsync("Ubuntu rule");
            
            rules = await Client.GetRssAutoDownloadingRulesAsync();
            rules.Should().BeEmpty();
        }

        [SkippableFact]
        [PrintTestName]
        public async Task GetMatchingArticles()
        {
            Skip.If(true, "TODO: Find a way to test it.");
            Skip.If(ApiVersionLessThan(2, 5, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.SetPreferencesAsync(new Preferences { RssProcessingEnabled = true, RssAutoDownloadingEnabled = true });
            await Task.Delay(1000);

            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"));
            await Task.Delay(1000);

            var rule = new RssAutoDownloadingRule()
            {
                MustContain = "Ubuntu"
            };

            var ruleName = "linux";
            await Client.SetRssAutoDownloadingRuleAsync(ruleName, rule);

            await Task.Delay(1000);

            var rules = await Client.GetMatchingRssArticlesAsync(ruleName);
            rules.Should().NotBeNullOrEmpty();
        }

        [SkippableFact]
        [PrintTestName]
        public async Task MarkArticleAsRead()
        {
            Skip.If(ApiVersionLessThan(2, 5, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.SetPreferencesAsync(new Preferences { RssProcessingEnabled = true });
            await Task.Delay(1000);

            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"));
            await Client.AddRssFeedAsync(new Uri("file:///rss/rutracker.rss"));
            await Task.Delay(1000);

            var items = await Client.GetRssItemsAsync(true);
            items.Feeds
                .SelectMany(f => f.Articles, (_, a) => a.IsRead)
                .Should()
                .AllBeEquivalentTo(false);

            var feedPath = "Ubuntu downloads";
            var articleId = "http://releases.ubuntu.com/17.10/ubuntu-17.10.1-desktop-amd64.iso.torrent";

            await Client.MarkRssItemAsReadAsync(feedPath, articleId);
            items = await Client.GetRssItemsAsync(true);
            items.Feeds
                .SelectMany(f => f.Articles)
                .Where(a => a.Id == articleId)
                .Select(a => a.IsRead)
                .Should().AllBeEquivalentTo(true);
            items.Feeds
                .SelectMany(f => f.Articles)
                .Where(a => a.Id != articleId)
                .Select(a => a.IsRead)
                .Should().AllBeEquivalentTo(false);
        }

        [SkippableFact]
        [PrintTestName]
        public async Task MarkFeedAsRead()
        {
            Skip.If(ApiVersionLessThan(2, 5, 1));

            await Client.LoginAsync(UserName, Password);

            await Client.SetPreferencesAsync(new Preferences { RssProcessingEnabled = true });
            await Task.Delay(1000);

            await Client.AddRssFeedAsync(new Uri("file:///rss/ubuntu.rss"));
            await Client.AddRssFeedAsync(new Uri("file:///rss/rutracker.rss"));
            await Task.Delay(1000);

            var items = await Client.GetRssItemsAsync(true);
            items.Feeds
                .SelectMany(f => f.Articles, (_, a) => a.IsRead)
                .Should()
                .AllBeEquivalentTo(false);

            var feedPath = "Ubuntu downloads";

            await Client.MarkRssItemAsReadAsync(feedPath);
            items = await Client.GetRssItemsAsync(true);
            items.Feeds
                .Where(f => f.Title == feedPath)
                .SelectMany(x => x.Articles, (_, a) => a.IsRead)
                .Should().AllBeEquivalentTo(true);
            items.Feeds
                .Where(f => f.Title != feedPath)
                .SelectMany(f => f.Articles, (_, a) => a.IsRead)
                .Should().AllBeEquivalentTo(false);
        }

        #endregion

        #region Search

        [SkippableFact]
        [PrintTestName]
        public async Task ManageSearchPlugins()
        {
            Skip.If(ApiVersionLessThan(2, 1, 1));

            await Client.LoginAsync(UserName, Password);

            var plugins = await Client.GetSearchPluginsAsync();
            plugins.Should().BeEmpty();

            // TODO: It would be better to use a custom mock plugin here.
            await Client.InstallSearchPluginAsync(
                new Uri("https://raw.githubusercontent.com/MadeOfMagicAndWires/qBit-plugins/730482f630bb7a5bab69fbc7ad1b42bda55c144c/engines/linuxtracker.py"));

            var plugin = await Utils.Retry(async () =>
            {
                plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().HaveCount(1);
                return plugins.Single();
            }, attempts: 10);

            var categories = ApiVersionLessThan(2, 6)
                ? new[] {new SearchPluginCategory("software")}
                : new[]
                {
                    new SearchPluginCategory("all", "All categories"),
                    new SearchPluginCategory("software", "Software"),
                };

            plugin.Should().BeEquivalentTo(
                new SearchPlugin
                {
                    Name = "linuxtracker",
                    FullName = "Linux Tracker",
                    IsEnabled = true,
                    Url = new Uri("http://linuxtracker.org/"),
                    Version = new System.Version(1, 1),
                    Categories = categories,
#pragma warning disable 618
                    SupportedCategories = categories.Select(c => c.Id).ToArray()
#pragma warning restore 618
                }, 
                cfg => cfg.Excluding(s => s.AdditionalData));

            await Client.DisableSearchPluginAsync(plugin.Name);
            await Utils.Retry(async () =>
            {
                plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().HaveCount(1);
                plugins.Single().IsEnabled.Should().BeFalse();
            });

            await Client.EnableSearchPluginAsync(plugin.Name);
            await Utils.Retry(async () =>
            {
                plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().HaveCount(1);
                plugins.Single().IsEnabled.Should().BeTrue();
            });

            await Client.UninstallSearchPluginAsync(plugin.Name);
            await Utils.Retry(async () =>
            {
                plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().BeEmpty();
            });

            await Client.UpdateSearchPluginsAsync();
            await Utils.Retry(async () =>
            {
                plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().HaveCountGreaterThan(1);
            });
        }

        [SkippableFact]
        [PrintTestName]
        public async Task Search()
        {
#warning Fix the failing test.
            Skip.If(true, "The test is always failing now. We must use a custom mock plugin to prevent failure.");
            Skip.If(ApiVersionLessThan(2, 1, 1));

            await Client.LoginAsync(UserName, Password);

            // TODO: It would be better to use a custom mock plugin here.
            await Client.InstallSearchPluginAsync(
                new Uri("https://raw.githubusercontent.com/MadeOfMagicAndWires/qBit-plugins/730482f630bb7a5bab69fbc7ad1b42bda55c144c/engines/linuxtracker.py"));

            var plugin = await Utils.Retry(async () =>
            {
                var plugins = await Client.GetSearchPluginsAsync();
                plugins.Should().HaveCount(1);
                return plugins.Single();
            });

            var ubuntuSearch = await Client.StartSearchAsync("Ubuntu", true);
            var fedoraSearch = await Client.StartSearchAsync("Fedora", plugin.Name);

            var statuses = await Client.GetSearchStatusAsync();
            // TODO: Fix the failure on the next line
            statuses.Select(s => s.Status).Should().AllBeEquivalentTo(SearchJobStatus.Running);

            await Client.StopSearchAsync(fedoraSearch);
            var fedoraStatus = await Client.GetSearchStatusAsync(fedoraSearch);
            fedoraStatus.Status.Should().Be(SearchJobStatus.Stopped);

            // Wait for ubuntu search to complete for about 1 minute:
            var ubuntuStatus = await Utils.Retry(async () =>
            {
                var status = await Client.GetSearchStatusAsync(ubuntuSearch);
                status.Status.Should().Be(SearchJobStatus.Stopped);
                return status;
            }, attempts: 21, delayMs: 3000);

            var results = await Client.GetSearchResultsAsync(ubuntuSearch, 0, 10);
            results.Results.Should().HaveCount(10);
            results.Status.Should().Be(SearchJobStatus.Stopped);
            results.Total.Should().Be(ubuntuStatus.Total);

            await Client.DeleteSearchAsync(ubuntuSearch);
            await Utils.Retry(async () =>
            {
                var st = await Client.GetSearchStatusAsync();
                st.Should().HaveCount(1);
            });
        }

        #endregion

        private bool ApiVersionLessThan(byte major, byte minor = 0, byte build = 0)
        {
            return DockerFixture.Env.ApiVersion < new ApiVersion(major, minor, build);
        }

        private bool ApiVersionMoreThan(byte major, byte minor = 0, byte build = 0)
        {
            return DockerFixture.Env.ApiVersion > new ApiVersion(major, minor, build);
        }

        private bool ApiVersionIs(byte major, byte minor = 0, byte build = 0)
        {
            return DockerFixture.Env.ApiVersion == new ApiVersion(major, minor, build);
        }
    }
}