using System;
using System.Collections;
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
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace QBittorrent.Client.Tests
{
    [Collection(DockerCollection.Name)]
    public class QBittorrentClientTests : IAsyncLifetime, IDisposable
    {
        private const string UserName = "admin";
        private const string Password = "adminadmin";
        
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
                    await Utils.Retry(() => tempClient.LoginAsync(UserName, Password), delayMs: 5000);
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
                new ContainerStopParameters {WaitBeforeKillSeconds = 10u});
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
            var version = await Client.GetQBittorrentVersionAsync();
            version.Should().Be(DockerFixture.Env.QBittorrentVersion);
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
            await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
        }
        
        [Fact]
        [PrintTestName]
        public async Task NoLogin()
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
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

                await Assert.ThrowsAsync<HttpRequestException>(() => Client.GetTorrentListAsync());
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
            await Client.AddTorrentsAsync(addRequest);
            
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().HaveCount(filesToAdd.Length);
                list.Select(t => t.Hash).Should().BeEquivalentTo(hashes);
            });
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

            var links = new []
            {
                new Uri(
                    "http://releases.ubuntu.com/17.10/ubuntu-17.10.1-desktop-amd64.iso.torrent?_ga=2.234486046.1639235846.1523865053-1922367372.1523865053"),
                new Uri(
                    "http://releases.ubuntu.com/16.04/ubuntu-16.04.4-desktop-amd64.iso.torrent?_ga=2.234486046.1639235846.1523865053-1922367372.1523865053"),
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
            
            var addRequest = new AddTorrentFilesRequest(torrentPath) { CreateRootFolder = false, Paused = true };
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

                var trackerUrls = trackers.Select(t => t.Url.AbsoluteUri).ToList();
                trackerUrls.Should().BeEquivalentTo(torrent.Trackers.SelectMany(x => x));
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
            info.DhtNodes.Should().Be(0);
            info.DownloadedData.Should().Be(0);
            info.DownloadSpeed.Should().Be(0);
            info.DownloadSpeedLimit.Should().Be(0);
            info.UploadedData.Should().Be(0);
            info.UploadSpeed.Should().Be(0);
            info.UploadSpeedLimit.Should().Be(0);
        }

        #endregion
        
        #region GetPartialDataAsync/AddCategoryAsync/DeleteCategoryAsync/DeleteAsync

        [Fact]
        [PrintTestName]
        public async Task GetPartialData()
        {
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
            partialData.CategoriesRemoved.Should().BeNull();
            responseId = partialData.ResponseId;

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd.Skip(1)) {Paused = true});
            await Task.Delay(refreshInterval);

            partialData = await Client.GetPartialDataAsync(responseId);
            partialData.FullUpdate.Should().BeFalse();
            partialData.TorrentsChanged.Should().HaveCount(2);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[1]);
            partialData.TorrentsChanged.Should().Contain(p => p.Key.ToLower() == hashes[2]);
            partialData.TorrentsRemoved.Should().BeNull();
            partialData.CategoriesAdded.Should().BeNull();
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
            partialData.CategoriesRemoved.Should().BeEquivalentTo("b");
        }
        
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

            await Client.PauseAllAsync();
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

            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(filesToAdd) {Paused = true});
            await Task.Delay(1000);

            var list = await Client.GetTorrentListAsync();
            list.Should().OnlyContain(t => t.State == TorrentState.PausedDownload);

            await Client.ResumeAllAsync();
            await Utils.Retry(async () =>
            {
                list = await Client.GetTorrentListAsync();
                list.Should().OnlyContain(t => t.State != TorrentState.PausedDownload);
            });
        }

        #endregion

        #region SetTorrentCategoryAsync

        [Fact]
        [PrintTestName]
        public async Task SetTorrentCategory()
        {
            await Client.LoginAsync(UserName, Password);

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) {Paused = true});
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

            var file = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) {Paused = true, Category = "xyz"});
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
            
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(file) {Paused = true});
            await Task.Delay(1000);
            var list = await Client.GetTorrentListAsync();
            list.Should().ContainSingle();
            var torrent = list.Single();
            var hash = torrent.Hash;
            
            var (down, up, props) = await Utils.WhenAll(
                Client.GetTorrentDownloadLimitAsync(hash),
                Client.GetTorrentUploadLimitAsync(hash),
                Client.GetTorrentPropertiesAsync(hash));
            down.Should().Be(null);
            up.Should().Be(null);
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
                props.SavePath.Should().Be("/tmp/");
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
            trackers.Select(t => t.Url).Should().BeEquivalentTo(tracker1, tracker2);
            
            var newTracker = new Uri("http://retracker.mgts.by:80/announce");
            await Client.AddTrackerAsync(torrent.Hash, newTracker);

            await Utils.Retry(async () =>
            {
                trackers = await Client.GetTorrentTrackersAsync(torrent.Hash);
                trackers.Select(t => t.Url).Should().BeEquivalentTo(tracker1, tracker2, newTracker);
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
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

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
        
        [Fact]
        [PrintTestName]
        public async Task ForceStart()
        {
            await Client.LoginAsync(UserName, Password);
           
            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

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
        
        [Fact]
        [PrintTestName]
        public async Task SuperSeeding()
        {
            await Client.LoginAsync(UserName, Password);
           
            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd));

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
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd) {FirstLastPiecePrioritized = initial});

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
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PrintTestName]
        public async Task SequentialDownload(bool initial)
        {
            await Client.LoginAsync(UserName, Password);
           
            var fileToAdd = Path.Combine(Utils.TorrentsFolder, "ubuntu-16.04.4-desktop-amd64.iso.torrent");
            await Client.AddTorrentsAsync(new AddTorrentFilesRequest(fileToAdd) {SequentialDownload = initial});

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
        [InlineData(nameof(Preferences.SavePath), "/root/Downloads/", "/tmp/")]
        [InlineData(nameof(Preferences.TempPathEnabled), false, true)]
        [InlineData(nameof(Preferences.TempPath), "/root/Downloads/temp/", "/tmp/")]
        [InlineData(nameof(Preferences.ExportDirectory), "", "/tmp/")]
        [InlineData(nameof(Preferences.ExportDirectoryForFinished), "", "/tmp/")]
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
        [InlineData(nameof(Preferences.ListenPort), 8999, 8888)]
        [InlineData(nameof(Preferences.UpnpEnabled), true, false)]
        [InlineData(nameof(Preferences.RandomPort), false, true, new [] {nameof(Preferences.ListenPort)})]
        [InlineData(nameof(Preferences.DownloadLimit), 0, 40960)]
        [InlineData(nameof(Preferences.UploadLimit), 0, 40960)]
        [InlineData(nameof(Preferences.MaxConnections), 500, 600)]
        [InlineData(nameof(Preferences.MaxConnectionsPerTorrent), 100, 200)]
        [InlineData(nameof(Preferences.MaxUploads), -1, 10)]
        [InlineData(nameof(Preferences.MaxUploadsPerTorrent), -1, 5)]
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
        [InlineData(nameof(Preferences.ProxyType), default(ProxyType), ProxyType.Http)]
        [InlineData(nameof(Preferences.ProxyAddress), "0.0.0.0", "192.168.254.200")]
        [InlineData(nameof(Preferences.ProxyPort), 8080, 8888)]
        [InlineData(nameof(Preferences.ProxyPeerConnections), false, true)]
        [InlineData(nameof(Preferences.ForceProxy), true, false)]
        [InlineData(nameof(Preferences.ProxyUsername), "", "testuser")]
        [InlineData(nameof(Preferences.ProxyPassword), "", "testpassword")]
        [InlineData(nameof(Preferences.IpFilterEnabled), false, true)]
        [InlineData(nameof(Preferences.IpFilterPath), "", "/tmp/ipfilter.dat")]
        [InlineData(nameof(Preferences.IpFilterTrackers), false, true)]
        [InlineData(nameof(Preferences.WebUIUpnp), true, false)]
        [InlineData(nameof(Preferences.DynamicDnsEnabled), false, true)]
        [InlineData(nameof(Preferences.DynamicDnsService), DynamicDnsService.DynDNS, DynamicDnsService.NoIP)]
        [InlineData(nameof(Preferences.DynamicDnsDomain), "changeme.dyndns.org", "test.example.com")]
        [InlineData(nameof(Preferences.DynamicDnsUsername), "", "testuser")]
        [InlineData(nameof(Preferences.DynamicDnsPassword), "", "testpassword")]
        [InlineData(nameof(Preferences.BannedIpAddresses), new string[0], new [] {"192.168.254.201", "2001:db8::ff00:42:8329"})]
        [InlineData(nameof(Preferences.AdditinalTrackers), new string[0], new [] {"http://test1.example.com", "http://test2.example.com"})]
        [InlineData("", null, null, Skip = "Rider xunit runner issue")]
        [PrintTestName]
        public async Task SetPreference(string name, object oldValue, object newValue, 
            string[] ignoredProperties = null)
        {
            Skip.If(Environment.OSVersion.Platform == PlatformID.Win32NT);
            
            var prop = typeof(Preferences).GetProperty(name);
            ignoredProperties = ignoredProperties ?? new string[0];
            
            await Client.LoginAsync(UserName, Password);
            
            var oldPrefs = await Client.GetPreferencesAsync();
            prop.GetValue(oldPrefs).Should().BeEquivalentTo(oldValue);
          
            var setPrefs = new Preferences();
            prop.SetValue(setPrefs, newValue);
            await Client.SetPreferencesAsync(setPrefs);

            var newPrefs = await Client.GetPreferencesAsync();
            prop.GetValue(newPrefs).Should().BeEquivalentTo(newValue);
            newPrefs.Should().BeEquivalentTo(oldPrefs, options => options
                .Excluding(ctx => ctx.SelectedMemberPath == name)
                .Excluding(ctx => ignoredProperties.Contains(ctx.SelectedMemberPath)));
        }

        [SkippableFact]
        public async Task SetPreferenceScanDir()
        {
            Skip.If(Environment.OSVersion.Platform == PlatformID.Win32NT);
            
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
        
        #endregion
    }
}
