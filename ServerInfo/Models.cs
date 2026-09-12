// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

namespace ServerInfo
{
    internal class PlayerInfo
    {
        public string Name { get; set; }
        public string SteamID { get; set; }
        public string AvatarUrl { get; set; }
    }

    internal class ModDetails
    {
        public string name { get; set; }
        public string guid { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string websiteUrl { get; set; }
        public string[] dependencies { get; set; }

        // packageName is the manifest's own "name" field, not the same
        // value as `name` above (BepInEx's display name) — see README
        public string @namespace { get; set; }
        public string packageName { get; set; }
    }

    internal class ServerInfoSnapshot
    {
        public string Name { get; set; }
        public int PlayersCount { get; set; }
        public PlayerInfo[] Players { get; set; }
        public ModDetails[] Mods { get; set; }
    }
}
