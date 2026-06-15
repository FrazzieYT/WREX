using Microsoft.Win32;

namespace SystemManager.Models
{
    public class RegistrySearchResult
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Value { get; set; } = "";
        public RegistryHive Hive { get; set; }
        public string KeyPath { get; set; } = "";
        public string FullPath => string.IsNullOrEmpty(KeyPath) ? Name : $@"{Path}\{Name}";
    }
}