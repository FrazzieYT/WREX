using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SystemManager.Models;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class HistoryViewModel : INotifyPropertyChanged
    {
        private HistoryEntry? _selectedEntry;
        private string _filter = "";
        private string _categoryFilter = "Все";

        public ObservableCollection<HistoryEntry> Entries => HistoryService.Entries;

        public HistoryEntry? SelectedEntry
        {
            get => _selectedEntry;
            set { _selectedEntry = value; OnPropertyChanged(); }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredEntries));
            }
        }

        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                _categoryFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredEntries));
            }
        }

        public ObservableCollection<string> Categories { get; } = new()
        {
            "Все", "System", "File", "Registry", "Process", "Console", "Navigation", "Cleanup"
        };

        public ObservableCollection<HistoryEntry> FilteredEntries
        {
            get
            {
                var filtered = Entries.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "Все")
                {
                    filtered = filtered.Where(e => e.Category == CategoryFilter);
                }

                if (!string.IsNullOrWhiteSpace(Filter))
                {
                    filtered = filtered.Where(e =>
                        e.Action.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                        e.Details.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                        e.Category.Contains(Filter, StringComparison.OrdinalIgnoreCase));
                }

                return new ObservableCollection<HistoryEntry>(filtered);
            }
        }

        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }

        public HistoryViewModel()
        {
            DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedEntry != null);
            ClearCommand = new RelayCommand(_ => ClearAll());
            RefreshCommand = new RelayCommand(_ => Refresh());
            ExportCommand = new RelayCommand(_ => Export());

            HistoryService.Log("Открыта история", "Пользователь открыл раздел истории", "Navigation");
        }

        private void DeleteSelected()
        {
            if (SelectedEntry != null)
            {
                HistoryService.Delete(SelectedEntry);
                SelectedEntry = null;
                OnPropertyChanged(nameof(FilteredEntries));
            }
        }

        private void ClearAll()
        {
            var result = System.Windows.MessageBox.Show(
                "Удалить всю историю действий?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                HistoryService.Clear();
                HistoryService.Log("История очищена", "Пользователь очистил всю историю", "System");
                OnPropertyChanged(nameof(FilteredEntries));
            }
        }

        private void Refresh()
        {
            HistoryService.Load();
            OnPropertyChanged(nameof(FilteredEntries));
        }

        private void Export()
        {
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"WREX_History_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                var lines = FilteredEntries.Select(e =>
                    $"[{e.FormattedTime}] [{e.Category}] {e.Action}: {e.Details}");

                System.IO.File.WriteAllLines(path, lines);
                HistoryService.Log("Экспорт истории", $"Экспортировано в {path}", "System");
                System.Windows.MessageBox.Show($"История экспортирована в:\n{path}", "Экспорт");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}