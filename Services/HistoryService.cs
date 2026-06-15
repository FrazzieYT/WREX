using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using SystemManager.Models;

namespace SystemManager.Services
{
    public static class HistoryService
    {
        private static readonly string HistoryFilePath = GetHistoryFilePath();

        private static string GetHistoryFilePath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrEmpty(appData) && Directory.Exists(appData))
                {
                    return Path.Combine(appData, "WREX", "history.json");
                }
            }
            catch { }
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                return Path.Combine(baseDir, "WREX_Data", "history.json");
            }

            return Path.Combine(@"X:\Windows\Temp", "WREX_history.json");
        }

        private static ObservableCollection<HistoryEntry> _entries = new();
        private static bool _initialized = false;
        private static readonly object _lock = new();

        public static ObservableCollection<HistoryEntry> Entries
        {
            get
            {
                if (!_initialized) Load();
                return _entries;
            }
        }

        public static void Log(string action, string details, string category = "System")
        {
            lock (_lock)
            {
                if (!_initialized) Load();

                var entry = new HistoryEntry
                {
                    Timestamp = DateTime.Now,
                    Action = action,
                    Details = details,
                    Category = category
                };

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _entries.Insert(0, entry);
                });

                Save();
            }
        }

        public static void LogFileCreation(string filePath, string category = "File")
            => Log("Создание файла", filePath, category);

        public static void LogFileModification(string filePath, string category = "File")
            => Log("Изменение файла", filePath, category);

        public static void LogFileDeletion(string filePath, string category = "File")
            => Log("Удаление файла", filePath, category);

        public static void LogRegistryWrite(string keyPath, string valueName, object value, string category = "Registry")
            => Log("Запись в реестр", $"[{keyPath}] {valueName} = {value}", category);

        public static void LogRegistryRead(string keyPath, string valueName, object? value, string category = "Registry")
            => Log("Чтение реестра", $"[{keyPath}] {valueName} = {value ?? "(не установлено)"}", category);

        public static void Delete(HistoryEntry entry)
        {
            lock (_lock) { _entries.Remove(entry); Save(); }
        }

        public static void Clear()
        {
            lock (_lock) { _entries.Clear(); Save(); }
        }

        public static void Update(HistoryEntry entry, string newAction, string newDetails)
        {
            lock (_lock)
            {
                entry.Action = newAction;
                entry.Details = newDetails;
                Save();
            }
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(HistoryFilePath))
                    {
                        _entries = new ObservableCollection<HistoryEntry>();
                        _initialized = true;
                        return;
                    }

                    var json = File.ReadAllText(HistoryFilePath);
                    var list = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
                    _entries = new ObservableCollection<HistoryEntry>(list ?? new List<HistoryEntry>());
                    _initialized = true;
                }
                catch
                {
                    _entries = new ObservableCollection<HistoryEntry>();
                    _initialized = true;
                }
            }
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(HistoryFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                List<HistoryEntry> list;
                lock (_lock) { list = new List<HistoryEntry>(_entries); }

                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(HistoryFilePath, json);
            }
            catch { }
        }
    }
}