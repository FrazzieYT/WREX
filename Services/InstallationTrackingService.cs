using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SystemManager.Services
{
    public static class InstallationTrackingService
    {
        private static bool _isTracking;
        private static CancellationTokenSource? _cts;
        private static readonly ConcurrentDictionary<string, DateTime> _initialFiles = new();
        private static readonly ConcurrentDictionary<string, DateTime> _initialRegistryKeys = new();
        private static readonly List<FileSystemWatcher> _watchers = new();
        private static readonly List<string> Drives = new() { @"C:\", @"D:\", @"E:\" };
        private static readonly string[] RegistryPaths = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
        };

        public static bool IsTracking => _isTracking;
        public static event Action<string, string, string>? OnChangeDetected;

        public static void StartTracking()
        {
            if (_isTracking) return;
            _isTracking = true;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => SnapshotCurrentState());
            StartWatchingDrives();
            HistoryService.Log("Трекинг запущен", "Мониторинг изменений", "System");
        }

        public static void StopTracking()
        {
            if (!_isTracking) return;
            _isTracking = false;
            _cts?.Cancel(); _cts = null;
            foreach (var w in _watchers) { try { w.EnableRaisingEvents = false; w.Dispose(); } catch { } }
            _watchers.Clear();
            _ = Task.Run(() =>
            {
                var changes = DetectChanges();
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    if (changes.Count > 0)
                        System.Windows.MessageBox.Show($"Обнаружено изменений: {changes.Count}\n\n{string.Join("\n", changes)}", "Результат");
                });
            });
            HistoryService.Log("Трекинг завершён", "Остановлен", "System");
        }

        private static void SnapshotCurrentState()
        {
            _initialFiles.Clear(); _initialRegistryKeys.Clear();
            foreach (var drive in Drives)
            {
                if (!Directory.Exists(drive)) continue;
                try
                {
                    foreach (var file in Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories))
                    {
                        try { _initialFiles[file.ToLowerInvariant()] = new FileInfo(file).LastWriteTimeUtc; } catch { }
                    }
                } catch { }
            }
            foreach (var path in RegistryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path, false);
                    if (key != null) foreach (var sub in key.GetSubKeyNames()) _initialRegistryKeys[$@"HKLM\{path}\{sub}".ToLowerInvariant()] = DateTime.UtcNow;
                } catch { }
            }
        }

        private static void StartWatchingDrives()
        {
            foreach (var drive in Drives)
            {
                if (!Directory.Exists(drive)) continue;
                try
                {
                    var watcher = new FileSystemWatcher(drive) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite, IncludeSubdirectories = true, EnableRaisingEvents = true, InternalBufferSize = 64 * 1024 };
                    watcher.Created += OnFileChanged; watcher.Changed += OnFileChanged; watcher.Deleted += OnFileChanged; watcher.Renamed += (_, e) => OnFileChanged(null, new FileSystemEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(e.FullPath) ?? "", e.Name));
                    _watchers.Add(watcher);
                } catch { }
            }
            Task.Run(async () =>
            {
                while (_isTracking && _cts != null && !_cts.Token.IsCancellationRequested)
                {
                    MonitorRegistry();
                    await Task.Delay(5000, _cts.Token).ConfigureAwait(false);
                }
            }, _cts?.Token ?? CancellationToken.None);
        }

        private static void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            if (!_isTracking || FormatUtils.ShouldIgnorePath(e.FullPath)) return;
            OnChangeDetected?.Invoke("File", e.FullPath, e.ChangeType.ToString());
            HistoryService.Log("Изменение (трекинг)", $"[{e.ChangeType}] {e.FullPath}", "System");
        }

        private static void MonitorRegistry()
        {
            if (!_isTracking) return;
            foreach (var path in RegistryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path, false);
                    if (key != null) foreach (var sub in key.GetSubKeyNames())
                    {
                        var full = $@"HKLM\{path}\{sub}".ToLowerInvariant();
                        if (!_initialRegistryKeys.ContainsKey(full)) { OnChangeDetected?.Invoke("Registry", full, "Created"); _initialRegistryKeys[full] = DateTime.UtcNow; }
                    }
                } catch { }
            }
        }

        private static List<string> DetectChanges()
        {
            var changes = new List<string>();
            foreach (var drive in Drives)
            {
                if (!Directory.Exists(drive)) continue;
                try
                {
                    foreach (var file in Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var fi = new FileInfo(file); var lower = file.ToLowerInvariant();
                            if (!_initialFiles.ContainsKey(lower)) changes.Add($"[+] {file}");
                            else if (_initialFiles[lower] != fi.LastWriteTimeUtc) changes.Add($"[~] {file}");
                        } catch { }
                    }
                } catch { }
            }
            return changes;
        }
    }
}
