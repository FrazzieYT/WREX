using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class UtilitiesViewModel : ViewModelBase
    {
        public ObservableCollection<UtilityItem> Utilities { get; } = new();
        public ObservableCollection<RestorePointInfo> RestorePoints { get; } = new();
        public ICommand LaunchCommand { get; }
        public ICommand RefreshRestorePointsCommand { get; }
        public ICommand CreateRestorePointCommand { get; }
        public ICommand ExportSystemInfoCommand { get; }
        public ICommand ExportHiveCommand { get; }
        public ICommand ImportHiveCommand { get; }
        public ICommand RestoreAllCommand { get; }
        public ICommand SimulateHotkeyCommand { get; }
        public string RegistryStatus { get => _registryStatus; set => SetProperty(ref _registryStatus, value); }
        public string UnlockStatus { get => _unlockStatus; set => SetProperty(ref _unlockStatus, value); }
        private string _registryStatus = "";
        private string _unlockStatus = "";

        public UtilitiesViewModel()
        {
            bool isWinRe = RegistryService.IsWinRE();
            Utilities.Add(new UtilityItem { Name = "Командная строка", Description = "cmd.exe", FileName = "cmd.exe" });
            Utilities.Add(new UtilityItem { Name = "PowerShell", Description = "powershell.exe", FileName = "powershell.exe" });
            Utilities.Add(new UtilityItem { Name = "Редактор реестра", Description = "regedit", FileName = "regedit.exe" });
            Utilities.Add(new UtilityItem { Name = "Блокнот", Description = "notepad.exe", FileName = "notepad.exe" });
            Utilities.Add(new UtilityItem { Name = "Диспетчер задач", Description = "taskmgr", FileName = "taskmgr.exe" });
            if (isWinRe)
            {
                Utilities.Add(new UtilityItem { Name = "DiskPart", Description = "diskpart.exe", FileName = "diskpart.exe" });
                Utilities.Add(new UtilityItem { Name = "BootRec", Description = "bootrec.exe", FileName = "cmd.exe", Arguments = "/k bootrec.exe" });
                Utilities.Add(new UtilityItem { Name = "BCDEdit", Description = "bcdedit.exe", FileName = "cmd.exe", Arguments = "/k bcdedit.exe" });
                Utilities.Add(new UtilityItem { Name = "DISM", Description = "dism.exe", FileName = "cmd.exe", Arguments = "/k dism.exe" });
                Utilities.Add(new UtilityItem { Name = "SFC", Description = "sfc.exe", FileName = "cmd.exe", Arguments = "/k sfc.exe" });
            }
            else
            {
                Utilities.Add(new UtilityItem { Name = "Службы", Description = "services.msc", FileName = "services.msc" });
                Utilities.Add(new UtilityItem { Name = "Диспетчер устройств", Description = "devmgmt.msc", FileName = "devmgmt.msc" });
                Utilities.Add(new UtilityItem { Name = "Управление дисками", Description = "diskmgmt.msc", FileName = "diskmgmt.msc" });
                Utilities.Add(new UtilityItem { Name = "Планировщик", Description = "taskschd.msc", FileName = "taskschd.msc" });
                Utilities.Add(new UtilityItem { Name = "События", Description = "eventvwr.msc", FileName = "eventvwr.msc" });
                Utilities.Add(new UtilityItem { Name = "Сведения", Description = "msinfo32", FileName = "msinfo32.exe" });
                Utilities.Add(new UtilityItem { Name = "Конфигурация", Description = "msconfig", FileName = "msconfig.exe" });
                Utilities.Add(new UtilityItem { Name = "Брандмауэр", Description = "wf.msc", FileName = "wf.msc" });
            }

            LaunchCommand = new RelayCommand(param =>
            {
                if (param is UtilityItem item)
                {
                    var win = System.Windows.Application.Current.MainWindow;
                    bool wasTop = win?.Topmost ?? false;
                    if (win != null) win.Topmost = false;
                    SystemLauncher.Launch(item.FileName, item.Arguments);
                    Task.Delay(500).ContinueWith(_ =>
                    {
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            if (win != null) win.Topmost = wasTop;
                        });
                    });
                    HistoryService.Log("Запуск утилиты", $"{item.Name}", "Utility");
                }
            });
            RefreshRestorePointsCommand = new RelayCommand(_ => LoadRestorePoints());
            CreateRestorePointCommand = new RelayCommand(_ => CreateRestorePoint());
            ExportSystemInfoCommand = new RelayCommand(_ => ExportSystemInfo());
            ExportHiveCommand = new RelayCommand(param => ExportHive(param?.ToString()));
            ImportHiveCommand = new RelayCommand(_ => ImportHive());
            RestoreAllCommand = new RelayCommand(_ => RestoreAll());
            SimulateHotkeyCommand = new RelayCommand(param => { if (param is string key) KeySimulator.SimulateHotkey(key); });

            if (!isWinRe)
            {
                Utilities.Add(new UtilityItem { Name = "Переменные среды", Description = "rundll32 sysdm.cpl", FileName = "rundll32.exe", Arguments = "sysdm.cpl,EditEnvironmentVariables" });
                Utilities.Add(new UtilityItem { Name = "Сетевые подключения", Description = "ncpa.cpl", FileName = "ncpa.cpl" });
                Utilities.Add(new UtilityItem { Name = "Параметры экрана", Description = "display settings", FileName = "ms-settings:display" });
                Utilities.Add(new UtilityItem { Name = "Параметры питания", Description = "powercfg.cpl", FileName = "powercfg.cpl" });
                Utilities.Add(new UtilityItem { Name = "Безопасность Windows", Description = "windows security", FileName = "windowsdefender:" });
                Utilities.Add(new UtilityItem { Name = "Компоненты Windows", Description = "optionalfeatures", FileName = "optionalfeatures.exe" });
            }
            try { LoadRestorePoints(); } catch { }
        }

        private void LoadRestorePoints()
        {
            RestorePoints.Clear();
            foreach (var p in SystemRestoreService.GetRestorePoints()) RestorePoints.Add(p);
        }

        private void CreateRestorePoint()
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Описание точки:", "Создание точки", "WREX Restore Point");
            if (!string.IsNullOrWhiteSpace(name) && SystemRestoreService.CreateRestorePoint(name)) LoadRestorePoints();
        }

        private void ExportSystemInfo()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Text files|*.txt", DefaultExt = ".txt", FileName = $"WREX_SystemInfo_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt" };
            if (dlg.ShowDialog() == true)
            {
                SystemInfoExporter.ExportToFile(dlg.FileName);
                System.Windows.MessageBox.Show($"Отчёт экспортирован:\n{dlg.FileName}", "Готово");
            }
        }

        private async void ExportHive(string? hiveName)
        {
            if (string.IsNullOrEmpty(hiveName)) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Файлы реестра (*.reg)|*.reg",
                DefaultExt = "reg",
                FileName = $"{hiveName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.reg"
            };

            if (dlg.ShowDialog() == true)
            {
                RegistryStatus = $"Экспорт {hiveName}...";
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"export {hiveName} \"{dlg.FileName}\" /y",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                });

                HistoryService.Log("Экспорт реестра", $"{hiveName} -> {dlg.FileName}", "Registry");
                RegistryStatus = $"Экспорт завершён: {System.IO.Path.GetFileName(dlg.FileName)}";
            }
        }

        private async void ImportHive()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Файлы реестра (*.reg)|*.reg|Все файлы (*.*)|*.*",
                DefaultExt = "reg",
                Title = "Импорт реестра из .reg файла"
            };

            if (dlg.ShowDialog() == true)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Импортировать файл?\n\n{dlg.FileName}\n\n⚠️ Это изменит настройки системы!",
                    "Подтверждение", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes) return;

                RegistryStatus = "Импорт...";
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"import \"{dlg.FileName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                });

                HistoryService.Log("Импорт реестра", dlg.FileName, "Registry");
                RegistryStatus = "Импорт завершён: " + System.IO.Path.GetFileName(dlg.FileName);
            }
        }

        private void RestoreAll()
        {
            var result = System.Windows.MessageBox.Show(
                "Восстановить ВСЕ ограничения?\n\nЭто восстановит:\n" +
                "• Диспетчер задач\n• Редактор реестра\n• CMD\n• Панель управления\n" +
                "• UAC\n• Скрытые файлы\n• Расширения файлов\n• Hosts\n\n" +
                "⚠️ Перезагрузка может потребоваться!",
                "Восстановление системы",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            var actions = new (string name, Func<string> action)[]
            {
                ("Диспетчер задач", RecoveryTools.EnableTaskManager),
                ("Редактор реестра", RecoveryTools.EnableRegistryEditor),
                ("Командная строка", RecoveryTools.EnableCMD),
                ("Панель управления", RecoveryTools.EnableControlPanel),
                ("UAC", RecoveryTools.EnableUAC),
                ("Скрытые файлы", RecoveryTools.ShowHiddenFiles),
                ("Расширения файлов", RecoveryTools.ShowFileExtensions),
                ("Hosts", RecoveryTools.ResetHosts),
                ("Брандмауэр", RecoveryTools.ResetFirewall),
                ("Восстановление системных файлов", RecoveryTools.RepairWindows),
            };

            var report = new System.Text.StringBuilder("Результаты восстановления:\n\n");
            int success = 0;
            foreach (var (name, action) in actions)
            {
                try
                {
                    var msg = action();
                    report.AppendLine($"✅ {name}: {msg}");
                    success++;
                }
                catch (Exception ex)
                {
                    report.AppendLine($"❌ {name}: {ex.Message}");
                }
            }

            UnlockStatus = $"✅ Восстановлено: {success}/{actions.Length}\n\n{report}";
            RegistryStatus = "Восстановление завершено";
            System.Windows.MessageBox.Show(report.ToString(), "Восстановление завершено",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
