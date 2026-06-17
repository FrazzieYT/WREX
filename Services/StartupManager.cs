using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SystemManager.Services
{
    public static class StartupManager
    {
        public static List<StartupEntry> GetAllStartupEntries()
        {
            var entries = new List<StartupEntry>();

            AddFromRegistry(entries, Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU\\Run");
            AddFromRegistry(entries, Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU\\RunOnce");
            AddFromRegistry(entries, Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run");
            AddFromRegistry(entries, Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM\\RunOnce");
            AddFromRegistry(entries, Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run (x86)");

            AddFromFolder(entries,
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Startup Folder");
            AddFromFolder(entries,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\StartUp"), "Common Startup");

            return entries;
        }

        public static bool DeleteStartupEntry(StartupEntry entry)
        {
            try
            {
                if (entry.Source.StartsWith("HK"))
                {
                    var parts = entry.Source.Split('\\');
                    RegistryKey? rootKey = parts[0] switch
                    {
                        "HKCU" => Registry.CurrentUser,
                        "HKLM" => Registry.LocalMachine,
                        _ => null
                    };
                    if (rootKey == null) return false;
                    var subPath = string.Join("\\", parts, 1, parts.Length - 1);
                    using var key = rootKey.OpenSubKey(subPath, true);
                    key?.DeleteValue(entry.Name, false);
                    return true;
                }
                else if (entry.Source == "Startup Folder" || entry.Source == "Common Startup")
                {
                    if (File.Exists(entry.Command))
                    {
                        File.Delete(entry.Command);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static void AddFromRegistry(List<StartupEntry> entries, RegistryKey root, string subPath, string source)
        {
            try
            {
                using var key = root.OpenSubKey(subPath, false);
                if (key == null) return;

                foreach (var name in key.GetValueNames())
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    var value = key.GetValue(name)?.ToString() ?? "";
                    entries.Add(new StartupEntry
                    {
                        Name = name,
                        Command = value,
                        Source = source,
                        Type = "Registry"
                    });
                }
            }
            catch { }
        }

        private static void AddFromFolder(List<StartupEntry> entries, string folderPath, string source)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                foreach (var file in Directory.GetFiles(folderPath))
                {
                    entries.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Source = source,
                        Type = "Folder"
                    });
                }
            }
            catch { }
        }
    }

    public class StartupEntry
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Source { get; set; } = "";
        public string Type { get; set; } = "";

        public string SourceIcon => Source switch
        {
            var s when s.Contains("HKCU") => "👤",
            var s when s.Contains("HKLM") => "🔧",
            "Startup Folder" => "📁",
            "Common Startup" => "🌐",
            _ => "❓"
        };
    }
}
