using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SystemManager.ViewModels
{
    public class FileSystemItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsDirectory { get; set; }
        public string FormattedSize { get; set; } = "";
        public long FileSize { get; set; }
    }

    public class ProcessItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Threads { get; set; }
        public string StartTime { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long MemoryMb { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class UtilityItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Arguments { get; set; } = "";
    }

    public class QuickCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class DriveInfoItem
    {
        public string Name { get; set; } = "";
        public string VolumeLabel { get; set; } = "";
        public long AvailableFreeSpace { get; set; }
        public long TotalSize { get; set; }
    }
}