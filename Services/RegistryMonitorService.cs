using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;

namespace SystemManager.Services
{
    public static class RegistryMonitorService
    {
        private static readonly ConcurrentDictionary<string, RegistryKeyInfo> _watchedKeys = new();
        private static Timer? _pollTimer;
        private static bool _isRunning;
        private static readonly object _lock = new();
        private const int PollIntervalMs = 2000;

        public static bool IsRunning => _isRunning;

        public static event Action<string, string, string>? OnRegistryChange;

        private static readonly List<RegistryWatchEntry> _defaultWatchEntries = new()
        {
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"),
            new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"),
            new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services"),
        };

        public static void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;

                foreach (var entry in _defaultWatchEntries)
                {
                    AddWatch(entry.Hive, entry.KeyPath);
                }

                _pollTimer = new Timer(_ => PollChanges(), null, PollIntervalMs, PollIntervalMs);
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _pollTimer?.Dispose();
                _pollTimer = null;
                _watchedKeys.Clear();
            }
        }

        public static void AddWatch(RegistryHive hive, string keyPath)
        {
            var fullPath = $"{hive}\\{keyPath}";
            if (_watchedKeys.ContainsKey(fullPath)) return;

            var info = TakeSnapshot(hive, keyPath);
            if (info != null)
            {
                _watchedKeys[fullPath] = info;
            }
        }

        public static void RemoveWatch(RegistryHive hive, string keyPath)
        {
            var fullPath = $"{hive}\\{keyPath}";
            _watchedKeys.TryRemove(fullPath, out _);
        }

        private static void PollChanges()
        {
            foreach (var kvp in _watchedKeys)
            {
                var parts = kvp.Key.Split(new[] { '\\' }, 2);
                if (parts.Length < 2) continue;

                if (!Enum.TryParse<RegistryHive>(parts[0], out var hive)) continue;
                var keyPath = parts[1];

                var current = TakeSnapshot(hive, keyPath);
                if (current == null) continue;

                var previous = kvp.Value;

                foreach (var name in current.ValueNames)
                {
                    if (!previous.Values.ContainsKey(name))
                    {
                        var change = $"Создано: [{kvp.Key}] {name}";
                        HistoryService.Log("Изменение реестра", change, "Registry");
                        OnRegistryChange?.Invoke(kvp.Key, name, "created");
                    }
                    else if (current.Values.TryGetValue(name, out var curVal) &&
                             previous.Values.TryGetValue(name, out var prevVal) &&
                             curVal != prevVal)
                    {
                        var change = $"Изменено: [{kvp.Key}] {name} = {curVal}";
                        HistoryService.Log("Изменение реестра", change, "Registry");
                        OnRegistryChange?.Invoke(kvp.Key, name, "modified");
                    }
                }

                foreach (var name in previous.ValueNames)
                {
                    if (!current.Values.ContainsKey(name))
                    {
                        var change = $"Удалено: [{kvp.Key}] {name}";
                        HistoryService.Log("Изменение реестра", change, "Registry");
                        OnRegistryChange?.Invoke(kvp.Key, name, "deleted");
                    }
                }

                _watchedKeys[kvp.Key] = current;
            }
        }

        private static RegistryKeyInfo? TakeSnapshot(RegistryHive hive, string keyPath)
        {
            try
            {
                var baseKey = hive switch
                {
                    RegistryHive.ClassesRoot => Registry.ClassesRoot,
                    RegistryHive.CurrentUser => Registry.CurrentUser,
                    RegistryHive.LocalMachine => Registry.LocalMachine,
                    RegistryHive.Users => Registry.Users,
                    RegistryHive.CurrentConfig => Registry.CurrentConfig,
                    _ => null
                };

                using var key = baseKey?.OpenSubKey(keyPath, false);
                if (key == null) return null;

                var info = new RegistryKeyInfo();
                var valueNames = key.GetValueNames();
                info.ValueNames = valueNames;

                foreach (var name in valueNames)
                {
                    var val = key.GetValue(name);
                    info.Values[name] = val?.ToString() ?? "";
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        private class RegistryKeyInfo
        {
            public string[] ValueNames { get; set; } = Array.Empty<string>();
            public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private record RegistryWatchEntry(RegistryHive Hive, string KeyPath);
    }
}
