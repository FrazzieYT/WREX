using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class DriveInfoItem
    {
        public string Name { get; set; } = "";
        public string VolumeLabel { get; set; } = "";
        public long AvailableFreeSpace { get; set; }
        public long TotalSize { get; set; }
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public string ComputerName => Environment.MachineName;
        public string UserName => Environment.UserName;
        public string OsVersion => Environment.OSVersion.ToString();
        public string DotNetVersion => Environment.Version.ToString();
        public int ProcessorCount => Environment.ProcessorCount;
        public bool Is64Bit => Environment.Is64BitOperatingSystem;
        public string SystemDir => Environment.SystemDirectory;

        public bool IsWinRE => RegistryService.IsWinRE();
        public string EnvironmentMode => IsWinRE ? "🔧 Windows RE (WinPE)" : "🖥️ Обычная ОС";

        public string OfflineWindowsPath
        {
            get
            {
                var path = RegistryService.DetectOfflineWindowsPath();
                return string.IsNullOrEmpty(path) ? "Не обнаружена" : path;
            }
        }

        public ObservableCollection<DriveInfoItem> Drives { get; } = new();

        public DashboardViewModel()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string label = "";
                    try { label = drive.VolumeLabel; } catch { }

                    Drives.Add(new DriveInfoItem
                    {
                        Name = drive.Name + (drive.Name.StartsWith(@"X:\") ? " (WinRE)" : ""),
                        VolumeLabel = label,
                        AvailableFreeSpace = drive.AvailableFreeSpace,
                        TotalSize = drive.TotalSize
                    });
                }
            }

            HistoryService.Log("Главная",
                IsWinRE ? "Запущено в WinRE" : "Запущено в обычной ОС",
                "System");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}