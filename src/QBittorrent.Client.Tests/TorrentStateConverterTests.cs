using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json;
using QBittorrent.Client.Converters;
using Xunit;

namespace QBittorrent.Client.Tests;

public class TorrentStateConverterTests
{
    [Theory]
    [MemberData(nameof(Values))]
    [InlineData("stoppedDL", TorrentState.PausedDownload)]
    [InlineData("stoppedUP", TorrentState.PausedUpload)]
    public void ReadJson(string stringValue, TorrentState expectedState)
    {
        var json = $"{{\"TorrentState\": \"{stringValue}\"}}";
        var testData = JsonConvert.DeserializeObject<TestData>(json);
        testData.TorrentState.Should().Be(expectedState);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void WriteJson(string expectedString, TorrentState torrentState)
    {
        var testData = new TestData() { TorrentState = torrentState };
        var json = JsonConvert.SerializeObject(testData, Formatting.None);
        json.Should().Be($"{{\"TorrentState\":\"{expectedString}\"}}");
    }

    public static object[][] Values
    {
        get
        {
            return (
                from field in typeof(TorrentState).GetFields(BindingFlags.Static | BindingFlags.Public)
                let attribute = field.GetCustomAttribute<EnumMemberAttribute>()
                select new [] { attribute.Value, field.GetValue(null) }
            ).ToArray();
        }
    }

    private class TestData
    {
        [JsonConverter(typeof(TorrentStateConverter))]
        public TorrentState TorrentState { get; set; }
    }
}
