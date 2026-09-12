// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using Newtonsoft.Json;

namespace ServerInfo
{
    // The Thunderstore package manifest.json shape — Hexium publishes
    // mods under the same namespace/name convention, so this isn't
    // Thunderstore-exclusive despite the file's origin
    internal class PackageManifest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version_number")]
        public string VersionNumber { get; set; }

        [JsonProperty("website_url")]
        public string WebsiteUrl { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dependencies")]
        public string[] Dependencies { get; set; }

        // Not in the file — filled in from the package folder name (see Find)
        public string Namespace { get; set; }
    }

    internal class PackageManifestReader
    {
        private readonly PluginLog log;

        // GetOrAdd also caches a null result, so a plugin with no
        // manifest.json isn't re-scanned on every request
        private readonly ConcurrentDictionary<string, PackageManifest> cache =
            new ConcurrentDictionary<string, PackageManifest>();

        public PackageManifestReader(PluginLog log)
        {
            this.log = log;
        }

        // <Namespace>-<Name>-<Version>, or just <Namespace>-<Name> when a
        // mod manager updates the package in place. Namespace/name never
        // contain hyphens, so splitting on the first one is unambiguous
        private static readonly Regex PackageFolderWithVersion =
            new Regex(@"^(?<namespace>[^-]+)-(?<name>.+)-(?<version>\d[\d.]*(?:-[\w.]+)?)$");
        private static readonly Regex PackageFolderNoVersion =
            new Regex(@"^(?<namespace>[^-]+)-(?<name>.+)$");

        internal static string ParseNamespace(string folderName)
        {
            folderName = folderName ?? "";
            var match = PackageFolderWithVersion.Match(folderName);
            if (!match.Success)
            {
                match = PackageFolderNoVersion.Match(folderName);
            }
            return match.Success ? match.Groups["namespace"].Value : null;
        }

        // manifest.json sits next to the plugin DLL, or one folder up if
        // the author nested the DLL inside the package
        public PackageManifest Find(PluginInfo pi)
        {
            return cache.GetOrAdd(pi.Metadata.GUID, _ =>
            {
                string dllPath = pi.Location;
                if (string.IsNullOrEmpty(dllPath))
                {
                    return null;
                }

                string dllDir = Path.GetDirectoryName(dllPath);
                string parentDir = dllDir != null ? Path.GetDirectoryName(dllDir) : null;

                foreach (var dir in new[] { dllDir, parentDir })
                {
                    if (dir == null)
                    {
                        continue;
                    }

                    string manifestPath = Path.Combine(dir, "manifest.json");
                    if (!File.Exists(manifestPath))
                    {
                        continue;
                    }

                    try
                    {
                        string text = File.ReadAllText(manifestPath);
                        var manifest = JsonConvert.DeserializeObject<PackageManifest>(text);
                        if (manifest != null)
                        {
                            manifest.Namespace = ParseNamespace(Path.GetFileName(dir));
                        }
                        return manifest;
                    }
                    catch (Exception ex)
                    {
                        log.Warning($"Unable to parse manifest.json for {pi.Metadata.Name}: {ex.Message}");
                        return null;
                    }
                }

                return null;
            });
        }
    }
}
