# Server Info

[![CI](https://github.com/odin-sons/serverinfo/actions/workflows/ci.yml/badge.svg)](https://github.com/odin-sons/serverinfo/actions/workflows/ci.yml)
[![Publish](https://github.com/odin-sons/serverinfo/actions/workflows/publish.yml/badge.svg)](https://github.com/odin-sons/serverinfo/actions/workflows/publish.yml)

A BepInEx plugin for Valheim dedicated servers. Exposes a small HTTP API
with the world name, connected players, and installed mods (enriched with
each mod's `manifest.json` when it has one) — used by the project's
status page to show live server state.

## Installing

Copy `ServerInfo.dll` into the server's `BepInEx/plugins/` folder and
restart the server process — that's it. BepInEx generates
`BepInEx/config/Odin_Sons.ServerInfo.cfg` on first run with every setting
below at its default, so there's nothing else to set up before it works.

## Network setup

The plugin runs its own HTTP server; it doesn't need nginx or any other
reverse proxy in front of it. Two things to check on a fresh server:

- **Firewall / port forwarding.** Whatever port you configure (`8880` by
  default) needs to be reachable from wherever your status page or
  browser calls it from — open it in your firewall, and forward it on
  your router if the server is behind NAT.
- **Windows: URL reservation.** `HttpListener` needs an admin-granted
  reservation before a non-admin process can bind a port for all hosts.
  This only controls which *local Windows accounts* may bind that port —
  it has no effect on network access, which is what the firewall step
  above is for. If the log shows an access error on startup, find the
  account actually running the server (`whoami`) and run once as
  administrator (replace `8880` and `YOUR_USERNAME`):
  ```
  netsh http add urlacl url=http://+:8880/ user=YOUR_USERNAME
  ```
  If you're not sure which account will run it (e.g. a service manager
  that may change this), `user=Everyone` always works but lets *any*
  local account on the machine claim that reservation too — fine on a
  single-user machine, worth avoiding on a shared one.

Cross-origin requests are controlled by two settings, both under `[Server]`:

- **`Domain`** — the public domain or IP this server is reachable at.
  Empty by default. It isn't used by the plugin for anything except
  gating the next setting — think of it as "this server is ready to be
  public" rather than an address the plugin acts on.
- **`AllowedOrigin`** — who's allowed to read the response from a
  browser. Defaults to `*` (any site), but is only actually sent once
  `Domain` is set — so a fresh install doesn't advertise a CORS policy
  for a server that isn't publicly reachable yet.

If your status page reads this endpoint directly from the browser, set
`Domain` to this server's real address and, if you want to restrict who
can read the response, set `AllowedOrigin` to the status page's exact
origin (scheme+host+port, e.g. `https://status.example.com` — the
browser requires an exact match, so a domain that merely points at the
same server doesn't count as the same origin).

## Configuration

Set via `BepInEx/config/Odin_Sons.ServerInfo.cfg`. Numeric settings
declare an acceptable range — BepInEx clamps an out-of-range value in
the file to the nearest bound instead of accepting it as-is, and any
BepInEx config-editor mod (e.g. Configuration Manager) renders them as a
bounded slider rather than free text.

**`[Server]`**

| Key | Type | Default | Range | Meaning |
|---|---|---|---|---|
| `Port` | Int32 | `8880` | 1–65535 | HTTP port for the web server. |
| `ServerInfoPath` | String | `/serverinfo` | — | URL path the server info endpoint is served on. |
| `Domain` | String | *(empty)* | — | This server's public domain or IP. Gates `AllowedOrigin` — see below. |
| `AllowedOrigin` | String | `*` | — | Value sent as `Access-Control-Allow-Origin`, only once `Domain` is set. |
| `RequestTimeoutSeconds` | Int32 | `5` | 1–60 | How long a request waits for the game's main thread before timing out. |

**`[Cache]`**

| Key | Type | Default | Range | Meaning |
|---|---|---|---|---|
| `CacheIntervalSeconds` | Int32 | `5` | 1–3600 | How often the server/player list is recomputed. |
| `AvatarCacheMinutes` | Int32 | `60` | 1–1440 | How long a fetched avatar URL is reused, and how long an inactive player's entry is kept. |

**`[Steam]`**

| Key | Type | Default | Meaning |
|---|---|---|---|
| `SteamApiKey` | String | *(empty)* | Optional; without it, the plugin still works — `players` is returned as usual, just without `AvatarUrl`. |

**`[Mods]`**

| Key | Type | Default | Meaning |
|---|---|---|---|
| `MetadataSources` | String | `Manifest,Assembly` | Comma-separated order of precedence for a mod's `description`/`websiteUrl`/`dependencies` — see below. |

**`[Logging]`**

| Key | Type | Default | Meaning |
|---|---|---|---|
| `LogLevel` | Flags enum | `Error, Warning` | Which message levels this plugin writes to the BepInEx log. Combine values in the `.cfg` (e.g. `Error, Warning, Info`) to include more detail; `Info` adds startup/diagnostic messages not needed for normal operation. |

### How the avatar cache works

Fetched avatar URLs are kept in memory, keyed by SteamID — not written
to disk, and gone on restart. An entry younger than `AvatarCacheMinutes`
is reused as-is; once it's older, the next request for that player
fetches a fresh URL from Steam and replaces it. Entries nobody's asked
for in over `AvatarCacheMinutes` are dropped during the next refresh, so
a player who was online once doesn't sit in memory forever.

There's no live command to clear it early — restarting the server is
the only way, since the cache doesn't survive that anyway. To make it
refresh sooner across the board, lower `AvatarCacheMinutes`.

## Example response

```
GET /serverinfo
```

```json
{
  "name": "MyValheimServer",
  "playersCount": 1,
  "players": [
    {
      "Name": "Ari",
      "SteamID": "76561198000000000",
      "AvatarUrl": "https://avatars.steamstatic.com/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx_full.jpg"
    }
  ],
  "mods": [
    {
      "name": "Conditional Config Sync",
      "guid": "_shudnal.ConditionalConfigSync",
      "version": "1.0.4",
      "description": "Shared config synchronization and server policy library for Valheim mods.",
      "websiteUrl": "https://github.com/shudnal/ConditionalConfigSync",
      "dependencies": ["denikson-BepInExPack_Valheim-5.4.2333"],
      "namespace": "shudnal",
      "packageName": "ConditionalConfigSync"
    },
    {
      "name": "BepInExPack_Valheim",
      "guid": "denikson.BepInExPack_Valheim",
      "version": "5.4.2333",
      "description": "BepInEx pack for Valheim.",
      "websiteUrl": "https://github.com/BepInEx/BepInEx",
      "dependencies": [],
      "namespace": "denikson",
      "packageName": "BepInExPack_Valheim"
    }
  ]
}
```

`name`/`guid`/`version` on a mod come from BepInEx itself, so they're
always present. Where `description`/`websiteUrl`/`dependencies` come
from is controlled by `MetadataSources`:

- **`Manifest`** — the mod's own `manifest.json` (Thunderstore/Hexium
  package): `description`, `website_url`, `dependencies`.
- **`Assembly`** — the mod's own compiled metadata: `AssemblyDescription`
  for `description`, an embedded `RepositoryUrl` (some build setups add
  this automatically) for `websiteUrl`, and its BepInEx dependency GUIDs
  for `dependencies`.

For each field, sources are tried in the configured order and the first
non-empty value wins — so the default `Manifest,Assembly` prefers the
manifest and only falls back to the assembly for mods that don't have
one. Set it to `Manifest` to disable the assembly fallback entirely, to
`Assembly` to ignore manifests altogether, or to `Assembly,Manifest` to
prefer assembly metadata when both are present. A field is `null` if
none of the configured sources have a value for it.

`namespace`/`packageName` aren't affected by `MetadataSources` — they're
Thunderstore/Hexium package identity, so they only ever come from a
manifest.json, or `null` without one.

`namespace` + `packageName` build a Thunderstore or Hexium mod page URL
(both use the same shape):

- `https://thunderstore.io/c/valheim/p/{namespace}/{packageName}/`
- `https://valheim.hexium.gg/mods/{namespace}/{packageName}`

## Credits

Based on PublicWebLink by Maddy.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for building from source.
