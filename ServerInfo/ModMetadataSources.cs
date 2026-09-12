// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using System;
using System.Linq;

namespace ServerInfo
{
    internal enum ModMetadataSource
    {
        Manifest,
        Assembly,
    }

    // Parses the admin-facing "Manifest,Assembly" config string into an
    // ordered, de-duplicated list of sources to try in turn
    internal static class ModMetadataSources
    {
        public const string Default = "Manifest,Assembly";

        private static readonly ModMetadataSource[] DefaultOrder =
            { ModMetadataSource.Manifest, ModMetadataSource.Assembly };

        public static ModMetadataSource[] Parse(string value, PluginLog log)
        {
            var parsed = (value ?? "")
                .Split(',')
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .Select(token => Enum.TryParse(token, true, out ModMetadataSource source) ? (ModMetadataSource?)source : null)
                .Where(source => source.HasValue)
                .Select(source => source.Value)
                .Distinct()
                .ToArray();

            if (parsed.Length > 0)
            {
                return parsed;
            }

            log.Warning($"MetadataSources config value \"{value}\" has no recognized sources " +
                $"(expected Manifest and/or Assembly, comma-separated) — falling back to \"{Default}\".");
            return DefaultOrder;
        }
    }
}
