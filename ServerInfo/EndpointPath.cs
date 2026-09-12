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
