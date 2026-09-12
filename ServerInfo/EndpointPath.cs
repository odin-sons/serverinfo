// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

namespace ServerInfo
{
    internal static class EndpointPath
    {
        public static string Normalize(string path)
        {
            path = string.IsNullOrWhiteSpace(path) ? "/serverinfo" : path.Trim().ToLowerInvariant();
            return path.StartsWith("/") ? path : "/" + path;
        }
    }
}
