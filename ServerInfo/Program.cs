using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace ServerInfo
{
    [BepInPlugin("fogrew.ServerInfo", "Server Info", "2.2.0")]
    public class ServerInfoPlugin : BaseUnityPlugin
    {
        private static PluginLog Log;
        private HttpListener listener;
        private bool running;

        private ConfigEntry<LogLevel> logLevel;
        private ConfigEntry<int> port;
        private ConfigEntry<string> serverInfoPath;
        private ConfigEntry<string> domain;
        private ConfigEntry<string> allowedOrigin;
        private ConfigEntry<int> requestTimeoutSeconds;
        private ConfigEntry<int> cacheIntervalSeconds;
        private ConfigEntry<int> avatarCacheMinutes;
        private ConfigEntry<string> steamApiKey;
        private ConfigEntry<string> modMetadataSources;

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private GameServerInfoProvider serverInfoProvider;

        private void Awake()
        {
            logLevel = Config.Bind("Logging", "LogLevel", LogLevel.Error | LogLevel.Warning,
                "Which message levels this plugin writes to the BepInEx log. Flags — combine values in the .cfg " +
                "(e.g. \"Error, Warning, Info\"). Info adds startup/diagnostic messages not needed for normal operation.");
            Log = new PluginLog(Logger, () => logLevel.Value);

            port = Config.Bind("Server", "Port", 8880,
                new ConfigDescription("HTTP port for the web server.", new AcceptableValueRange<int>(1, 65535)));
            serverInfoPath = Config.Bind("Server", "ServerInfoPath", "/serverinfo",
                "URL path the server info endpoint is served on.");
            domain = Config.Bind("Server", "Domain", "",
                "Public domain or IP this server is reachable at. Optional and purely informational to the plugin " +
                "itself — it only gates AllowedOrigin below: leave it empty while the server isn't publicly reachable " +
                "yet, and the plugin won't advertise a CORS policy for it.");
            allowedOrigin = Config.Bind("Server", "AllowedOrigin", "*",
                "Value sent as Access-Control-Allow-Origin — so a status page on another domain can read the response " +
                "directly from the browser — but only once Domain (above) is set. Must exactly match the calling " +
                "page's scheme+host+port (e.g. \"https://status.example.com\"); \"*\" allows any site.");

            requestTimeoutSeconds = Config.Bind("Server", "RequestTimeoutSeconds", 5,
                new ConfigDescription("How long, in seconds, an HTTP request waits for the game's main thread before returning a timeout error.", new AcceptableValueRange<int>(1, 60)));

            cacheIntervalSeconds = Config.Bind("Cache", "CacheIntervalSeconds", 5,
                new ConfigDescription("How often, in seconds, the server/player list is recomputed. Lower is fresher but does more work per request.", new AcceptableValueRange<int>(1, 3600)));
            avatarCacheMinutes = Config.Bind("Cache", "AvatarCacheMinutes", 60,
                new ConfigDescription("How long, in minutes, a fetched Steam avatar URL is reused before being re-fetched, and how long an inactive player's entry is kept before eviction.", new AcceptableValueRange<int>(1, 1440)));

            steamApiKey = Config.Bind("Steam", "SteamApiKey", "",
                "Steam Web API key used to fetch player avatars.");

            modMetadataSources = Config.Bind("Mods", "MetadataSources", ModMetadataSources.Default,
                "Comma-separated order of precedence for a mod's description/websiteUrl/dependencies. " +
                "Recognized values: \"Manifest\" (its manifest.json) and \"Assembly\" (its own compiled metadata). " +
                "For each field, the first source in the list with a non-empty value wins. Example: " +
                "\"Assembly,Manifest\" prefers assembly metadata; \"Manifest\" alone disables the assembly fallback; " +
                "\"Assembly\" alone ignores manifest.json entirely. Does not affect namespace/packageName, which only " +
                "ever come from a manifest.json.");

            var steamAvatars = new SteamAvatarService(Log, () => steamApiKey.Value, () => avatarCacheMinutes.Value);
            var manifestReader = new PackageManifestReader(Log);
            serverInfoProvider = new GameServerInfoProvider(steamAvatars, manifestReader, () => cacheIntervalSeconds.Value,
                () => ModMetadataSources.Parse(modMetadataSources.Value, Log));

            StartServer();
        }

        private void Update()
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void StartServer()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://+:{port.Value}/");
                listener.Start();
                running = true;
                Task.Run(HandleLoop);
                Log.Info($"Web server listening on port {port.Value}, serving {EndpointPath.Normalize(serverInfoPath.Value)}");
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                Log.Error($"Access denied binding port {port.Value}. On Windows, run once as administrator " +
                    $"(replace YOUR_USERNAME with the account running this server — check with 'whoami'): " +
                    $"netsh http add urlacl url=http://+:{port.Value}/ user=YOUR_USERNAME " +
                    $"— see README.md for details and a less strict fallback.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start server: {ex}");
            }
        }

        private void StopServer()
        {
            running = false;
            if (listener != null)
            {
                listener.Stop();
                listener.Close();
                listener = null;
            }
        }

        private async Task HandleLoop()
        {
            while (running)
            {
                try
                {
                    var ctx = await listener.GetContextAsync();
                    await HandleRequest(ctx);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in handle loop: {ex}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            if (!string.IsNullOrWhiteSpace(domain.Value))
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", allowedOrigin.Value);
            }

            string path = ctx.Request.Url.AbsolutePath.ToLowerInvariant();
            string infoPath = EndpointPath.Normalize(serverInfoPath.Value);
            string json;

            try
            {
                if (path == "/")
                {
                    json = Json(new { endpoints = new[] { infoPath } });
                }
                else if (path == infoPath)
                {
                    json = await GetServerInfoJsonAsync();
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    json = Json(new { error = "Not found" });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled error serving {path}: {ex}");
                ctx.Response.StatusCode = 500;
                json = Json(new { error = "Internal server error", details = ex.Message });
            }

            var buf = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json";
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.OutputStream.Close();
        }

        // ZNet/Chainloader are only safe to touch from Unity's main thread;
        // HandleLoop runs on HttpListener's own background thread
        private async Task<string> GetServerInfoJsonAsync()
        {
            var tcs = new TaskCompletionSource<ServerInfoSnapshot>();
            var timeout = Task.Delay(TimeSpan.FromSeconds(requestTimeoutSeconds.Value));

            mainThreadQueue.Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(serverInfoProvider.GetSnapshot());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            var completedTask = await Task.WhenAny(tcs.Task, timeout);
            if (completedTask == timeout)
            {
                return Json(new { error = "Request timed out" });
            }

            var snapshot = await tcs.Task;
            if (snapshot == null)
            {
                return Json(new { error = "Server not initialized" });
            }

            return Json(new
            {
                name = snapshot.Name,
                playersCount = snapshot.PlayersCount,
                players = snapshot.Players,
                mods = snapshot.Mods,
            });
        }

        private string Json(object obj) =>
            JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}
