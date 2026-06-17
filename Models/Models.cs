namespace SystemManager.Models
{
    public class HistoryEntry
    {
        public System.DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public string Category { get; set; } = "";
        public string FormattedTime => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
        public override string ToString() => $"[{FormattedTime}] {Action}: {Details}";
    }
}
