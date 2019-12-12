using System;
using System.Net;
using FluentAssertions;
using Xunit;
using U = QBittorrent.Client.Internal.Utils;

namespace QBittorrent.Client.Tests
{
    public class UtilsTests
    {
        [Fact]
        public void ParseValidIp4Endpoint()
        {
            var endpoint = U.ParseIpEndpoint("127.0.0.1:8080");
            endpoint.Address.Should().Be(IPAddress.Loopback);
            endpoint.Port.Should().Be(8080);
        }

        [Fact]
        public void ParseValidIp6Endpoint()
        {
            var endpoint = U.ParseIpEndpoint("[::1]:8080");
            endpoint.Address.Should().Be(IPAddress.IPv6Loopback);
            endpoint.Port.Should().Be(8080);
        }

        [Fact]
        public void ParseIp4EndpointPort0()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("127.0.0.1:0"));
        }

        [Fact]
        public void ParseIp6EndpointPort0()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("[::1]:0"));
        }

        [Fact]
        public void ParseIp4EndpointNoPort()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("127.0.0.1"));
        }

        [Fact]
        public void ParseIp6EndpointNoPort()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("[::1]"));
        }

        [Fact]
        public void ParseIp4EndpointNoIp()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint(":8080"));
        }

        [Fact]
        public void ParseIp6EndpointNoIp()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("[]:8080"));
        }

        [Fact]
        public void ParseIp6EndpointNoBrakets()
        {
            Assert.Throws<FormatException>(() => U.ParseIpEndpoint("::1:8080"));
        }
    }
}
