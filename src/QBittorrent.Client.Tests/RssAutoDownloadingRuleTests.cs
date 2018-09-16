using System;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace QBittorrent.Client.Tests
{
    public class RssAutoDownloadingRuleTests
    {
        [Fact]
        public void Deserialize()
        {
            const string ruleText = 
                @"{
                    ""enabled"": false,
                    ""mustContain"": ""The *Punisher*"",
                    ""mustNotContain"": """",
                    ""useRegex"": false,
                    ""episodeFilter"": ""1x01-;"",
                    ""smartFilter"": false,
                    ""previouslyMatchedEpisodes"": [
                    ],
                    ""affectedFeeds"": [
                        ""http://showrss.info/user/134567.rss?magnets=true""
                    ],
                    ""ignoreDays"": 1,
                    ""lastMatch"": ""20 Nov 2017 09:05:11"",
                    ""addPaused"": true,
                    ""assignedCategory"": """",
                    ""savePath"": ""C:/Users/JohnDoe/Downloads/Punisher""
                }";

            var settings = new JsonSerializerSettings
            {
                DateFormatString = "dd MMM yyyy HH:mm:ss"
            };

            var rule = JsonConvert.DeserializeObject<RssAutoDownloadingRule>(ruleText, settings);
            rule.Should().BeEquivalentTo(
                new RssAutoDownloadingRule
                {
                    Enabled = false,
                    MustContain = "The *Punisher*",
                    MustNotContain = "",
                    UseRegex = false,
                    EpisodeFilter = "1x01-;",
                    SmartFilter = false,
                    PreviouslyMatchedEpisodes = new string[0],
                    AffectedFeeds = new [] { new Uri("http://showrss.info/user/134567.rss?magnets=true") },
                    IgnoreDays = 1,
                    LastMatch = new DateTime(2017, 11, 20, 9, 5, 11),
                    AddPaused = true,
                    AssignedCategory = "",
                    SavePath = "C:/Users/JohnDoe/Downloads/Punisher"
                });
        }
    }
}
