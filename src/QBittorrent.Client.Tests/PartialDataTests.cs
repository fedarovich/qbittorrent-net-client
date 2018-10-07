using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

#pragma warning disable 618

namespace QBittorrent.Client.Tests
{
    public class PartialDataTests
    {
        [Fact]
        public void LegacyCategories()
        {
            string data = @"{
                ""rid"" : 1,
                ""categories"" : [""cat1"", ""cat2""]
            }";

            var partialData = JsonConvert.DeserializeObject<PartialData>(data);
            partialData.CategoriesAdded.Should().BeEquivalentTo("cat1", "cat2");
            partialData.CategoriesChanged.Should().BeEquivalentTo(
                new Dictionary<string, Category>
                {
                    ["cat1"] = new Category {Name = "cat1", SavePath = ""},
                    ["cat2"] = new Category {Name = "cat2", SavePath = ""}
                });
        }

        [Fact]
        public void ModernCategories()
        {
            string data = @"{
                ""rid"" : 1,
                ""categories"" : {
                    ""cat1"" : {""name"" : ""cat1"", ""savePath"" : """"}, 
                    ""cat2"" : {""name"" : ""cat2"", ""savePath"" : ""/home/test/cat2""}, 
                }
            }";

            var partialData = JsonConvert.DeserializeObject<PartialData>(data);
            partialData.CategoriesAdded.Should().BeNull();
            partialData.CategoriesChanged.Should().BeEquivalentTo(
                new Dictionary<string, Category>
                {
                    ["cat1"] = new Category { Name = "cat1", SavePath = "" },
                    ["cat2"] = new Category { Name = "cat2", SavePath = "/home/test/cat2" }
                });
        }
    }
}
