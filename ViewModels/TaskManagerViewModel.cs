using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class TaskManagerViewModel : ViewModelBase, IDisposable
    {
        private ObservableCollection<ProcessItem> _processes = new();
        private ProcessItem? _selectedProcess;
        private string _searchText = "";
        private readonly System.Timers.Timer _refreshTimer;
        private bool _disposed;
        private ObservableCollection<StartupEntry> _startupEntries = new();
        private ObservableCollection<StartupEntry> _filteredStartupEntries = new();
        private StartupEntry? _selectedStartupEntry;
        private string _startupSearchText = "";

        private static readonly HashSet<string> CriticalProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass",
            "svchost", "dwm", "fontdrvhost", "sihost", "taskhostw", "RuntimeBroker",
            "ShellExperienceHost", "StartMenuExperienceHost", "ctfmon", "conhost",
            "spoolsv", "WmiPrvSE", "SearchIndexer", "dllhost", "msdtc",
            "SecurityHealthService", "MsMpEng", "NisSrv", "WUDFHost", "WerFault",
            "ntoskrnl", "mcupdate", "Classpnp", "disk", "partmgr", "volmgr",
            "mountmgr", "intelppm", "ACPI", "Wdf01000", "mssmbios",
            "Tcpip", "Afd", "NetBT", "nsiproxy", "mpsdrv", "tdx",
            "wfplwfs", "pacer", "mouclass", "kbdclass", "mouhid", "kbdhid",
            "Null", "Beep", "RDPCDD", "RDPDR", "RDPWD", "RasMan",
            "SstpSvc", "NetBIOS", "MrxSmb", "mrxsmb10", "mrxsmb20",
            "bowser", "WfpLwf", "Dnscache", "Dhcp", "EventLog", "Spooler",
            "Sysmon", "WdNisDrv", "WdNisSvc", "WdFilter", "BFE", "mpssvc"
        };

        public ObservableCollection<ProcessItem> Processes { get => _processes; set => SetProperty(ref _processes, value); }
        public ProcessItem? SelectedProcess { get => _selectedProcess; set => SetProperty(ref _selectedProcess, value); }
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); _ = RefreshProcessesAsync(); } }
        public ObservableCollection<StartupEntry> StartupEntries { get => _filteredStartupEntries; set => SetProperty(ref _filteredStartupEntries, value); }
        public StartupEntry? SelectedStartupEntry { get => _selectedStartupEntry; set => SetProperty(ref _selectedStartupEntry, value); }
        public string StartupSearchText { get => _startupSearchText; set { _startupSearchText = value; OnPropertyChanged(); ApplyStartupFilter(); } }

        public ICommand RefreshCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand RestartProcessCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand ShowPropertiesCommand { get; }
        public ICommand CopyProcessNameCommand { get; }
        public ICommand RefreshStartupCommand { get; }
        public ICommand DeleteStartupEntryCommand { get; }

        public TaskManagerViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await RefreshProcessesAsync());
            KillCommand = new RelayCommand(_ => KillProcess(), _ => SelectedProcess != null);
            RestartProcessCommand = new RelayCommand(RestartProcess);
            OpenFileLocationCommand = new RelayCommand(OpenFileLocation);
            ShowPropertiesCommand = new RelayCommand(ShowProperties);
            CopyProcessNameCommand = new RelayCommand(CopyProcessName);
            RefreshStartupCommand = new RelayCommand(_ => LoadStartupEntries());
            DeleteStartupEntryCommand = new RelayCommand(_ => DeleteStartupEntry(), _ => SelectedStartupEntry != null);

            _ = RefreshProcessesAsync();
            LoadStartupEntries();
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += async (_, _) => await System.Windows.Application.Current?.Dispatcher?.InvokeAsync(RefreshProcessesAsync)!;
            _refreshTimer.Start();
        }

        private static bool IsCritical(string name, string path)
        {
            if (CriticalProcesses.Contains(name)) return true;
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                var ver = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrEmpty(ver.CompanyName) && ver.CompanyName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return true;
            } catch { }
            return false;
        }

        private async Task RefreshProcessesAsync()
        {
            try
            {
                var list = await Task.Run(() =>
                {
                    return Process.GetProcesses()
                        .Where(p => string.IsNullOrWhiteSpace(SearchText) || p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                        .Select(p =>
                        {
                            try
                            {
                                string path = ""; try { path = p.MainModule?.FileName ?? ""; } catch { }
                                return new ProcessItem
                                {
                                    Id = p.Id, Name = p.ProcessName,
                                    Description = "", MemoryMb = p.WorkingSet64 / 1024 / 1024,
                                    Threads = p.Threads.Count, StartTime = "", FullPath = path,
                                    IsCritical = IsCritical(p.ProcessName, path)
                                };
                            } catch { return null; }
                        })
                        .OfType<ProcessItem>().ToList();
                });
                Processes = new ObservableCollection<ProcessItem>(list);
            } catch { }
        }

        private void RestartProcess(object? param)
        {
            if (param is not ProcessItem proc || string.IsNullOrEmpty(proc.FullPath)) return;
            try { var p = Process.GetProcessById(proc.Id); p.Kill(); p.WaitForExit(1000); SystemLauncher.Launch(proc.FullPath); _ = RefreshProcessesAsync(); }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void OpenFileLocation(object? param)
        {
            if (param is not ProcessItem proc || string.IsNullOrEmpty(proc.FullPath)) return;
            SystemLauncher.Launch("explorer.exe", $"/select,\"{proc.FullPath}\"");
        }

        private void ShowProperties(object? param)
        {
            if (param is not ProcessItem proc) return;
            System.Windows.MessageBox.Show($"Имя: {proc.Name}\nPID: {proc.Id}\nПамять: {proc.MemoryMb} МБ\nПотоки: {proc.Threads}\nПуть: {proc.FullPath}\nСтатус: {(proc.IsCritical ? "🔴 Системный" : "⚪ Обычный")}", $"Свойства: {proc.Name}");
        }

        private void CopyProcessName(object? param)
        {
            if (param is not ProcessItem proc) return;
            System.Windows.Clipboard.SetText(proc.Name);
        }

        private void KillProcess()
        {
            if (SelectedProcess == null) return;
            if (SelectedProcess.IsCritical) { System.Windows.MessageBox.Show("Нельзя завершить системный процесс!", "Внимание"); return; }
            try { Process.GetProcessById(SelectedProcess.Id).Kill(); _ = RefreshProcessesAsync(); }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void LoadStartupEntries()
        {
            _startupEntries = new ObservableCollection<StartupEntry>(StartupManager.GetAllStartupEntries());
            ApplyStartupFilter();
        }

        private void ApplyStartupFilter()
        {
            StartupEntries = string.IsNullOrWhiteSpace(_startupSearchText)
                ? new ObservableCollection<StartupEntry>(_startupEntries)
                : new ObservableCollection<StartupEntry>(_startupEntries.Where(e =>
                    e.Name.Contains(_startupSearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Command.Contains(_startupSearchText, StringComparison.OrdinalIgnoreCase)));
        }

        private void DeleteStartupEntry()
        {
            if (SelectedStartupEntry == null) return;
            if (System.Windows.MessageBox.Show($"Удалить '{SelectedStartupEntry.Name}' из автозагрузки?",
                "Подтверждение", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                StartupManager.DeleteStartupEntry(SelectedStartupEntry);
                LoadStartupEntries();
            }
        }

        public void Dispose() { if (!_disposed) { _refreshTimer?.Dispose(); _disposed = true; } }
    }
}
