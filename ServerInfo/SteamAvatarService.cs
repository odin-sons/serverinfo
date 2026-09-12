using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace ServerInfo
{
    internal class SteamAvatarService
    {
        // GetPlayerSummaries accepts a comma-separated steamids list, up
        // to this many per call
        private const int MaxIdsPerRequest = 100;

        private static readonly HttpClient httpClient = new HttpClient();

        private readonly PluginLog log;
        private readonly Func<string> apiKey;
        private readonly Func<int> cacheMinutes;

        private readonly ConcurrentDictionary<string, (string Url, DateTime CachedAt)> cache =
            new ConcurrentDictionary<string, (string, DateTime)>();
        private DateTime lastPrune = DateTime.MinValue;
        private bool warnedMissingApiKey;

        public SteamAvatarService(PluginLog log, Func<string> apiKey, Func<int> cacheMinutes)
        {
            this.log = log;
            this.apiKey = apiKey;
            this.cacheMinutes = cacheMinutes;
        }

        public static string ExtractSteamID(string hostName)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                return "";
            }

            if (IsValidSteamID(hostName))
            {
                return hostName;
            }

            if (hostName.StartsWith("Steam_"))
            {
                return hostName.Substring(6);
            }

            return "";
        }

        public static bool IsValidSteamID(string id)
        {
            if (string.IsNullOrEmpty(id) || !long.TryParse(id, out _))
            {
                return false;
            }
            return id.Length == 17 && id.StartsWith("7656");
        }

        public Dictionary<string, string> GetAvatarUrls(IEnumerable<string> steamIds)
        {
            Prune();

            var ids = steamIds.Distinct().ToArray();
            var result = new Dictionary<string, string>();
            int minutes = cacheMinutes();
            var toFetch = new List<string>();

            foreach (var id in ids)
            {
                if (cache.TryGetValue(id, out var entry) && (DateTime.Now - entry.CachedAt).TotalMinutes < minutes)
                {
                    result[id] = entry.Url;
                }
                else
                {
                    toFetch.Add(id);
                }
            }

            if (toFetch.Count == 0)
            {
                return result;
            }

            if (string.IsNullOrEmpty(apiKey()))
            {
                if (!warnedMissingApiKey)
                {
                    log.Warning("SteamApiKey is not set in config — player avatars will be omitted.");
                    warnedMissingApiKey = true;
                }
                return result;
            }
            warnedMissingApiKey = false;

            for (int offset = 0; offset < toFetch.Count; offset += MaxIdsPerRequest)
            {
                var batch = toFetch.Skip(offset).Take(MaxIdsPerRequest).ToArray();
                foreach (var pair in FetchBatch(batch))
                {
                    result[pair.Key] = pair.Value;
                    cache[pair.Key] = (pair.Value, DateTime.Now);
                }
            }

            return result;
        }

        private Dictionary<string, string> FetchBatch(string[] steamIds)
        {
            var urls = new Dictionary<string, string>();

            try
            {
                string idList = string.Join(",", steamIds);
                string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey()}&steamids={idList}";
                string response = httpClient.GetStringAsync(url).GetAwaiter().GetResult();

                var steamResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);
                foreach (var player in steamResponse?.Response?.Players ?? new List<SteamPlayer>())
                {
                    if (!string.IsNullOrEmpty(player.SteamId))
                    {
                        urls[player.SteamId] = player.AvatarFull ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warning($"Unable to load Steam avatars: {ex.Message}");
            }

            return urls;
        }

        // Otherwise a player who never reconnects keeps their entry forever
        private void Prune()
        {
            int minutes = cacheMinutes();
            if ((DateTime.Now - lastPrune).TotalMinutes < minutes)
            {
                return;
            }
            lastPrune = DateTime.Now;

            var cutoff = DateTime.Now.AddMinutes(-minutes);
            foreach (var pair in cache)
            {
                if (pair.Value.CachedAt < cutoff)
                {
                    cache.TryRemove(pair.Key, out _);
                }
            }
        }

        private class SteamApiResponse
        {
            public SteamResponse Response { get; set; }
        }

        private class SteamResponse
        {
            public List<SteamPlayer> Players { get; set; }
        }

        private class SteamPlayer
        {
            [JsonProperty("steamid")]
            public string SteamId { get; set; }

            [JsonProperty("avatarfull")]
            public string AvatarFull { get; set; }
        }
    }
}
