# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(with the [BepInEx-safe caveat in CONTRIBUTING.md](CONTRIBUTING.md#releasing) —
no prerelease suffixes).

## [Unreleased]

## [2.2.1] - 2026-09-12

### Added

- Automated Hexium publishing in the release pipeline, alongside
  Thunderstore. Reverse engineered from
  [Kesomannen/gale](https://github.com/Kesomannen/gale)'s open-source
  client against Hexium's own (undocumented) submission API:
  initiate a multipart upload, `PUT` the package bytes, finish the
  upload, then submit. No plugin code changes — release tooling only.

## [2.2.0] - 2026-09-12

### Added

- Unit test project (`ServerInfo.Tests`) covering the reflection helpers,
  ID/path parsing, and config parsing, plus packaging validation
  (`manifest.json` fields, `icon.png` dimensions, and version consistency
  across `Program.cs`/`AssemblyInfo.cs`/`manifest.json`/`thunderstore.toml`).
- CI (`.github/workflows/ci.yml`): builds and runs the full test suite on
  every push and pull request.
- Automated release pipeline (`.github/workflows/publish.yml`): pushing a
  `v*.*.*` tag builds, tests, and — only if that passes — publishes to
  Thunderstore and attaches the package zip to a GitHub Release for a
  manual Hexium upload (Hexium has no public upload API yet).
- `manifest.json`, `icon.png`, `thunderstore.toml`, and `package.ps1` for
  publishing to Thunderstore and Hexium (see CONTRIBUTING.md).

### Changed

- **Breaking:** no longer bundles its own copy of Newtonsoft.Json.
  Declares a dependency on the shared `ValheimModding-JsonDotNET` library
  instead, to avoid two copies of the same assembly being loaded at once.
  Installs through a mod manager pull this in automatically; a manual
  DLL-only install now needs that library present too.
- Packaged files no longer sit under a `plugins/` subfolder in the
  release zip — they ship flat at the package root instead, so
  `manifest.json` ends up next to the installed DLL (the plugin reads
  its own neighboring manifest.json at runtime; `plugins/` gets
  flattened straight into `BepInEx/plugins/` on install, separating the
  two).

## [2.1.0] - 2026-09-12

### Added

- `Domain` config (`[Server]` section) — this server's public domain or
  IP. It's purely a readiness flag for the plugin: the
  `Access-Control-Allow-Origin` header is now only sent once `Domain` is
  set, so a fresh install doesn't advertise a CORS policy before the
  server is actually meant to be reachable from the outside.
  `AllowedOrigin` itself is unchanged (`*` by default).

## [2.0.0] - 2026-09-12

### Changed

- **Breaking:** renamed the project from PublicWebLink to **Server
  Info**: plugin GUID changed to `Odin_Sons.ServerInfo`, display name to
  `Server Info`, assembly to `ServerInfo.dll`, namespace to `ServerInfo`.
  BepInEx treats the new GUID as a different plugin and generates a
  fresh `Odin_Sons.ServerInfo.cfg`; the old `Maddy.Publicweblink.cfg` is
  not migrated.

## [1.6.0] - 2026-09-12

### Added

- `MetadataSources` config (`[Mods]` section) — a comma-separated order
  of precedence (`Manifest`, `Assembly`) admins can set to control where
  a mod's `description`/`websiteUrl`/`dependencies` come from, down to
  disabling one source entirely or reversing the default priority.

## [1.5.0] - 2026-09-12

### Added

- `LogLevel` config (`[Logging]` section) — a BepInEx `LogLevel` flags
  value controlling which message levels this plugin writes to the log,
  defaulting to `Error, Warning`; add `Info` to also get
  startup/diagnostic messages.
- Mods without a `manifest.json` now fall back to whatever their own
  assembly carries for `description` (`AssemblyDescription`),
  `websiteUrl` (an embedded `RepositoryUrl`), and `dependencies` (their
  BepInEx dependency GUIDs), instead of always returning `null`.

### Changed

- Documented that the plugin works fully without `SteamApiKey` set —
  `players` is still returned, just without `AvatarUrl`.

### Fixed

- The "SteamApiKey is not set" warning is now logged once instead of on
  every refresh.

## [1.4.0] - 2026-09-12

### Added

- Numeric settings (`Port`, `RequestTimeoutSeconds`,
  `CacheIntervalSeconds`, `AvatarCacheMinutes`) now declare an acceptable
  range, so BepInEx clamps an out-of-range value in the `.cfg` file
  instead of accepting it as-is, and config-editor mods render them as a
  bounded slider.

### Changed

- Config settings split into `[Server]`, `[Cache]`, and `[Steam]`
  sections instead of one flat `[General]`.
- Documented in README: how the avatar cache is keyed and expires, and
  that installing is just "drop the DLL" — BepInEx generates the config
  file with defaults on first run.

## [1.3.1] - 2026-09-12

### Changed

- The Windows URL-ACL access-denied message no longer suggests
  `user=Everyone` as the primary fix — it now points at finding and
  using the actual account running the server (`whoami`), with
  `Everyone` documented as a fallback rather than the default: it lets
  any local account on the machine claim the same URL reservation, not
  just the one that needs it.

## [1.3.0] - 2026-09-12

### Added

- `ServerInfoPath` config option — the endpoint path no longer has to be
  `/serverinfo`.
- CORS support (`AllowedOrigin` config, `*` by default) so a status page
  on another domain can call the endpoint directly from the browser.
- A startup log line confirming the port and path the server is
  listening on.
- Startup now detects a Windows URL-ACL access-denied error specifically
  and logs the exact `netsh` command to fix it.

### Changed

- Errors while handling a request are now logged server-side (with the
  full exception), not just returned in the response body.

## [1.2.0] - 2026-09-12

### Added

- Avatar URLs are cached (`AvatarCacheMinutes` config, default 60) and
  pruned once entries go stale, instead of being fetched fresh on every
  refresh for every connected player.

### Changed

- Steam avatar lookups for all connected players are batched into a
  single `GetPlayerSummaries` call instead of one request per player.
- `CacheIntervalSeconds` and `RequestTimeoutSeconds` are now configurable
  instead of hardcoded.
- Code reorganized from a single file into `GameReflection.cs`,
  `GameServerInfoProvider.cs`, `PackageManifestReader.cs`,
  `SteamAvatarService.cs`, and `Models.cs`.
- Renamed internal `Thunderstore*` types/methods to be format-agnostic
  (`PackageManifest`, `ParseNamespace`) — Hexium uses the same
  namespace/name convention, so nothing here was Thunderstore-specific.

### Security

- Steam avatar requests now use HTTPS instead of HTTP.

## [1.1.1] - 2026-09-12

### Fixed

- `namespace` coming back `null` for mods whose package folder has no
  version suffix (some mod managers update packages in place).

## [1.1.0] - 2026-09-12

### Changed

- `mods` in `/serverinfo` changed from a plain string array to objects
  enriched with each mod's own `manifest.json`: `description`,
  `websiteUrl`, `dependencies`, `namespace`, `packageName`.
- Build no longer requires a local Valheim or BepInEx install — moved to
  NuGet-only dependencies (`BepInEx.Core`, `UnityEngine.Modules`), with
  `ZNet`/`Peer` access rewritten to use reflection instead of a direct
  assembly reference.

### Removed

- The unused Harmony dependency.

[Unreleased]: https://github.com/odin-sons/serverinfo/compare/v2.2.1...HEAD
[2.2.1]: https://github.com/odin-sons/serverinfo/compare/v2.2.0...v2.2.1
[2.2.0]: https://github.com/odin-sons/serverinfo/releases/tag/v2.2.0
