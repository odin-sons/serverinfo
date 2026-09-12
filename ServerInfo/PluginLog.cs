// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Odin_Sons <https://github.com/odin-sons/serverinfo>

using System;
using BepInEx.Logging;

namespace ServerInfo
{
    // Gates our own log calls by the configured LogLevel, so a server admin
    // who only wants Error/Warning noise doesn't also get Info by default
    internal class PluginLog
    {
        private readonly ManualLogSource source;
        private readonly Func<LogLevel> enabledLevels;

        public PluginLog(ManualLogSource source, Func<LogLevel> enabledLevels)
        {
            this.source = source;
            this.enabledLevels = enabledLevels;
        }

        public void Error(string message)
        {
            if (enabledLevels().HasFlag(LogLevel.Error))
            {
                source.LogError(message);
            }
        }

        public void Warning(string message)
        {
            if (enabledLevels().HasFlag(LogLevel.Warning))
            {
                source.LogWarning(message);
            }
        }

        public void Info(string message)
        {
            if (enabledLevels().HasFlag(LogLevel.Info))
            {
                source.LogInfo(message);
            }
        }
    }
}
