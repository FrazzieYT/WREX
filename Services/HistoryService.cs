using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using SystemManager.Models;

namespace SystemManager.Services
{
    public static class HistoryService
    {
        private static readonly string HistoryFilePath = GetHistoryFilePath();
        private static ObservableCollection<HistoryEntry> _entries = new();
        private static bool _initialized = false;
        private static bool _dirty = false;
        private static Timer? _saveTimer;
        private static readonly object _lock = new();

        private static string GetHistoryFilePath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrEmpty(appData) && Directory.Exists(appData))
                    return Path.Combine(appData, "WREX", "history.json");
            }
            catch { }
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
                return Path.Combine(baseDir, "WREX_Data", "history.json");
            return Path.Combine(@"X:\Windows\Temp", "WREX_history.json");
        }

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
            try
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
                        if (_entries.Count > 500) _entries.RemoveAt(_entries.Count - 1);
                    });

                    _dirty = true;
                    ScheduleSave();
                }
            }
            catch { }
        }

        public static void Delete(HistoryEntry entry)
        {
            lock (_lock) { _entries.Remove(entry); _dirty = true; ScheduleSave(); }
        }

        public static void Clear()
        {
            lock (_lock) { _entries.Clear(); _dirty = true; ScheduleSave(); }
        }

        private static void ScheduleSave()
        {
            _saveTimer?.Dispose();
            _saveTimer = new Timer(_ => { if (_dirty) Save(); }, null, 2000, Timeout.Infinite);
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

        private static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(HistoryFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                List<HistoryEntry> list;
                lock (_lock) { list = new List<HistoryEntry>(_entries); _dirty = false; }

                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(HistoryFilePath, json);
            }
            catch { }
        }
    }
}
