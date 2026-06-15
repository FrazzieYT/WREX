using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class CleanupViewModel : INotifyPropertyChanged
    {
        private bool _isProcessing;
        private string _lastResult = "";
        private int _totalDeletedFiles;
        private int _totalDeletedDirectories;
        private long _totalFreedBytes;
        private string _statusMessage = "Готов к очистке";

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public bool HasResult => !string.IsNullOrEmpty(_lastResult);

        public string LastResult
        {
            get => _lastResult;
            set 
            { 
                _lastResult = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasResult));
            }
        }

        public int TotalDeletedFiles
        {
            get => _totalDeletedFiles;
            set { _totalDeletedFiles = value; OnPropertyChanged(); }
        }

        public int TotalDeletedDirectories
        {
            get => _totalDeletedDirectories;
            set { _totalDeletedDirectories = value; OnPropertyChanged(); }
        }

        public long TotalFreedBytes
        {
            get => _totalFreedBytes;
            set { _totalFreedBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedFreedBytes)); }
        }

        public string FormattedFreedBytes => FormatSize(TotalFreedBytes);

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand CleanTempCommand { get; }
        public ICommand CleanPrefetchCommand { get; }
        public ICommand CleanRecentCommand { get; }
        public ICommand CleanWindowsTempCommand { get; }
        public ICommand FullCleanupCommand { get; }

        public CleanupViewModel()
        {
            CleanTempCommand = new RelayCommand(async _ => 
            {
                try { await ExecuteCleanupAsync("temp", CleanupService.CleanTemp); } 
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            });
            
            CleanPrefetchCommand = new RelayCommand(async _ => 
            {
                try { await ExecuteCleanupAsync("prefetch", CleanupService.CleanPrefetch); } 
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            });
            
            CleanRecentCommand = new RelayCommand(async _ => 
            {
                try { await ExecuteCleanupAsync("recent", CleanupService.CleanRecent); } 
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            });
            
            CleanWindowsTempCommand = new RelayCommand(async _ => 
            {
                try { await ExecuteCleanupAsync("windows_temp", CleanupService.CleanWindowsTemp); } 
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            });
            
            FullCleanupCommand = new RelayCommand(async _ => 
            {
                try { await ExecuteFullCleanupAsync(); } 
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            });
        }

        private async Task ExecuteCleanupAsync(string operationName, Func<CleanupResult> cleanupFunc)
        {
            if (IsProcessing) return;

            IsProcessing = true;
            StatusMessage = $"Выполняется очистка: {operationName}...";
            
            try
            {
                var result = await Task.Run(cleanupFunc);
                
                TotalDeletedFiles += result.DeletedFiles;
                TotalDeletedDirectories += result.DeletedDirectories;
                TotalFreedBytes += result.FreedBytes;
                
                LastResult = $"✓ {result.OperationName} завершена\n" +
                           $"Удалено файлов: {result.DeletedFiles}\n" +
                           $"Удалено папок: {result.DeletedDirectories}\n" +
                           $"Освобождено: {FormatSize(result.FreedBytes)}\n" +
                           $"Пропущено: {result.SkippedItems}";
                
                StatusMessage = $"Очистка {operationName} завершена";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ExecuteFullCleanupAsync()
        {
            if (IsProcessing) return;

            IsProcessing = true;
            StatusMessage = "Выполняется полная очистка системы...";
            
            try
            {
                var result = await Task.Run(CleanupService.RunFullCleanup);
                
                TotalDeletedFiles = result.DeletedFiles;
                TotalDeletedDirectories = result.DeletedDirectories;
                TotalFreedBytes = result.FreedBytes;
                
                LastResult = $"✓ Полная очистка завершена\n" +
                           $"Всего удалено файлов: {result.DeletedFiles}\n" +
                           $"Всего удалено папок: {result.DeletedDirectories}\n" +
                           $"Всего освобождено: {FormatSize(result.FreedBytes)}\n" +
                           $"Пропущено: {result.SkippedItems}";
                
                StatusMessage = "Полная очистка завершена успешно";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}