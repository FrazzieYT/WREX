using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        public string ComputerName => Environment.MachineName;
        public string UserName => Environment.UserName;
        public string OsVersion => Environment.OSVersion.ToString();
        public string DotNetVersion => Environment.Version.ToString();
        public int ProcessorCount => Environment.ProcessorCount;
        public bool Is64Bit => Environment.Is64BitOperatingSystem;
        public string SystemDir => Environment.SystemDirectory;
        
        public bool IsWinRe => RegistryService.IsWinRE();
        
        public string EnvironmentMode => IsWinRe ? "🔧 Windows RE (WinPE)" : "🖥️ Обычная ОС";

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
                    try 
                    { 
                        label = drive.VolumeLabel; 
                    } 
                    catch 
                    { 
                        // Игнорируем
                    }

                    Drives.Add(new DriveInfoItem
                    {
                        Name = drive.Name + (drive.Name.StartsWith(@"X:\") ? " (WinRE)" : ""),
                        VolumeLabel = label,
                        AvailableFreeSpace = drive.AvailableFreeSpace,
                        TotalSize = drive.TotalSize
                    });
                }
            }
            
            HistoryService.Log(
                "Главная",
                IsWinRe ? "Запущено в WinRE" : "Запущено в обычной ОС"
            );
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}