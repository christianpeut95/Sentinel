namespace Sentinel.Constants
{
    public static class AppVersion
    {
        public const string Version = "1.0.0-alpha";
        public const string ReleaseDate = "March 2026";
        public const string ProductName = "Sentinel";

        public static bool IsDemoMode { get; set; }

        public static string DisplayVersion => IsDemoMode ? $"{Version} (Demo)" : Version;
        public static string FullVersionString => IsDemoMode ? $"v{Version} (Demo)" : $"v{Version}";

        public static string GetVersionInfo() => $"{ProductName} {FullVersionString} ({ReleaseDate})";
    }
}
