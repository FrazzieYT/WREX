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
    public class ProcessItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long MemoryMB { get; set; }
        public int Threads { get; set; }
        public string StartTime { get; set; } = "";
        public string FullPath { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TaskManagerViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ProcessItem> _processes = new();
        private ProcessItem? _selectedProcess;
        private string _searchText = "";
        private Timer _refreshTimer;

        public ObservableCollection<ProcessItem> Processes
        {
            get => _processes;
            set { _processes = value; OnPropertyChanged(); }
        }

        public ProcessItem? SelectedProcess
        {
            get => _selectedProcess;
            set { _selectedProcess = value; OnPropertyChanged(); }
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
            _refreshTimer.Elapsed += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => RefreshProcesses());
            };
            _refreshTimer.Start();
        }
        
        private string GetFullPath(Process p)
        {
            try { return p.MainModule?.FileName ?? ""; } 
            catch { return ""; }
        }
        
        private void RestartProcess(object? parameter)
        {
            if (parameter is ProcessItem process && !string.IsNullOrEmpty(process.FullPath))
            {
                try
                {
                    var proc = Process.GetProcessById(process.Id);
                    proc.Kill();
                    proc.WaitForExit(1000);
                    
                    Process.Start(process.FullPath);
                    HistoryService.Log("Перезапуск процесса", $"Имя: {process.Name}, Путь: {process.FullPath}", "TaskManager");
                    RefreshProcesses();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Не удалось перезапустить: {ex.Message}", "Ошибка");
                }
            }
        }
        
        private void OpenFileLocation(object? parameter)
        {
            if (parameter is ProcessItem process && !string.IsNullOrEmpty(process.FullPath))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{process.FullPath}\"");
                    HistoryService.Log("Открыто расположение файла", process.FullPath, "TaskManager");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Не удалось открыть: {ex.Message}", "Ошибка");
                }
            }
        }
        
        private void ShowProperties(object? parameter)
        {
            if (parameter is ProcessItem process)
            {
                var info = $"Имя: {process.Name}\n" +
                           $"Описание: {process.Description}\n" +
                           $"PID: {process.Id}\n" +
                           $"Память: {process.MemoryMB} МБ\n" +
                           $"Потоки: {process.Threads}\n" +
                           $"Путь: {process.FullPath}";
                
                System.Windows.MessageBox.Show(info, $"Свойства: {process.Name}", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        
        private void CopyProcessName(object? parameter)
        {
            if (parameter is ProcessItem process)
            {
                System.Windows.Clipboard.SetText(process.Name);
                HistoryService.Log("Скопировано имя процесса", process.Name, "TaskManager");
            }
        }
        
        public void RefreshProcesses()
        {
            try
            {
                var query = Process.GetProcesses()
                    .Where(p => string.IsNullOrWhiteSpace(SearchText) ||
                                p.ProcessName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderByDescending(p =>
                    {
                        try { return p.WorkingSet64; } catch { return 0L; }
                    })
                    .Select(p =>
                    {
                        try
                        {
                            return new ProcessItem
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                Description = GetDescription(p),
                                MemoryMB = p.WorkingSet64 / 1024 / 1024,
                                Threads = p.Threads.Count,
                                StartTime = GetStartTime(p),
                                FullPath = GetFullPath(p)
                            };
                        }
                        catch { return null; }
                    })
                    .Where(p => p != null)
                    .ToList();

                Processes = new ObservableCollection<ProcessItem>(query!);
            }
            catch { }
        }

        private string GetDescription(Process p)
        {
            try { return p.MainWindowTitle; } catch { return ""; }
        }

        private string GetStartTime(Process p)
        {
            try { return p.StartTime.ToString("HH:mm:ss"); } catch { return "N/A"; }
        }

        public void KillProcess()
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}