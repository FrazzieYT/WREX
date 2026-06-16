using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SystemManager.Services;

namespace SystemManager.Models
{
    public class RegistryTreeNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;
        private bool _childrenLoaded;

        public RegistryHive? Hive { get; set; }
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool ChildrenLoaded
        {
            get => _childrenLoaded;
            set { _childrenLoaded = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RegistryTreeNode> Children { get; set; } = new();

        public bool IsRoot => Hive.HasValue && string.IsNullOrEmpty(FullPath);
        public bool IsHive => Hive.HasValue;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RegistryValueItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public object? Value { get; set; }
        public RegistryValueKind Kind { get; set; }
        public string FullPath { get; set; } = "";
        public RegistryHive Hive { get; set; }

        public string TypeDisplay => Kind.ToString();
        public string ValueDisplay => Value?.ToString() ?? "(не установлено)";
        public bool IsFavorite => RegistryService.IsFavorite(Hive, FullPath, Name);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RegistrySearchResult
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? Value { get; set; }
        public RegistryHive Hive { get; set; }
        public string KeyPath { get; set; } = "";
        public string FullPath => $@"{Path}\{Name}";
    }

    public class FavoriteRegistryEntry
    {
        public RegistryHive Hive { get; set; }
        public string KeyPath { get; set; } = "";
        public string? ValueName { get; set; }
        public string DisplayName { get; set; } = "";
    }
}