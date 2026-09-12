using System.Linq;
using System.Reflection;

namespace ServerInfo
{
    // Fallback source for mod details when there's no manifest.json — reads
    // whatever the plugin's own assembly metadata happens to carry instead
    internal static class AssemblyMetadataReader
    {
        public static string GetDescription(Assembly assembly)
        {
            var description = assembly?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }

        // Newer SDK-style projects (PublishRepositoryUrl) embed this
        // automatically from the project's <RepositoryUrl>
        public static string GetRepositoryUrl(Assembly assembly)
        {
            var url = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;
            return string.IsNullOrWhiteSpace(url) ? null : url;
        }
    }
}
