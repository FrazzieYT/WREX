using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class StartupManagerViewModel : ViewModelBase
    {
        private ObservableCollection<StartupEntry> _entries = new();
        private ObservableCollection<StartupEntry> _filteredEntries = new();
        private StartupEntry? _selectedEntry;
        private string _searchText = "";

        public ObservableCollection<StartupEntry> Entries
        {
            get => _filteredEntries;
            set => SetProperty(ref _filteredEntries, value);
        }

        public StartupEntry? SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand DeleteEntryCommand { get; }

        public StartupManagerViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadEntries());
            DeleteEntryCommand = new RelayCommand(_ => DeleteEntry(), _ => SelectedEntry != null);
            LoadEntries();
        }

        private void LoadEntries()
        {
            _entries = new ObservableCollection<StartupEntry>(StartupManager.GetAllStartupEntries());
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Entries = string.IsNullOrWhiteSpace(_searchText)
                ? new ObservableCollection<StartupEntry>(_entries)
                : new ObservableCollection<StartupEntry>(_entries.Where(e =>
                    e.Name.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase) ||
                    e.Command.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase)));
        }

        private void DeleteEntry()
        {
            if (SelectedEntry == null) return;
            if (System.Windows.MessageBox.Show($"Удалить '{SelectedEntry.Name}' из автозагрузки?",
                "Подтверждение", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                StartupManager.DeleteStartupEntry(SelectedEntry);
                LoadEntries();
            }
        }
    }
}
