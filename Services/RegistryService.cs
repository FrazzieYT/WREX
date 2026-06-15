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
        // WinRE Detection
        public static bool IsWinRE()
        {
            try
            {
                // Проверяем, запущены ли мы в WinRE
                // В WinRE нет Explorer.exe и есть X:\windows\system32
                var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "";
                if (systemRoot.StartsWith("X:\\", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Проверяем наличие ключа WinPE
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
                // Ищем Windows на других дисках
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                        continue;

                    var windowsPath = Path.Combine(drive.Name, "Windows");
                    if (Directory.Exists(windowsPath))
                    {
                        // Проверяем наличие системных файлов
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

        // Offline hive management
        private static readonly Dictionary<string, string> LoadedOfflineHives = new();

        public static void LoadOfflineHive(string hiveName, string hiveFilePath)
        {
            try
            {
                if (!File.Exists(hiveFilePath))
                    throw new FileNotFoundException($"Файл куста реестра не найден: {hiveFilePath}");

                // Загружаем куст в текущий реестр
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
                
                // Используем reg load для загрузки куста
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
                HistoryService.Log("Загружен куст реестра", $"{hiveName} из {hiveFilePath}", "Registry");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка загрузки куста реестра", $"{hiveName}: {ex.Message}", "Registry");
                throw;
            }
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
                HistoryService.Log("Выгружен offline куст реестра", hiveName, "Registry");
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        // Избраное
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
                HistoryService.Log("Добавлено в избранное", displayName, "Registry");
            }
        }
        
        public static bool LoadOfflineHive(string hiveName)
        {
            try
            {
                var offlinePath = DetectOfflineWindowsPath();
                if (string.IsNullOrEmpty(offlinePath))
                    return false;

                // Путь к файлу куста реестра
                var hiveFilePath = Path.Combine(offlinePath, "Windows", "System32", "config", hiveName);
        
                if (!File.Exists(hiveFilePath))
                    return false;

                // Загружаем куст в HKU\Offline_{hiveName}
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
                HistoryService.Log("Загружен offline куст реестра", $"{hiveName} из {hiveFilePath}", "Registry");
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
            HistoryService.Log("Удалено из избранного", entry.DisplayName, "Registry");
        }

        // Поиск
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

        // Вспомогательные методы
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

        // Основные методы
        public static void SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.String)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.CreateSubKey(keyPath, true);
                
                object? oldValue = null;
                try { oldValue = key.GetValue(valueName); } catch { }
                
                key.SetValue(valueName, value, kind);

                HistoryService.Log("Запись в реестр",
                    $@"[{hive}\{keyPath}] {valueName} = {value}{(oldValue != null ? $" (было: {oldValue})" : "")}",
                    "Registry");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка записи в реестр",
                    $@"[{hive}\{keyPath}] {valueName}: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static object? GetValue(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetValue(valueName);
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка чтения реестра",
                    $@"[{hive}\{keyPath}] {valueName}: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static void DeleteValue(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(keyPath, true);

                object? oldValue = null;
                try { oldValue = key?.GetValue(valueName); } catch { }

                key?.DeleteValue(valueName, false);

                HistoryService.Log("Удаление значения реестра",
                    $@"[{hive}\{keyPath}] {valueName} (было: {oldValue ?? "не найдено"})",
                    "Registry");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка удаления значения реестра",
                    $@"[{hive}\{keyPath}] {valueName}: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static void CreateKey(RegistryHive hive, string keyPath)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                baseKey.CreateSubKey(keyPath, true);

                HistoryService.Log("Создан ключ реестра", $@"[{hive}\{keyPath}]", "Registry");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка создания ключа реестра",
                    $@"[{hive}\{keyPath}]: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static void DeleteKey(RegistryHive hive, string keyPath, bool recursive = false)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);

                if (recursive)
                    baseKey.DeleteSubKeyTree(keyPath, false);
                else
                    baseKey.DeleteSubKey(keyPath, false);

                HistoryService.Log("Удалён ключ реестра", $@"[{hive}\{keyPath}]", "Registry");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка удаления ключа реестра",
                    $@"[{hive}\{keyPath}]: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static string[] GetSubKeyNames(RegistryHive hive, string keyPath)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetSubKeyNames() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка получения подключей",
                    $@"[{hive}\{keyPath}]: {ex.Message}",
                    "Registry");
                throw;
            }
        }

        public static string[] GetValueNames(RegistryHive hive, string keyPath)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetValueNames() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка получения значений",
                    $@"[{hive}\{keyPath}]: {ex.Message}",
                    "Registry");
                throw;
            }
        }
    }
}