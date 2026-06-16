using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class TaskManagerViewModel : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<ProcessItem> _processes = new();
        private ProcessItem? _selectedProcess;
        private string _searchText = string.Empty;
        private readonly Timer _refreshTimer;
        private bool _disposed;

        public ObservableCollection<ProcessItem> Processes
        {
            get => _processes;
            set { _processes = value; OnPropertyChanged(); }
        }

        public ProcessItem? SelectedProcess
        {
            get => _selectedProcess;
            set 
            { 
                _selectedProcess = value; 
                OnPropertyChanged(); 
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                RefreshProcesses();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand RestartProcessCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand ShowPropertiesCommand { get; }
        public ICommand CopyProcessNameCommand { get; }

        public TaskManagerViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshProcesses());
            KillCommand = new RelayCommand(_ => KillProcess(), _ => SelectedProcess != null);
            
            RestartProcessCommand = new RelayCommand(RestartProcess);
            OpenFileLocationCommand = new RelayCommand(OpenFileLocation);
            ShowPropertiesCommand = new RelayCommand(ShowProperties);
            CopyProcessNameCommand = new RelayCommand(CopyProcessName);
            
            RefreshProcesses();

            _refreshTimer = new Timer(3000);
            _refreshTimer.Elapsed += (_, _) => 
                System.Windows.Application.Current?.Dispatcher?.Invoke(RefreshProcesses);
            _refreshTimer.Start();
        }

        private static string GetFullPath(Process p)
        {
            try { return p.MainModule?.FileName ?? string.Empty; } 
            catch { return string.Empty; }
        }
        
        private static string GetDescription(Process p)
        {
            try { return p.MainWindowTitle; } 
            catch { return string.Empty; }
        }

        private static string GetStartTime(Process p)
        {
            try { return p.StartTime.ToString("HH:mm:ss"); } 
            catch { return "N/A"; }
        }

        private void RestartProcess(object? parameter)
        {
            if (parameter is not ProcessItem process || string.IsNullOrEmpty(process.FullPath)) return;

            try
            {
                var proc = Process.GetProcessById(process.Id);
                proc.Kill();
                proc.WaitForExit(1000);
                
                SystemLauncher.Launch(process.FullPath);
                
                HistoryService.Log("Перезапуск процесса", $"Имя: {process.Name}, PID: {process.Id}", "TaskManager");
                RefreshProcesses();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Не удалось перезапустить: {ex.Message}", "Ошибка");
            }
        }
        
        private void OpenFileLocation(object? parameter)
        {
            if (parameter is not ProcessItem process || string.IsNullOrEmpty(process.FullPath)) return;

            try
            {
                SystemLauncher.Launch("explorer.exe", $"/select,\"{process.FullPath}\"");
                HistoryService.Log("Открыто расположение файла", process.FullPath, "TaskManager");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Не удалось открыть: {ex.Message}", "Ошибка");
            }
        }
        
        private void ShowProperties(object? parameter)
        {
            if (parameter is not ProcessItem process) return;
            
            var info = $"Имя: {process.Name}\n" +
                       $"Описание: {process.Description}\n" +
                       $"PID: {process.Id}\n" +
                       $"Память: {process.MemoryMb} МБ\n" +
                       $"Потоки: {process.Threads}\n" +
                       $"Путь: {process.FullPath}";
            
            System.Windows.MessageBox.Show(info, $"Свойства: {process.Name}", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private void CopyProcessName(object? parameter)
        {
            if (parameter is not ProcessItem process) return;

            System.Windows.Clipboard.SetText(process.Name);
            HistoryService.Log("Скопировано имя процесса", process.Name, "TaskManager");
        }
        
        private void RefreshProcesses()
        {
            try
            {
                var query = Process.GetProcesses()
                    .Where(p => string.IsNullOrWhiteSpace(SearchText) || 
                                p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                    .Select(p =>
                    {
                        try
                        {
                            return new ProcessItem
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                Description = GetDescription(p),
                                MemoryMb = p.WorkingSet64 / 1024 / 1024,
                                Threads = p.Threads.Count,
                                StartTime = GetStartTime(p),
                                FullPath = GetFullPath(p)
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .OfType<ProcessItem>()
                    .ToList();

                Processes = new ObservableCollection<ProcessItem>(query);
            }
            catch
            {
                // Игнорируем
            }
        }

        private void KillProcess()
        { 
            if (SelectedProcess == null) return;

            try
            {
                var process = Process.GetProcessById(SelectedProcess.Id);
                var name = process.ProcessName;
                process.Kill();
                
                HistoryService.Log("Завершён процесс", $"PID: {SelectedProcess.Id}, Имя: {name}", "Process");
                RefreshProcesses();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Не удалось завершить процесс {SelectedProcess.Name}:\n{ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _refreshTimer?.Dispose();
            _disposed = true;
        }
        
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}