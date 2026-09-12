// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using ServerInfo;
using Xunit;

namespace ServerInfo.Tests
{
    public class SteamAvatarServiceTests
    {
        [Theory]
        [InlineData("76561198000000000", true)]
        [InlineData("Steam_76561198000000000", false)]
        [InlineData("12345", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("7656119800000000A", false)]
        public void IsValidSteamID_ValidatesFormat(string id, bool expected)
        {
            Assert.Equal(expected, SteamAvatarService.IsValidSteamID(id));
        }

        [Theory]
        [InlineData("76561198000000000", "76561198000000000")]
        [InlineData("Steam_76561198000000000", "76561198000000000")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("garbage", "")]
        public void ExtractSteamID_HandlesKnownHostNameFormats(string hostName, string expected)
        {
            Assert.Equal(expected, SteamAvatarService.ExtractSteamID(hostName));
        }
    }
}
