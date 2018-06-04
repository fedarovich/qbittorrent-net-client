using System;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace QBittorrent.Client.Tests
{
    public class PreferencesTests
    {
        [Fact]
        public void SerializeToEmpty()
        {
            var prefs = new Preferences();
            var str = JsonConvert.SerializeObject(prefs);
            str.Should().Be("{}");
        }
    }
}
