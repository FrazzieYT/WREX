using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SystemManager.Services
{
    public static class FileMonitorService
    {
        private static readonly List<FileSystemWatcher> _creationWatchers = new();
        private static readonly List<FileSystemWatcher> _accessWatchers = new();
        private static readonly ConcurrentDictionary<string, DateTime> _recentWrexOperations = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _wrexDirectories = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _watchedAccessFiles = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isRunning = false;
        private static readonly object _lock = new();
        private static Timer? _cleanupTimer;

        static FileMonitorService()
        {
            AddWrexDirectory(AppPaths.AppDirectory);
            AddWrexDirectory(AppPaths.DataDirectory);
            AddWrexDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WREX"));
            AddWrexDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WREX"));
        }

        private static void AddWrexDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                _wrexDirectories.Add(Path.GetFullPath(path).TrimEnd('\\').ToLowerInvariant());
            }
        }
        
        public static void MarkWrexOperation(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var fullPath = Path.GetFullPath(path).ToLowerInvariant();
                _recentWrexOperations[fullPath] = DateTime.Now;
                
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _recentWrexOperations[dir] = DateTime.Now;
                }
            }
            catch { }
        }
        
        public static void AddWatchedAccessFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                var fullPath = Path.GetFullPath(filePath).ToLowerInvariant();
                _watchedAccessFiles.Add(fullPath);
                
                if (_isRunning)
                {
                    var dir = Path.GetDirectoryName(fullPath);
                    var name = Path.GetFileName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        StartAccessWatcher(dir, name);
                    }
                }
            }
            catch { }
        }

        public static void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
                
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        StartCreationWatcher(drive.RootDirectory.FullName);
                    }
                }
                
                foreach (var file in _watchedAccessFiles)
                {
                    var dir = Path.GetDirectoryName(file);
                    var name = Path.GetFileName(file);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        StartAccessWatcher(dir, name);
                    }
                }
                _cleanupTimer = new Timer(_ => CleanupOldOperations(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _cleanupTimer?.Dispose();
                _cleanupTimer = null;

                foreach (var watcher in _creationWatchers.Concat(_accessWatchers))
                {
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                    }
                    catch { }
                }
                _creationWatchers.Clear();
                _accessWatchers.Clear();
            }
        }

        private static void StartCreationWatcher(string path)
        {
            try
            {
                var watcher = new FileSystemWatcher
                {
                    Path = path,
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    IncludeSubdirectories = true,
                    InternalBufferSize = 128 * 1024,
                };

                watcher.Created += OnCreated;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;
                _creationWatchers.Add(watcher);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to watch creation in {path}: {ex.Message}");
            }
        }

        private static void StartAccessWatcher(string path, string filter)
        {
            try
            {
                var watcher = new FileSystemWatcher
                {
                    Path = path,
                    Filter = filter,
                    NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    InternalBufferSize = 64 * 1024,
                };

                watcher.Changed += OnAccessed;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;
                _accessWatchers.Add(watcher);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to watch access in {path} for {filter}: {ex.Message}");
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath)) return;

            bool isDirectory = false;
            try { isDirectory = Directory.Exists(e.FullPath); } catch { }

            string action = isDirectory ? "Создана папка сторонним приложением" : "Создан файл сторонним приложением";
            HistoryService.Log(action, e.FullPath, "Monitor");
        }

        private static void OnAccessed(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnore(e.FullPath)) return;
            if (!IsWatchedAccessFile(e.FullPath)) return;

            HistoryService.Log("Обращение к файлу", e.FullPath, "Monitor");
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Watcher error: {e.GetException().Message}");
        }

        private static bool ShouldIgnore(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            var lowerPath = Path.GetFullPath(path).ToLowerInvariant();
            
            foreach (var wrexDir in _wrexDirectories)
            {
                if (lowerPath.StartsWith(wrexDir + "\\") || lowerPath == wrexDir) return true;
            }
            
            if (_recentWrexOperations.TryGetValue(lowerPath, out var time))
            {
                if ((DateTime.Now - time).TotalSeconds < 5) return true;
            }
            
            if (lowerPath.Contains("\\windows\\temp\\") || 
                lowerPath.Contains("\\windows\\prefetch\\") ||
                lowerPath.Contains("\\inetpub\\logs\\") ||
                lowerPath.Contains("\\$recycle.bin\\") ||
                lowerPath.Contains("\\system volume information\\") ||
                lowerPath.EndsWith("\\ntuser.dat") ||
                lowerPath.Contains("\\appdata\\local\\microsoft\\windows\\explorer\\thumbcache_"))
            {
                return true;
            }

            return false;
        }

        private static bool IsWatchedAccessFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return _watchedAccessFiles.Contains(Path.GetFullPath(path).ToLowerInvariant());
        }

        private static void CleanupOldOperations()
        {
            var threshold = DateTime.Now.AddSeconds(-10);
            foreach (var kvp in _recentWrexOperations)
            {
                if (kvp.Value < threshold)
                {
                    _recentWrexOperations.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}