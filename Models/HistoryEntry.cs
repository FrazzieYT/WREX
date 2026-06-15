using System;

namespace SystemManager.Models
{
    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public string Category { get; set; } = ""; // "System", "Process", "Console", "Navigation", "Registry", "File"
        
        public string ActionType { get; set; } = ""; 
        public string TargetPath { get; set; } = "";
        /*public override string ToString()
        {
            // Более универсальный вариант
            if (!string.IsNullOrEmpty(TargetPath))
                return $"[{ActionType}] {TargetPath} - {FormattedTime}";
    
            return $"[{Action}] {Details} - {FormattedTime}";
        }*/
        
        public override string ToString()
        {
            return $"{Timestamp:G} - {Action}: {Details}";
        }
        
        public string FormattedTime => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
    }
}