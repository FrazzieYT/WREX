using System.Collections.ObjectModel;
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
        public ICommand StartTrackingCommand { get; }
        public ICommand StopTrackingCommand { get; }
        public ICommand EnableUACCommand { get; }
        public ICommand DisableUACCommand { get; }
        public ICommand EnableTaskManagerCommand { get; }
        public ICommand EnableRegistryEditorCommand { get; }
        public ICommand EnableCMDCommand { get; }
        public ICommand EnableControlPanelCommand { get; }
        public ICommand ShowHiddenFilesCommand { get; }
        public ICommand ShowExtensionsCommand { get; }
        public ICommand ResetDefenderCommand { get; }
        public ICommand ResetFirewallCommand { get; }
        public ICommand RepairWindowsCommand { get; }
        public ICommand ResetHostsCommand { get; }
        public ICommand ExportSystemInfoCommand { get; }

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

            LaunchCommand = new RelayCommand(param => { if (param is UtilityItem item) { SystemLauncher.Launch(item.FileName, item.Arguments); HistoryService.Log("Запуск утилиты", $"{item.Name}", "Utility"); } });
            RefreshRestorePointsCommand = new RelayCommand(_ => LoadRestorePoints());
            CreateRestorePointCommand = new RelayCommand(_ => CreateRestorePoint());
            StartTrackingCommand = new RelayCommand(_ => { InstallationTrackingService.StartTracking(); System.Windows.MessageBox.Show("Трекинг запущен!\nУстановите программу, затем нажмите 'Остановить'.", "Трекинг"); });
            StopTrackingCommand = new RelayCommand(_ => InstallationTrackingService.StopTracking());

            EnableUACCommand = new RelayCommand(_ => ShowResult(RecoveryTools.EnableUAC()));
            DisableUACCommand = new RelayCommand(_ => ShowResult(RecoveryTools.DisableUAC()));
            EnableTaskManagerCommand = new RelayCommand(_ => ShowResult(RecoveryTools.EnableTaskManager()));
            EnableRegistryEditorCommand = new RelayCommand(_ => ShowResult(RecoveryTools.EnableRegistryEditor()));
            EnableCMDCommand = new RelayCommand(_ => ShowResult(RecoveryTools.EnableCMD()));
            EnableControlPanelCommand = new RelayCommand(_ => ShowResult(RecoveryTools.EnableControlPanel()));
            ShowHiddenFilesCommand = new RelayCommand(_ => ShowResult(RecoveryTools.ShowHiddenFiles()));
            ShowExtensionsCommand = new RelayCommand(_ => ShowResult(RecoveryTools.ShowFileExtensions()));
            ResetDefenderCommand = new RelayCommand(_ => ShowResult(RecoveryTools.ResetWindowsDefender()));
            ResetFirewallCommand = new RelayCommand(_ => ShowResult(RecoveryTools.ResetFirewall()));
            RepairWindowsCommand = new RelayCommand(_ => ShowResult(RecoveryTools.RepairWindows()));
            ResetHostsCommand = new RelayCommand(_ => ShowResult(RecoveryTools.ResetHosts()));
            ExportSystemInfoCommand = new RelayCommand(_ => ExportSystemInfo());

            LoadRestorePoints();
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
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files|*.txt|All files|*.*",
                DefaultExt = ".txt",
                FileName = $"WREX_SystemInfo_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                SystemInfoExporter.ExportToFile(dlg.FileName);
                System.Windows.MessageBox.Show($"Отчёт экспортирован:\n{dlg.FileName}", "Готово");
            }
        }

        private static void ShowResult(string message)
        {
            System.Windows.MessageBox.Show(message, "Результат", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
