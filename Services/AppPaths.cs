using System;
using System.IO;

namespace SystemManager.Services
{
    public static class AppPaths
    {
        public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
        
        public static string AppDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WREX");

        public static string DataDirectory
        {
            get
            {
                var path = Path.Combine(AppDirectory, "WREX_Data");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string LogsDirectory
        {
            get
            {
                var path = Path.Combine(DataDirectory, "Logs");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string ExportDirectory
        {
            get
            {
                var path = Path.Combine(DataDirectory, "Export");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string HistoryFile => Path.Combine(DataDirectory, "history.json");
        public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
        public static string FavoritesFile => Path.Combine(AppDataFolder, "registry_favorites.json");
        public static string RegistryFavoritesFile => Path.Combine(DataDirectory, "registry_favorites.json");
        public static string ConfigFile => Path.Combine(AppDataFolder, "config.json");

        public static bool IsPortable
        {
            get
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                return !AppDirectory.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void EnsureAppDataFolder()
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
        }
    }
}