using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace SystemManager.ViewModels
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
}