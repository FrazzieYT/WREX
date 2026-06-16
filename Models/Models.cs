using System;

namespace SystemManager.Models
{
    public class DriveInfoModel
    {
        public string Name { get; set; } = "";
        public string VolumeLabel { get; set; } = "";
        public long AvailableFreeSpace { get; set; }
        public long TotalSize { get; set; }
    }

    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public string Category { get; set; } = "";

        public string FormattedTime => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
    }
}