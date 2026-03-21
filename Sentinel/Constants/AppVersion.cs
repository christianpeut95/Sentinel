namespace Sentinel.Constants
{
    public static class AppVersion
    {
        public const string Version = "1.0.1-alpha";
        public const string ReleaseDate = "March 2026";
        public const string ProductName = "Sentinel";

        public static bool IsDemoMode { get; set; }

        public static string DisplayVersion => IsDemoMode ? $"{Version} (Demo)" : Version;
        public static string FullVersionString => IsDemoMode ? $"v{Version} (Demo)" : $"v{Version}";

        public static string GetVersionInfo() => $"{ProductName} {FullVersionString} ({ReleaseDate})";
        
        // Get build date from assembly file timestamp
        private static DateTime? _buildDate;
        public static DateTime BuildDate
        {
            get
            {
                if (_buildDate == null)
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var fileInfo = new System.IO.FileInfo(assembly.Location);
                    _buildDate = fileInfo.LastWriteTimeUtc;
                }
                return _buildDate.Value;
            }
        }
        
        public static string BuildDateString => BuildDate.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        public static string FullVersionWithBuild => $"{FullVersionString} (built {BuildDateString})";
    }
}

