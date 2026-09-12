using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ServerInfo.Tests
{
    // Validates the files that actually get zipped up and uploaded to
    // Thunderstore/Hexium — catches a forgotten version bump or a broken
    // manifest.json before it ships, not after.
    public class PackagingValidationTests
    {
        private static readonly string RepoRoot = FindRepoRoot();

        private static string FindRepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "manifest.json")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            if (dir == null)
            {
                throw new InvalidOperationException("Could not locate the repo root (manifest.json not found in any parent directory).");
            }
            return dir;
        }

        private static readonly string SourceRoot = Path.Combine(RepoRoot, "ServerInfo");

        private static JObject LoadManifest() =>
            JObject.Parse(File.ReadAllText(Path.Combine(RepoRoot, "manifest.json")));

        [Fact]
        public void RequiredPackageFiles_ExistAtRepoRoot()
        {
            foreach (var file in new[] { "manifest.json", "icon.png", "README.md", "CHANGELOG.md" })
            {
                Assert.True(File.Exists(Path.Combine(RepoRoot, file)), $"{file} is required at the repo root for Thunderstore/Hexium.");
            }
        }

        [Fact]
        public void Manifest_NameIsValidForThunderstoreAndHexium()
        {
            var name = (string)LoadManifest()["name"];
            Assert.False(string.IsNullOrEmpty(name));
            Assert.Matches("^[A-Za-z0-9_]{1,128}$", name);
        }

        [Fact]
        public void Manifest_DescriptionFitsBothPlatformLimits()
        {
            var description = (string)LoadManifest()["description"];
            Assert.False(string.IsNullOrWhiteSpace(description));
            // Thunderstore allows 250 chars, Hexium 256 — stay under the stricter one
            Assert.True(description.Length <= 250, $"description is {description.Length} chars, over the 250-char limit shared by Thunderstore and Hexium.");
        }

        [Fact]
        public void Manifest_VersionNumberIsBepInExSafeSemVer()
        {
            // Thunderstore/Hexium both accept a "-alpha.N"/"-beta.N"/"-rc.N"
            // suffix on version_number, but BepInEx's own [BepInPlugin]
            // Version is parsed as a System.Version, which has no concept of
            // a prerelease suffix. Verified directly against BepInEx.dll:
            // a suffixed string doesn't throw there, it silently leaves
            // BepInPlugin.Version null — so this mod never uses a suffix,
            // keeping every version string BepInEx-safe too.
            var version = (string)LoadManifest()["version_number"];
            Assert.Matches(@"^\d+\.\d+\.\d+$", version);
            Assert.True(Version.TryParse(version, out _), $"'{version}' is not parseable as a System.Version, which is what BepInEx uses for [BepInPlugin]'s Version.");
        }

        [Fact]
        public void Manifest_WebsiteUrlFieldIsPresentAsString()
        {
            var token = LoadManifest()["website_url"];
            Assert.NotNull(token);
            Assert.Equal(JTokenType.String, token.Type);
        }

        [Fact]
        public void Manifest_DependenciesAreWellFormedDependencyStrings()
        {
            var dependencies = LoadManifest()["dependencies"];
            Assert.NotNull(dependencies);
            foreach (var dependency in dependencies)
            {
                Assert.Matches(@"^[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+$", (string)dependency);
            }
        }

        [Fact]
        public void Manifest_VersionMatchesBepInPluginVersion()
        {
            var manifestVersion = (string)LoadManifest()["version_number"];
            var programCs = File.ReadAllText(Path.Combine(SourceRoot, "Program.cs"));
            var match = Regex.Match(programCs, "\\[BepInPlugin\\(\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"([^\"]+)\"\\)\\]");
            Assert.True(match.Success, "Could not find a [BepInPlugin] version in Program.cs.");
            Assert.Equal(manifestVersion, match.Groups[1].Value);
        }

        [Fact]
        public void Manifest_VersionMatchesAssemblyInfoVersion()
        {
            var manifestVersion = (string)LoadManifest()["version_number"];
            var assemblyInfo = File.ReadAllText(Path.Combine(SourceRoot, "Properties", "AssemblyInfo.cs"));
            // Anchored to line start so the commented-out template example
            // ("// [assembly: AssemblyVersion(...")) is skipped
            var match = Regex.Match(assemblyInfo, "^\\[assembly: AssemblyVersion\\(\"([^\"]+)\"\\)\\]", RegexOptions.Multiline);
            Assert.True(match.Success, "Could not find an active AssemblyVersion attribute in Properties/AssemblyInfo.cs.");
            Assert.Equal(manifestVersion + ".0", match.Groups[1].Value);
        }

        private static string LoadTomlText() =>
            File.ReadAllText(Path.Combine(RepoRoot, "thunderstore.toml"));

        private static string TomlValue(string toml, string key)
        {
            var match = Regex.Match(toml, $"(?m)^{key}\\s*=\\s*\"([^\"]*)\"");
            Assert.True(match.Success, $"thunderstore.toml is missing '{key}'.");
            return match.Groups[1].Value;
        }

        [Fact]
        public void ThunderstoreToml_PackageIdentityMatchesManifestAndPluginGuid()
        {
            var toml = LoadTomlText();
            var manifest = LoadManifest();

            var tomlNamespace = TomlValue(toml, "namespace");
            var tomlName = TomlValue(toml, "name");

            Assert.Equal((string)manifest["name"], tomlName);
            Assert.Equal((string)manifest["description"], TomlValue(toml, "description"));
            Assert.Equal((string)manifest["website_url"], TomlValue(toml, "websiteUrl"));

            var programCs = File.ReadAllText(Path.Combine(SourceRoot, "Program.cs"));
            var guidMatch = Regex.Match(programCs, "\\[BepInPlugin\\(\"([^\"]+)\"");
            Assert.True(guidMatch.Success, "Could not find the BepInPlugin GUID in Program.cs.");
            Assert.Equal($"{tomlNamespace}.{tomlName}", guidMatch.Groups[1].Value);
        }

        [Fact]
        public void ThunderstoreToml_DependenciesMatchManifest()
        {
            var toml = LoadTomlText();
            var manifestDependencies = LoadManifest()["dependencies"]
                .Select(dependency => (string)dependency)
                .OrderBy(dependency => dependency)
                .ToArray();

            var sectionMatch = Regex.Match(toml, @"\[package\.dependencies\](.*?)(\r?\n\[|\z)", RegexOptions.Singleline);
            Assert.True(sectionMatch.Success, "Could not find [package.dependencies] in thunderstore.toml.");

            var tomlDependencies = Regex.Matches(sectionMatch.Groups[1].Value, "^([A-Za-z0-9_]+-[A-Za-z0-9_]+)\\s*=\\s*\"([^\"]+)\"", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(match => $"{match.Groups[1].Value}-{match.Groups[2].Value}")
                .OrderBy(dependency => dependency)
                .ToArray();

            Assert.Equal(manifestDependencies, tomlDependencies);
        }

        [Fact]
        public void Changelog_HasASectionForTheCurrentVersion()
        {
            // publish.yml extracts this exact section as the GitHub Release
            // body — a missing one would otherwise only be noticed after
            // the release goes out with an empty changelog. Keep a
            // Changelog heading shape: "## [X.Y.Z] - YYYY-MM-DD".
            var version = (string)LoadManifest()["version_number"];
            var changelog = File.ReadAllText(Path.Combine(RepoRoot, "CHANGELOG.md"));
            Assert.Matches($@"(?m)^## \[{Regex.Escape(version)}\] - \d{{4}}-\d{{2}}-\d{{2}}\s*$", changelog);
        }

        [Fact]
        public void Icon_Is256x256Png()
        {
            var path = Path.Combine(RepoRoot, "icon.png");
            Assert.True(File.Exists(path), "icon.png is missing at the repo root.");

            using (var image = Image.FromFile(path))
            {
                Assert.Equal(256, image.Width);
                Assert.Equal(256, image.Height);
                Assert.Equal(ImageFormat.Png.Guid, image.RawFormat.Guid);
            }
        }
    }
}
