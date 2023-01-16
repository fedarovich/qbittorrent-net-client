using System;
using FluentAssertions;
using FluentAssertions.Execution;
using QBittorrent.Client.Extensions;
using Xunit;

namespace QBittorrent.Client.Tests
{
    public class UriExtensionsTests
    {
        [Theory]
        [InlineData("http://example.com", "http://example.com")]
        [InlineData("http://example.com/", "http://example.com/")]
        [InlineData("http://example.com/a", "http://example.com:80/a/")]
        [InlineData("http://example.com/a/", "http://example.com/a/")]
        public void EnsureTrailingSlash(string original, string expected)
        {
            var originalUri = new Uri(original);
            var actualUri = originalUri.EnsureTrailingSlash();
            using (new AssertionScope())
            {
                actualUri.OriginalString.Should().Be(expected);
                if (original == expected)
                {
                    actualUri.Should().BeSameAs(originalUri);
                }
            }
        }

        [Fact]
        public void WithQueryParametersNull()
        {
            var uri = new Uri("http://example.com/a/b/c/");
            var actual = uri.WithQueryParameters(null);
            actual.Should().BeSameAs(uri);
        }

        [Fact]
        public void WithQueryParametersEmpty()
        {
            var uri = new Uri("http://example.com/a/b/c/");
            var actual = uri.WithQueryParameters();
            actual.Should().BeSameAs(uri);
        }

        [Fact]
        public void WithQueryParametersSingle()
        {
            var uri = new Uri("http://example.com/a/b/c/");
            var actual = uri.WithQueryParameters(("testKey", "testValue"));
            actual.AbsoluteUri.Should().Be("http://example.com/a/b/c/?testKey=testValue");
        }

        [Fact]
        public void WithQueryParametersTwo()
        {
            var uri = new Uri("http://example.com/a/b/c/");
            var actual = uri.WithQueryParameters(("testKey", "testValue"), ("test key", "test value"));
            actual.AbsoluteUri.Should().Be("http://example.com/a/b/c/?testKey=testValue&test%20key=test%20value");
        }
    }
}
