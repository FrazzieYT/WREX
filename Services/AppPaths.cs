using System;
using System.IO;

namespace SystemManager.Services
{
    public static class AppPaths
    {
        public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string AppDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WREX");

        public static string DataDirectory
        {
            get
            {
                var path = Path.Combine(AppDirectory, "WREX_Data");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string LogsDirectory
        {
            get
            {
                var path = Path.Combine(DataDirectory, "Logs");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string ExportDirectory
        {
            get
            {
                var path = Path.Combine(DataDirectory, "Export");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}
