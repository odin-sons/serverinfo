// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using ServerInfo;
using Xunit;

namespace ServerInfo.Tests
{
    public class AssemblyMetadataReaderTests
    {
        [Fact]
        public void GetDescription_ReturnsNullForNullAssembly()
        {
            Assert.Null(AssemblyMetadataReader.GetDescription(null));
        }

        [Fact]
        public void GetRepositoryUrl_ReturnsNullForNullAssembly()
        {
            Assert.Null(AssemblyMetadataReader.GetRepositoryUrl(null));
        }

        [Fact]
        public void GetDescription_ReturnsNullWhenAssemblyHasNoDescription()
        {
            // ServerInfo.dll itself carries an empty AssemblyDescription —
            // empty counts as "no value", same as a mod with no metadata at all
            var serverInfoAssembly = typeof(GameReflection).Assembly;
            Assert.Null(AssemblyMetadataReader.GetDescription(serverInfoAssembly));
        }

        [Fact]
        public void GetRepositoryUrl_ReturnsNullWhenAssemblyHasNoRepositoryUrlMetadata()
        {
            var serverInfoAssembly = typeof(GameReflection).Assembly;
            Assert.Null(AssemblyMetadataReader.GetRepositoryUrl(serverInfoAssembly));
        }
    }
}
