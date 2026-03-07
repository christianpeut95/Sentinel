namespace Sentinel.Constants
{
    public static class AppVersion
    {
        public const string Version = "1.0.0-alpha";
        public const string ReleaseDate = "March 2026";
        public const string ProductName = "Sentinel";
        public const string FullVersionString = "v1.0.0-alpha";
        
        public static string GetVersionInfo() => $"{ProductName} {FullVersionString} ({ReleaseDate})";
    }
}
