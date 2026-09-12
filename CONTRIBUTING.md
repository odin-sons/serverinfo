# Contributing

## Building

No local Valheim or BepInEx install is needed — everything the project
references comes from NuGet (see `nuget.config` for the extra BepInEx
feed, `nuget.bepinex.dev`, alongside nuget.org):

```
dotnet build ServerInfo.sln -c Release
```

Output: `ServerInfo\bin\Release\net472\ServerInfo.dll`.

Valheim's own game types (`ZNet`, `Peer`, …) aren't a compile-time
dependency — the plugin runs inside the game process, so those types
already exist in the AppDomain at runtime. `GameReflection` reaches them
by name via reflection instead of a direct reference.

## Project layout

Top level separates the mod's own code, its tests, and repo/publishing
infrastructure:

- `ServerInfo/` — the plugin itself (`ServerInfo.csproj`).
  - `Program.cs` — plugin entrypoint, HTTP server, config, routing.
  - `GameReflection.cs` — reflection helpers for Valheim's own types.
  - `GameServerInfoProvider.cs` — builds the `/serverinfo` snapshot.
  - `PackageManifestReader.cs` — reads a mod's `manifest.json`.
  - `SteamAvatarService.cs` — Steam avatar lookups, batching, caching.
  - `EndpointPath.cs` — normalizes the configurable endpoint path.
  - `AssemblyMetadataReader.cs` / `ModMetadataSources.cs` — mod-metadata fallback and its config parsing.
  - `PluginLog.cs` — log-level-gated logging.
  - `Models.cs` — response/internal data shapes.
- `ServerInfo.Tests/` — unit tests plus packaging validation (below).
- Repo root — solution file, docs, and publishing infrastructure:
  `manifest.json`, `icon.png`, `thunderstore.toml`, `package.ps1`,
  `.github/workflows/`.

## Testing

```
dotnet test ServerInfo.sln
```

Covers the pure logic (reflection helpers, ID/path parsing, config
parsing) via a probe type with no game dependency, and validates the
packaging files themselves — `manifest.json` fields, `icon.png`
dimensions, and that the version number is consistent across
`Program.cs`, `Properties/AssemblyInfo.cs`, and `manifest.json`. This
also runs in CI (see Publishing) — a failing test blocks a release.

## Releasing

Bump the version in all of:

- `ServerInfo/Program.cs` (`[BepInPlugin]`)
- `ServerInfo/Properties/AssemblyInfo.cs` (`AssemblyVersion`/`AssemblyFileVersion`)
- `manifest.json` (`version_number`)
- `thunderstore.toml` (`versionNumber` — cosmetic only, CI always
  overrides it from the git tag, but keep it sensible for manual
  `tcli build`)

Always plain `Major.Minor.Patch` — **no prerelease suffix** (no
`-alpha.1`/`-beta.1`/`-rc.1`), even though Thunderstore/Hexium both
accept one. BepInEx's `[BepInPlugin]` Version is parsed as a
`System.Version`, which has no concept of a suffix; a suffixed string
doesn't error there, it silently leaves `BepInPlugin.Version` null
(verified directly against BepInEx.dll).

`dotnet test` fails loudly if the first three drift apart, so it's hard
to miss one. Add an entry to `CHANGELOG.md`, commit, then tag:

```
git tag v<version>
git push origin v<version>
```

Pushing a `v*.*.*` tag runs `.github/workflows/publish.yml`, which
builds, tests, and — only if that passes — publishes to Thunderstore and
attaches the package zip to a GitHub Release.

To build the package locally without publishing (e.g. to check its
contents, or to upload it to Hexium by hand):

```
./package.ps1
```

This builds the project and zips `manifest.json`, `icon.png`,
`README.md`, `CHANGELOG.md`, and the plugin DLL — all flat at the
package root — into `release/ServerInfo-<version>.zip`. No `plugins/`
subfolder on purpose: that special folder gets flattened straight into
`BepInEx/plugins/` on install, separating the DLL from `manifest.json`,
but this mod reads its own neighboring `manifest.json` at runtime (see
`PackageManifestReader.cs`), so everything ships flat next to it
instead — the same layout observed in the wild for other
manifest-reading mods (e.g. `shudnal-ConditionalConfigSync`).

## Publishing

`package.ps1`'s output is a standard Thunderstore-format package —
Hexium accepts the same zip as-is, no changes needed.

- **Thunderstore** publishing is automated: pushing a version tag builds
  and uploads it via [Thunderstore CLI](https://github.com/thunderstore-io/thunderstore-cli)
  (`tcli`), authenticated with the `THUNDERSTORE_API_TOKEN` repository
  secret (a Thunderstore service account token — Settings → Teams →
  fogrew → Service Accounts on thunderstore.io). See
  `.github/workflows/publish.yml`.
- **Hexium has no public upload API yet**, so this stays manual: grab
  the zip from the tag's GitHub Release (or run `package.ps1` locally)
  and upload it from your team's submit page at
  https://valheim.hexium.gg/.

`manifest.json`'s `website_url` is left empty — fill it in with this
project's repository URL once one exists.
