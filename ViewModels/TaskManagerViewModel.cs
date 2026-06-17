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
        private string _serviceSearchText = "";
        private ServiceInfo? _selectedService;
        private ObservableCollection<ServiceInfo> _allServices = new();
        private ObservableCollection<ServiceInfo> _filteredServices = new();

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
        public ObservableCollection<ServiceInfo> FilteredServices { get => _filteredServices; set => SetProperty(ref _filteredServices, value); }
        public ServiceInfo? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }
        public string ServiceSearchText { get => _serviceSearchText; set { _serviceSearchText = value; OnPropertyChanged(); ApplyServiceFilter(); } }

        public ICommand RefreshCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand RestartProcessCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand ShowPropertiesCommand { get; }
        public ICommand CopyProcessNameCommand { get; }
        public ICommand RunProcessCommand { get; }
        public ICommand RefreshStartupCommand { get; }
        public ICommand DeleteStartupEntryCommand { get; }
        public ICommand AddStartupEntryCommand { get; }
        public ICommand RefreshServicesCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand RestartServiceCommand { get; }
        public ICommand SetServiceAutoCommand { get; }
        public ICommand SetServiceManualCommand { get; }
        public ICommand SetServiceDisabledCommand { get; }
        public ICommand RenameStartupEntryCommand { get; }
        public ICommand ChangeServiceCommand { get; }
        public ICommand AddServiceCommand { get; }

        public TaskManagerViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await RefreshProcessesAsync());
            KillCommand = new RelayCommand(_ => KillProcess(), _ => SelectedProcess != null);
            RestartProcessCommand = new RelayCommand(RestartProcess);
            OpenFileLocationCommand = new RelayCommand(OpenFileLocation);
            ShowPropertiesCommand = new RelayCommand(ShowProperties);
            CopyProcessNameCommand = new RelayCommand(CopyProcessName);
            RunProcessCommand = new RelayCommand(_ => RunProcess());
            RefreshStartupCommand = new RelayCommand(_ => LoadStartupEntries());
            DeleteStartupEntryCommand = new RelayCommand(_ => DeleteStartupEntry(), _ => SelectedStartupEntry != null);
            AddStartupEntryCommand = new RelayCommand(_ => AddStartupEntry());
            RefreshServicesCommand = new RelayCommand(async _ => await LoadServicesAsync());
            StartServiceCommand = new RelayCommand(async _ => await StartServiceAsync(), _ => SelectedService?.Status != "Running");
            StopServiceCommand = new RelayCommand(async _ => await StopServiceAsync(), _ => SelectedService?.Status == "Running");
            RestartServiceCommand = new RelayCommand(async _ => await RestartServiceAsync(), _ => SelectedService?.Status == "Running");
            SetServiceAutoCommand = new RelayCommand(async _ => await SetServiceStartModeAsync("Auto"), _ => SelectedService != null);
            SetServiceManualCommand = new RelayCommand(async _ => await SetServiceStartModeAsync("Manual"), _ => SelectedService != null);
            SetServiceDisabledCommand = new RelayCommand(async _ => await SetServiceStartModeAsync("Disabled"), _ => SelectedService != null);
            RenameStartupEntryCommand = new RelayCommand(_ => RenameStartupEntry(), _ => SelectedStartupEntry != null);
            ChangeServiceCommand = new RelayCommand(param => ChangeServicePath(param as ServiceInfo));
            AddServiceCommand = new RelayCommand(_ => AddService());

            _ = RefreshProcessesAsync();
            LoadStartupEntries();
            _ = LoadServicesAsync();
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += async (_, _) => await System.Windows.Application.Current?.Dispatcher?.InvokeAsync(RefreshProcessesAsync)!;
            _refreshTimer.Start();
        }

        private static bool IsCritical(string name, string path)
        {
            if (CriticalProcesses.Contains(name)) return true;
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase)) return true;
            try { var ver = FileVersionInfo.GetVersionInfo(path); if (!string.IsNullOrEmpty(ver.CompanyName) && ver.CompanyName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return true; } catch { }
            return false;
        }

        private async Task RefreshProcessesAsync()
        {
            try
            {
                var previousId = SelectedProcess?.Id;
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
                                return new ProcessItem { Id = p.Id, Name = p.ProcessName, Description = "", MemoryMb = p.WorkingSet64 / 1024 / 1024, Threads = p.Threads.Count, StartTime = "", FullPath = path, IsCritical = IsCritical(p.ProcessName, path) };
                            } catch { return null; }
                        })
                        .OfType<ProcessItem>().ToList();
                });
                Processes = new ObservableCollection<ProcessItem>(list);
                if (previousId.HasValue)
                    SelectedProcess = list.FirstOrDefault(p => p.Id == previousId.Value);
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

        private void RunProcess()
        {
            var dialog = new InputDialog("Запуск программы", "Введите команду или путь к файлу:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    SystemLauncher.Launch(dialog.InputText);
                    _ = RefreshProcessesAsync();
                }
                catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
            }
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
                : new ObservableCollection<StartupEntry>(_startupEntries.Where(e => e.Name.Contains(_startupSearchText, StringComparison.OrdinalIgnoreCase) || e.Command.Contains(_startupSearchText, StringComparison.OrdinalIgnoreCase)));
        }

        private void DeleteStartupEntry()
        {
            if (SelectedStartupEntry == null) return;
            if (System.Windows.MessageBox.Show($"Удалить '{SelectedStartupEntry.Name}' из автозагрузки?", "Подтверждение", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                StartupManager.DeleteStartupEntry(SelectedStartupEntry);
                LoadStartupEntries();
            }
        }

        private void AddStartupEntry()
        {
            var dialog = new EditStartupDialog("", "", "HKCU\\Run");
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.NewName)) return;

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                key?.SetValue(dialog.NewName, dialog.NewCommand);
                LoadStartupEntries();
                System.Windows.MessageBox.Show($"Добавлено: {dialog.NewName}", "Готово");
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void RenameStartupEntry()
        {
            if (SelectedStartupEntry == null) return;

            var dialog = new EditStartupDialog(SelectedStartupEntry.Name, SelectedStartupEntry.Command, SelectedStartupEntry.Source);
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (regKey != null)
                {
                    regKey.DeleteValue(SelectedStartupEntry.Name, false);
                    regKey.SetValue(dialog.NewName, dialog.NewCommand);
                }
                LoadStartupEntries();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void ChangeServicePath(ServiceInfo? service)
        {
            if (service == null) return;
            if (RegistryService.IsWinRE())
            {
                System.Windows.MessageBox.Show("Управление службами ограничено в WinRE.", "Информация");
                return;
            }

            var dialog = new EditServiceDialog(service.Name, service.PathName, service.StartMode);
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Service WHERE Name='{service.Name}'");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    if (!string.IsNullOrWhiteSpace(dialog.NewPath) && dialog.NewPath != service.PathName)
                    {
                        var inParams = obj.GetMethodParameters("Change");
                        inParams["PathName"] = dialog.NewPath;
                        obj.InvokeMethod("Change", inParams, null);
                    }

                    if (!string.IsNullOrWhiteSpace(dialog.NewStartMode) && dialog.NewStartMode != service.StartMode)
                    {
                        WindowsServiceManager.SetStartMode(service.Name, dialog.NewStartMode);
                    }
                }
                System.Windows.MessageBox.Show("Параметры службы изменены. Требуется перезагрузка.", "Готово");
                _ = LoadServicesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private async Task LoadServicesAsync()
        {
            _allServices = new ObservableCollection<ServiceInfo>(await Task.Run(() => WindowsServiceManager.GetAllServices()));
            ApplyServiceFilter();
        }

        private void ApplyServiceFilter()
        {
            FilteredServices = string.IsNullOrWhiteSpace(_serviceSearchText)
                ? new ObservableCollection<ServiceInfo>(_allServices)
                : new ObservableCollection<ServiceInfo>(_allServices.Where(s => s.DisplayName.Contains(_serviceSearchText, StringComparison.OrdinalIgnoreCase) || s.Name.Contains(_serviceSearchText, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task StartServiceAsync()
        {
            if (SelectedService == null) return;
            await Task.Run(() => WindowsServiceManager.StartService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task StopServiceAsync()
        {
            if (SelectedService == null) return;
            await Task.Run(() => WindowsServiceManager.StopService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task RestartServiceAsync()
        {
            if (SelectedService == null) return;
            await Task.Run(() => WindowsServiceManager.RestartService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task SetServiceStartModeAsync(string mode)
        {
            if (SelectedService == null) return;
            await Task.Run(() => WindowsServiceManager.SetStartMode(SelectedService.Name, mode));
            await LoadServicesAsync();
        }

        private void AddService()
        {
            if (RegistryService.IsWinRE())
            {
                System.Windows.MessageBox.Show("Добавление служб ограничено в WinRE.", "Информация");
                return;
            }

            var dialog = new EditServiceDialog("НоваяСлужба", @"C:\path\to\service.exe", "Manual");
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.NewName)) return;

            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Service");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var inParams = obj.GetMethodParameters("Create");
                    if (inParams == null) continue;
                    inParams["Name"] = dialog.NewName;
                    inParams["PathName"] = dialog.NewPath;
                    inParams["StartMode"] = dialog.NewStartMode;
                    inParams["DisplayName"] = dialog.NewName;
                    obj.InvokeMethod("Create", inParams, null);
                }
                System.Windows.MessageBox.Show($"Служба '{dialog.NewName}' создана.", "Готово");
                _ = LoadServicesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _refreshTimer?.Dispose();
            _disposed = true;
        }
    }
}
