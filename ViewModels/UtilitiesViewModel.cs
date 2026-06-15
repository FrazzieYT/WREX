using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class UtilityItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Arguments { get; set; } = "";
        public bool IsAvailable { get; set; } = true;
    }

    public class UtilitiesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<UtilityItem> Utilities { get; } = new();
        public ICommand LaunchCommand { get; }

        public UtilitiesViewModel()
        {
            bool isWinRE = RegistryService.IsWinRE();

            // Базовые утилиты, доступные в обеих средах
            Utilities.Add(new UtilityItem { Name = "Командная строка", Description = "cmd.exe", FileName = "cmd.exe" });
            Utilities.Add(new UtilityItem { Name = "PowerShell", Description = "powershell.exe", FileName = "powershell.exe" });
            Utilities.Add(new UtilityItem { Name = "Редактор реестра", Description = "regedit", FileName = "regedit.exe" });
            Utilities.Add(new UtilityItem { Name = "Блокнот", Description = "notepad.exe", FileName = "notepad.exe" });
            Utilities.Add(new UtilityItem { Name = "Диспетчер задач", Description = "taskmgr", FileName = "taskmgr.exe" });

            if (isWinRE)
            {
                // Специфичные утилиты для WinRE
                Utilities.Add(new UtilityItem { Name = "DiskPart", Description = "diskpart.exe", FileName = "diskpart.exe" });
                Utilities.Add(new UtilityItem { Name = "BootRec", Description = "bootrec.exe", FileName = "cmd.exe", Arguments = "/k bootrec.exe" });
                Utilities.Add(new UtilityItem { Name = "BCDEdit", Description = "bcdedit.exe", FileName = "cmd.exe", Arguments = "/k bcdedit.exe" });
                Utilities.Add(new UtilityItem { Name = "DISM", Description = "dism.exe", FileName = "cmd.exe", Arguments = "/k dism.exe" });
                Utilities.Add(new UtilityItem { Name = "SFC", Description = "sfc.exe", FileName = "cmd.exe", Arguments = "/k sfc.exe" });
            }
            else
            {
                // Утилиты только для обычной ОС (требуют запущенных служб)
                Utilities.Add(new UtilityItem { Name = "Службы", Description = "services.msc", FileName = "services.msc" });
                Utilities.Add(new UtilityItem { Name = "Диспетчер устройств", Description = "devmgmt.msc", FileName = "devmgmt.msc" });
                Utilities.Add(new UtilityItem { Name = "Управление дисками", Description = "diskmgmt.msc", FileName = "diskmgmt.msc" });
                Utilities.Add(new UtilityItem { Name = "Планировщик заданий", Description = "taskschd.msc", FileName = "taskschd.msc" });
                Utilities.Add(new UtilityItem { Name = "Просмотр событий", Description = "eventvwr.msc", FileName = "eventvwr.msc" });
                Utilities.Add(new UtilityItem { Name = "Сведения о системе", Description = "msinfo32", FileName = "msinfo32.exe" });
                Utilities.Add(new UtilityItem { Name = "Конфигурация системы", Description = "msconfig", FileName = "msconfig.exe" });
                Utilities.Add(new UtilityItem { Name = "Брандмауэр", Description = "wf.msc", FileName = "wf.msc" });
            }

            LaunchCommand = new RelayCommand(param =>
            {
                if (param is UtilityItem item && item.IsAvailable)
                {
                    SystemLauncher.Launch(item.FileName, item.Arguments);
                    HistoryService.Log("Запуск утилиты", $"{item.Name} ({item.FileName})", "Utility");
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}