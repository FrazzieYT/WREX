using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SystemManager.Models;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class HistoryViewModel : ViewModelBase
    {
        private HistoryEntry? _selectedEntry;
        private string _filter = "";
        private string _categoryFilter = "Все";

        public ObservableCollection<HistoryEntry> Entries => HistoryService.Entries;

        public HistoryEntry? SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        public string Filter
        {
            get => _filter;
            set { _filter = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredEntries)); }
        }

        public string CategoryFilter
        {
            get => _categoryFilter;
            set { _categoryFilter = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredEntries)); }
        }

        public ObservableCollection<string> Categories { get; } = new()
        {
            "Все", "System", "File", "Registry", "Process", "Console", "Navigation", "Cleanup", "Monitor"
        };

        public ObservableCollection<HistoryEntry> FilteredEntries
        {
            get
            {
                var filtered = Entries.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "Все")
                    filtered = filtered.Where(e => e.Category == CategoryFilter);
                if (!string.IsNullOrWhiteSpace(Filter))
                    filtered = filtered.Where(e => e.Action.Contains(Filter, StringComparison.OrdinalIgnoreCase) || e.Details.Contains(Filter, StringComparison.OrdinalIgnoreCase) || e.Category.Contains(Filter, StringComparison.OrdinalIgnoreCase));
                return new ObservableCollection<HistoryEntry>(filtered);
            }
        }

        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }

        public HistoryViewModel()
        {
            DeleteCommand = new RelayCommand(_ => { if (SelectedEntry != null) { HistoryService.Delete(SelectedEntry); SelectedEntry = null; OnPropertyChanged(nameof(FilteredEntries)); } }, _ => SelectedEntry != null);
            ClearCommand = new RelayCommand(_ => { if (System.Windows.MessageBox.Show("Удалить всю историю?", "Подтверждение", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes) { HistoryService.Clear(); OnPropertyChanged(nameof(FilteredEntries)); } });
            RefreshCommand = new RelayCommand(_ => { HistoryService.Load(); OnPropertyChanged(nameof(FilteredEntries)); });
            ExportCommand = new RelayCommand(_ => Export());
            HistoryService.Log("Открыта история", "Пользователь открыл раздел истории", "Navigation");
        }

        private void Export()
        {
            try
            {
                var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"WREX_History_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                System.IO.File.WriteAllLines(path, FilteredEntries.Select(e => $"[{e.FormattedTime}] [{e.Category}] {e.Action}: {e.Details}"));
                System.Windows.MessageBox.Show($"История экспортирована в:\n{path}", "Экспорт");
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }
    }
}
