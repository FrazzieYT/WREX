namespace SystemManager.ViewModels
{
    public class FileSystemItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsDirectory { get; set; }
        public bool IsArchive { get; set; }
        public string FormattedSize { get; set; } = "";
        public long FileSize { get; set; }
        public string? ArchiveParentPath { get; set; }
    }

    public class ProcessItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Threads { get; set; }
        public string StartTime { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long MemoryMb { get; set; }
        public bool IsCritical { get; set; }
        public string StatusIcon => IsCritical ? "🔴" : "⚪";
        public string StatusText => IsCritical ? "Системный" : "Обычный";
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
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class DriveInfoItem
    {
        public string Name { get; set; } = "";
        public string VolumeLabel { get; set; } = "";
        public long AvailableFreeSpace { get; set; }
        public long TotalSize { get; set; }
    }
}
