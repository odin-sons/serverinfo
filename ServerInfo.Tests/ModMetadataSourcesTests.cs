using BepInEx.Logging;
using ServerInfo;
using Xunit;

namespace ServerInfo.Tests
{
    public class ModMetadataSourcesTests
    {
        private static PluginLog TestLog() => new PluginLog(new ManualLogSource("test"), () => LogLevel.All);

        [Fact]
        public void Parse_DefaultOrder()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest, ModMetadataSource.Assembly },
                ModMetadataSources.Parse("Manifest,Assembly", TestLog()));
        }

        [Fact]
        public void Parse_ReversedOrder()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Assembly, ModMetadataSource.Manifest },
                ModMetadataSources.Parse("Assembly,Manifest", TestLog()));
        }

        [Fact]
        public void Parse_SingleSource()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest },
                ModMetadataSources.Parse("Manifest", TestLog()));
        }

        [Fact]
        public void Parse_IsCaseInsensitiveAndTrimsWhitespace()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Assembly },
                ModMetadataSources.Parse(" assembly ", TestLog()));
        }

        [Fact]
        public void Parse_DeduplicatesRepeatedValues()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest },
                ModMetadataSources.Parse("Manifest,Manifest", TestLog()));
        }

        [Fact]
        public void Parse_FallsBackToDefaultOnUnrecognizedValue()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest, ModMetadataSource.Assembly },
                ModMetadataSources.Parse("nonsense", TestLog()));
        }

        [Fact]
        public void Parse_FallsBackToDefaultOnEmpty()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest, ModMetadataSource.Assembly },
                ModMetadataSources.Parse("", TestLog()));
        }

        [Fact]
        public void Parse_FallsBackToDefaultOnNull()
        {
            Assert.Equal(
                new[] { ModMetadataSource.Manifest, ModMetadataSource.Assembly },
                ModMetadataSources.Parse(null, TestLog()));
        }
    }
}
