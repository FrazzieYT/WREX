using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using SystemManager.Models;

namespace SystemManager.Services
{
    public static class RegistryService
    {
        public static bool IsWinRE()
        {
            try
            {
                var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "";
                if (systemRoot.StartsWith("X:\\", StringComparison.OrdinalIgnoreCase))
                    return true;

                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
                var miniNT = key?.GetValue("MiniNT");
                return miniNT != null;
            }
            catch
            {
                return false;
            }
        }

        public static string? DetectOfflineWindowsPath()
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                        continue;

                    var windowsPath = Path.Combine(drive.Name, "Windows");
                    if (Directory.Exists(windowsPath))
                    {
                        var system32 = Path.Combine(windowsPath, "System32");
                        if (Directory.Exists(system32))
                            return drive.Name;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        private static readonly Dictionary<string, string> LoadedOfflineHives = new();

        public static void LoadOfflineHive(string hiveName, string hiveFilePath)
        {
            if (!File.Exists(hiveFilePath))
                throw new FileNotFoundException($"Файл куста реестра не найден: {hiveFilePath}");

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
            
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"load \"HKU\\{hiveName}\" \"{hiveFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"Не удалось загрузить куст: {error}");
            }

            LoadedOfflineHives[hiveName] = hiveFilePath;
        }

        public static bool UnloadOfflineHive(string hiveName)
        {
            try
            {
                if (!LoadedOfflineHives.ContainsKey(hiveName))
                    return false;

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"unload \"HKU\\Offline_{hiveName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
        
                process.Start();
                process.WaitForExit();
        
                LoadedOfflineHives.Remove(hiveName);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        private static readonly string FavoritesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "WREX", 
            "favorites.json");

        public static List<FavoriteRegistryEntry> GetFavorites()
        {
            try
            {
                if (!File.Exists(FavoritesFilePath)) return new();
                var json = File.ReadAllText(FavoritesFilePath);
                return JsonSerializer.Deserialize<List<FavoriteRegistryEntry>>(json) ?? new();
            }
            catch { return new(); }
        }

        private static void SaveFavorites(List<FavoriteRegistryEntry> favorites)
        {
            try
            {
                FileMonitorService.MarkWrexOperation(FavoritesFilePath);

                var dir = Path.GetDirectoryName(FavoritesFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                File.WriteAllText(FavoritesFilePath, 
                    JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static bool IsFavorite(RegistryHive hive, string keyPath, string? valueName)
            => GetFavorites().Any(f => f.Hive == hive && f.KeyPath == keyPath && f.ValueName == valueName);

        public static void AddToFavorites(RegistryHive hive, string keyPath, string? valueName)
        {
            var favorites = GetFavorites();
            if (!favorites.Any(f => f.Hive == hive && f.KeyPath == keyPath && f.ValueName == valueName))
            {
                var displayName = string.IsNullOrEmpty(keyPath) 
                    ? HiveToString(hive) 
                    : $@"{HiveToString(hive)}\{keyPath}{(valueName != null ? $" [{valueName}]" : "")}";
                    
                favorites.Add(new FavoriteRegistryEntry
                {
                    Hive = hive,
                    KeyPath = keyPath,
                    ValueName = valueName,
                    DisplayName = displayName
                });
                SaveFavorites(favorites);
            }
        }
        
        public static bool LoadOfflineHive(string hiveName)
        {
            try
            {
                var offlinePath = DetectOfflineWindowsPath();
                if (string.IsNullOrEmpty(offlinePath))
                    return false;

                var hiveFilePath = Path.Combine(offlinePath, "Windows", "System32", "config", hiveName);
        
                if (!File.Exists(hiveFilePath))
                    return false;

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"load \"HKU\\Offline_{hiveName}\" \"{hiveFilePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
        
                process.Start();
                process.WaitForExit();
        
                if (process.ExitCode != 0)
                    return false;

                LoadedOfflineHives[hiveName] = hiveFilePath;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static void RemoveFromFavorites(FavoriteRegistryEntry entry)
        {
            var favorites = GetFavorites();
            favorites.RemoveAll(f => f.Hive == entry.Hive && f.KeyPath == entry.KeyPath && f.ValueName == entry.ValueName);
            SaveFavorites(favorites);
        }
        
        public static List<RegistrySearchResult> SearchRegistry(string searchText, RegistryHive? hive, 
            bool searchKeys, bool searchValues, bool searchValueData, int maxResults)
        {
            var results = new List<RegistrySearchResult>();
            var hives = hive.HasValue ? new[] { hive.Value } : GetRootHives();
            
            foreach (var h in hives)
            {
                if (results.Count >= maxResults) break;
                SearchKey(h, "", searchText, searchKeys, searchValues, searchValueData, results, maxResults);
            }
            
            return results;
        }

        private static void SearchKey(RegistryHive hive, string keyPath, string searchText, 
            bool searchKeys, bool searchValues, bool searchValueData, 
            List<RegistrySearchResult> results, int maxResults)
        {
            if (results.Count >= maxResults) return;
            
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, 
                    Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = string.IsNullOrEmpty(keyPath) ? baseKey : baseKey.OpenSubKey(keyPath);
                if (key == null) return;

                if (searchKeys && !string.IsNullOrEmpty(keyPath) && 
                    keyPath.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new RegistrySearchResult
                    {
                        Type = "Key",
                        Name = Path.GetFileName(keyPath),
                        Path = $@"{HiveToString(hive)}\{Path.GetDirectoryName(keyPath)}",
                        Hive = hive,
                        KeyPath = keyPath
                    });
                }

                foreach (var valueName in key.GetValueNames())
                {
                    if (results.Count >= maxResults) break;
                    
                    var value = key.GetValue(valueName);
                    var valueStr = value?.ToString() ?? "";
                    
                    if ((searchValues && valueName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (searchValueData && valueStr.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new RegistrySearchResult
                        {
                            Type = "Value",
                            Name = valueName,
                            Path = $@"{HiveToString(hive)}\{keyPath}",
                            Value = valueStr,
                            Hive = hive,
                            KeyPath = keyPath
                        });
                    }
                }
     
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    if (results.Count >= maxResults) break;
                    var newPath = string.IsNullOrEmpty(keyPath) ? subKeyName : $@"{keyPath}\{subKeyName}";
                    SearchKey(hive, newPath, searchText, searchKeys, searchValues, searchValueData, results, maxResults);
                }
            }
            catch { }
        }
        
        public static RegistryValueKind? GetValueKind(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, 
                    Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetValueKind(valueName);
            }
            catch { return null; }
        }

        public static RegistryHive[] GetRootHives() => new[]
        {
            RegistryHive.LocalMachine,
            RegistryHive.CurrentUser,
            RegistryHive.ClassesRoot,
            RegistryHive.Users,
            RegistryHive.CurrentConfig
        };

        public static string HiveToString(RegistryHive hive) => hive switch
        {
            RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
            RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
            RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
            RegistryHive.Users => "HKEY_USERS",
            RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
            _ => hive.ToString()
        };
        
        public static void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.String)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = baseKey.CreateSubKey(keyPath, true);
            
            key.SetValue(valueName, value, kind);
        }

        public static object? GetValue(RegistryHive hive, string keyPath, string valueName)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = baseKey.OpenSubKey(keyPath);
            return key?.GetValue(valueName);
        }

        public static void DeleteValue(RegistryHive hive, string keyPath, string valueName)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = baseKey.OpenSubKey(keyPath, true);

            key?.DeleteValue(valueName, false);
        }

        public static void CreateKey(RegistryHive hive, string keyPath)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            baseKey.CreateSubKey(keyPath, true);
        }

        public static void DeleteKey(RegistryHive hive, string keyPath, bool recursive = false)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);

            if (recursive)
                baseKey.DeleteSubKeyTree(keyPath, false);
            else
                baseKey.DeleteSubKey(keyPath, false);
        }

        public static string[] GetSubKeyNames(RegistryHive hive, string keyPath)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = baseKey.OpenSubKey(keyPath);
            return key?.GetSubKeyNames() ?? Array.Empty<string>();
        }

        public static string[] GetValueNames(RegistryHive hive, string keyPath)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = baseKey.OpenSubKey(keyPath);
            return key?.GetValueNames() ?? Array.Empty<string>();
        }
    }
}