using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SystemManager.Services;

namespace SystemManager.Models
{
    public class RegistryValueItem : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name 
        { 
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public object? Value { get; set; }
        public RegistryValueKind Kind { get; set; }
        public string FullPath { get; set; } = "";
        public RegistryHive Hive { get; set; }
        
        public override string ToString()
        {
            return $"{Name} ({Kind}): {ValueDisplay}";
        }
        
        public string TypeDisplay => Kind.ToString();
        public string ValueDisplay => Value?.ToString() ?? "(не установлено)";
        public bool IsFavorite => RegistryService.IsFavorite(Hive, FullPath, Name);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}