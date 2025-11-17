using System;
using System.IO;

namespace FootPedalApp
{
    public static class Logger
    {
        private static string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FootPedalApp",
            "debug.log"
        );

        static Logger()
        {
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                // Clear log on startup
                File.WriteAllText(logPath, $"=== FootPedalApp Started at {DateTime.Now} ===\r\n");
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n";
                File.AppendAllText(logPath, logMessage);
            }
            catch { }
        }

        public static void LogException(string context, Exception ex)
        {
            Log($"ERROR in {context}: {ex.Message}\r\n{ex.StackTrace}");
        }

        public static string GetLogPath()
        {
            return logPath;
        }
    }
}
