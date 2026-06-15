using Microsoft.Win32;

namespace SystemManager.Models
{
    public class FavoriteRegistryEntry
    {
        public RegistryHive Hive { get; set; }
        public string KeyPath { get; set; } = "";
        public string? ValueName { get; set; }
        public string DisplayName { get; set; } = "";
        
        public string Name => string.IsNullOrEmpty(ValueName) 
            ? (string.IsNullOrEmpty(KeyPath) ? DisplayName : System.IO.Path.GetFileName(KeyPath))
            : ValueName;
        
        public string Path => KeyPath;
        
        public string HiveDisplay => Hive.ToString();
    }
}