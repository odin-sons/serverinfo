param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$manifest = Get-Content (Join-Path $root "manifest.json") -Raw | ConvertFrom-Json
$version = $manifest.version_number

dotnet build (Join-Path $root "ServerInfo\ServerInfo.csproj") -c $Configuration

$dll = Join-Path $root "ServerInfo\bin\$Configuration\net472\ServerInfo.dll"
if (-not (Test-Path $dll)) {
    throw "Build output not found: $dll"
}

$releaseDir = Join-Path $root "release"
$stage = Join-Path $releaseDir "stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

# No "plugins/" subfolder: that gets flattened straight into BepInEx/plugins/
# on install, separating the DLL from manifest.json — but this mod reads its
# own neighboring manifest.json at runtime (see PackageManifestReader.cs), so
# everything ships flat at the package root instead.
Copy-Item (Join-Path $root "manifest.json") $stage
Copy-Item (Join-Path $root "icon.png") $stage
Copy-Item (Join-Path $root "README.md") $stage
Copy-Item (Join-Path $root "CHANGELOG.md") $stage
Copy-Item (Join-Path $root "LICENSE") $stage
Copy-Item $dll $stage

$zipPath = Join-Path $releaseDir "ServerInfo-$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath

Remove-Item $stage -Recurse -Force

Write-Output "Package ready: $zipPath"
