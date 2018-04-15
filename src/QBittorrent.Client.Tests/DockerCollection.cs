using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QBittorrent.Client.Tests
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class DockerCollection : ICollectionFixture<DockerFixture>
    {
        public const string Name = "Docker";
    }
}
