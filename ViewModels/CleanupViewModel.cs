using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class CleanupViewModel : ViewModelBase
    {
        private bool _isProcessing;
        private string _lastResult = "";
        private int _totalDeletedFiles;
        private int _totalDeletedDirectories;
        private long _totalFreedBytes;
        private string _statusMessage = "Готов к очистке";

        public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }
        public bool HasResult => !string.IsNullOrEmpty(_lastResult);
        public string LastResult { get => _lastResult; set { _lastResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasResult)); } }
        public int TotalDeletedFiles { get => _totalDeletedFiles; set => SetProperty(ref _totalDeletedFiles, value); }
        public int TotalDeletedDirectories { get => _totalDeletedDirectories; set => SetProperty(ref _totalDeletedDirectories, value); }
        public long TotalFreedBytes { get => _totalFreedBytes; set { _totalFreedBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedFreedBytes)); } }
        public string FormattedFreedBytes => FormatUtils.FormatSizeRu(TotalFreedBytes);
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand CleanTempCommand { get; }
        public ICommand CleanPrefetchCommand { get; }
        public ICommand CleanRecentCommand { get; }
        public ICommand CleanWindowsTempCommand { get; }
        public ICommand FullCleanupCommand { get; }

        public CleanupViewModel()
        {
            CleanTempCommand = new RelayCommand(async _ => await RunCleanupAsync("temp", CleanupService.CleanTemp));
            CleanPrefetchCommand = new RelayCommand(async _ => await RunCleanupAsync("prefetch", CleanupService.CleanPrefetch));
            CleanRecentCommand = new RelayCommand(async _ => await RunCleanupAsync("recent", CleanupService.CleanRecent));
            CleanWindowsTempCommand = new RelayCommand(async _ => await RunCleanupAsync("windows_temp", CleanupService.CleanWindowsTemp));
            FullCleanupCommand = new RelayCommand(async _ => await RunFullCleanupAsync());
        }

        private async Task RunCleanupAsync(string name, Func<CleanupResult> cleanupFunc)
        {
            if (IsProcessing) return;
            IsProcessing = true; StatusMessage = $"Очистка: {name}...";
            try
            {
                var result = await Task.Run(cleanupFunc);
                TotalDeletedFiles += result.DeletedFiles; TotalDeletedDirectories += result.DeletedDirectories; TotalFreedBytes += result.FreedBytes;
                LastResult = $"✓ {result.OperationName}\nУдалено файлов: {result.DeletedFiles}\nУдалено папок: {result.DeletedDirectories}\nОсвобождено: {FormatUtils.FormatSizeRu(result.FreedBytes)}";
            }
            finally { IsProcessing = false; }
        }

        private async Task RunFullCleanupAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true; StatusMessage = "Полная очистка...";
            try
            {
                var result = await Task.Run(CleanupService.RunFullCleanup);
                TotalDeletedFiles = result.DeletedFiles; TotalDeletedDirectories = result.DeletedDirectories; TotalFreedBytes = result.FreedBytes;
                LastResult = $"✓ Полная очистка\nВсего: {result.DeletedFiles} файлов, {result.DeletedDirectories} папок\nОсвобождено: {FormatUtils.FormatSizeRu(result.FreedBytes)}";
            }
            finally { IsProcessing = false; }
        }
    }
}
