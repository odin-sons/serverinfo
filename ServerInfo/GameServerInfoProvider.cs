// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;

namespace ServerInfo
{
    internal class GameServerInfoProvider
    {
        private readonly SteamAvatarService steamAvatars;
        private readonly PackageManifestReader manifestReader;
        private readonly Func<int> cacheIntervalSeconds;
        private readonly Func<ModMetadataSource[]> metadataSources;

        private ServerInfoSnapshot cache;
        private DateTime lastUpdate = DateTime.MinValue;

        public GameServerInfoProvider(SteamAvatarService steamAvatars, PackageManifestReader manifestReader,
            Func<int> cacheIntervalSeconds, Func<ModMetadataSource[]> metadataSources)
        {
            this.steamAvatars = steamAvatars;
            this.manifestReader = manifestReader;
            this.cacheIntervalSeconds = cacheIntervalSeconds;
            this.metadataSources = metadataSources;
        }

        // Null means ZNet isn't initialized yet
        public ServerInfoSnapshot GetSnapshot()
        {
            if (cache != null && (DateTime.Now - lastUpdate).TotalSeconds <= cacheIntervalSeconds())
            {
                return cache;
            }

            var znetType = GameReflection.FindType("ZNet");
            var zs = znetType != null ? GameReflection.GetStaticMember(znetType, "instance") : null;
            if (zs == null)
            {
                return null;
            }

            var playerInfos = BuildPlayerInfos(zs);

            cache = new ServerInfoSnapshot
            {
                Name = GameReflection.Invoke(zs, "GetWorldName") as string,
                PlayersCount = playerInfos.Count,
                Players = playerInfos.ToArray(),
                Mods = Chainloader.PluginInfos.Values.Select(BuildModDetails).ToArray(),
            };
            lastUpdate = DateTime.Now;

            return cache;
        }

        private List<PlayerInfo> BuildPlayerInfos(object zs)
        {
            var playerInfos = new List<PlayerInfo>();

            var peers = GameReflection.Invoke(zs, "GetConnectedPeers") as IEnumerable;
            if (peers != null)
            {
                foreach (var peer in peers)
                {
                    bool isReady = peer != null && (GameReflection.Invoke(peer, "IsReady") as bool? ?? false);
                    if (!isReady)
                    {
                        continue;
                    }

                    string name = GameReflection.GetMember(peer, "m_playerName") as string;
                    var socket = GameReflection.GetMember(peer, "m_socket");
                    string hostName = GameReflection.Invoke(socket, "GetHostName") as string;
                    string steamId = SteamAvatarService.ExtractSteamID(hostName);

                    playerInfos.Add(new PlayerInfo { Name = name, SteamID = steamId });
                }
            }

            var idsNeedingAvatar = playerInfos
                .Where(p => SteamAvatarService.IsValidSteamID(p.SteamID))
                .Select(p => p.SteamID);
            var avatarUrls = steamAvatars.GetAvatarUrls(idsNeedingAvatar);

            foreach (var player in playerInfos)
            {
                if (avatarUrls.TryGetValue(player.SteamID, out var url))
                {
                    player.AvatarUrl = url;
                }
            }

            return playerInfos;
        }

        private ModDetails BuildModDetails(PluginInfo pi)
        {
            var manifest = manifestReader.Find(pi);
            var assembly = pi.Instance?.GetType().Assembly;

            string description = null;
            string websiteUrl = null;
            string[] dependencies = null;

            foreach (var source in metadataSources())
            {
                if (source == ModMetadataSource.Manifest)
                {
                    description = Coalesce(description, manifest?.Description);
                    websiteUrl = Coalesce(websiteUrl, manifest?.WebsiteUrl);
                    dependencies = Coalesce(dependencies, manifest?.Dependencies);
                }
                else if (source == ModMetadataSource.Assembly)
                {
                    description = Coalesce(description, AssemblyMetadataReader.GetDescription(assembly));
                    websiteUrl = Coalesce(websiteUrl, AssemblyMetadataReader.GetRepositoryUrl(assembly));
                    dependencies = Coalesce(dependencies, pi.Dependencies?.Select(d => d.DependencyGUID).ToArray());
                }
            }

            return new ModDetails
            {
                name = pi.Metadata.Name,
                guid = pi.Metadata.GUID,
                version = pi.Metadata.Version?.ToString(),
                description = description,
                websiteUrl = websiteUrl,
                dependencies = dependencies,
                // Namespace/packageName are Thunderstore/Hexium package identity —
                // only a manifest.json has them, regardless of source order
                @namespace = manifest?.Namespace,
                packageName = manifest?.Name,
            };
        }

        private static string Coalesce(string current, string candidate) =>
            string.IsNullOrWhiteSpace(current) ? candidate : current;

        private static string[] Coalesce(string[] current, string[] candidate) =>
            (current == null || current.Length == 0) ? candidate : current;
    }
}
