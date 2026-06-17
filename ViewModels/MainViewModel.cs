using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public enum OperationStatus { Ready, Processing, Completed, Error, Warning }

    public class MainViewModel : ViewModelBase
    {
        private object? _currentView;
        private int _selectedMenuIndex;
        private string _statusMessage = "Готов";
        private string _adminStatus = "  ";
        private OperationStatus _currentStatus = OperationStatus.Ready;
        private bool _isTopmost = true;

        private readonly DashboardViewModel _dashboardView = new();
        private readonly ExplorerViewModel _explorerView = new();
        private readonly TaskManagerViewModel _taskManagerView = new();
        private readonly ConsoleViewModel _consoleView = new();
        private readonly UtilitiesViewModel _utilitiesView = new();
        private readonly HistoryViewModel _historyView = new();
        private readonly CleanupViewModel _cleanupView = new();
        private readonly ServiceManagerViewModel _serviceManagerView = new();

        public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public int SelectedMenuIndex { get => _selectedMenuIndex; set { if (SetProperty(ref _selectedMenuIndex, value)) SwitchView(value); } }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string AdminStatus { get => _adminStatus; set => SetProperty(ref _adminStatus, value); }
        public OperationStatus CurrentStatus { get => _currentStatus; set => SetProperty(ref _currentStatus, value); }
        public bool IsTopmost { get => _isTopmost; set => SetProperty(ref _isTopmost, value); }

        public MainViewModel()
        {
            SelectedMenuIndex = 0;
            var isAdmin = SecurityHelper.IsAdministrator();
            AdminStatus = isAdmin ? "🔒 Администратор" : "⚠️ Ограниченные права";
            if (!isAdmin) { StatusMessage = "Запустите от имени администратора для полного доступа."; CurrentStatus = OperationStatus.Warning; }
        }

        private void SwitchView(int index)
        {
            switch (index)
            {
                case 0: CurrentView = _dashboardView; StatusMessage = "Главная"; CurrentStatus = OperationStatus.Ready; break;
                case 1: CurrentView = _explorerView; StatusMessage = "Проводник"; CurrentStatus = OperationStatus.Ready; break;
                case 2: CurrentView = _taskManagerView; StatusMessage = "Диспетчер задач"; CurrentStatus = OperationStatus.Ready; break;
                case 3: CurrentView = _consoleView; StatusMessage = "Консоль"; CurrentStatus = OperationStatus.Ready; break;
                case 4: CurrentView = _utilitiesView; StatusMessage = "Утилиты"; CurrentStatus = OperationStatus.Ready; break;
                case 5: CurrentView = _historyView; StatusMessage = "История"; CurrentStatus = OperationStatus.Ready; break;
                case 6: CurrentView = _cleanupView; StatusMessage = "Очистка"; CurrentStatus = OperationStatus.Ready; break;
                case 7: CurrentView = _serviceManagerView; StatusMessage = "Службы"; CurrentStatus = OperationStatus.Ready; break;
                case 8: CurrentView = new RegistryViewModel(); StatusMessage = "Реестр"; CurrentStatus = OperationStatus.Ready; break;
                default: CurrentView = _dashboardView; StatusMessage = "Главная"; CurrentStatus = OperationStatus.Ready; break;
            }
        }
    }
}
