// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using ServerInfo;
using Xunit;

namespace ServerInfo.Tests
{
    public class PackageManifestReaderTests
    {
        [Theory]
        [InlineData("shudnal-ConditionalConfigSync-1.0.4", "shudnal")]
        [InlineData("shudnal-ConditionalConfigSync", "shudnal")]
        [InlineData("denikson-BepInExPack_Valheim-5.4.2350", "denikson")]
        [InlineData("Odin_Sons-ServerInfo-2.2.0", "Odin_Sons")]
        [InlineData("SingleWord", null)]
        [InlineData("", null)]
        public void ParseNamespace_HandlesKnownFolderShapes(string folderName, string expectedNamespace)
        {
            Assert.Equal(expectedNamespace, PackageManifestReader.ParseNamespace(folderName));
        }

        [Fact]
        public void ParseNamespace_HandlesNullFolderName()
        {
            Assert.Null(PackageManifestReader.ParseNamespace(null));
        }
    }
}
