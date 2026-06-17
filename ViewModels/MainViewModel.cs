using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public enum OperationStatus { Ready, Processing, Completed, Error, Warning }

    public class MainViewModel : ViewModelBase
    {
        private object? _currentView;
        private int _selectedMenuIndex = -1;
        private string _statusMessage = "Готов";
        private string _adminStatus = "  ";
        private OperationStatus _currentStatus = OperationStatus.Ready;
        private bool _isTopmost = true;

        private object? _dashboardView;
        private object? _explorerView;
        private object? _taskManagerView;
        private object? _consoleView;
        private object? _utilitiesView;
        private object? _historyView;
        private object? _cleanupView;
        private object? _registryView;

        public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string AdminStatus { get => _adminStatus; set => SetProperty(ref _adminStatus, value); }
        public OperationStatus CurrentStatus { get => _currentStatus; set => SetProperty(ref _currentStatus, value); }
        public bool IsTopmost { get => _isTopmost; set => SetProperty(ref _isTopmost, value); }

        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set { if (SetProperty(ref _selectedMenuIndex, value)) SwitchView(value); }
        }

        public MainViewModel()
        {
            var isAdmin = SecurityHelper.IsAdministrator();
            AdminStatus = isAdmin ? "🔒 Администратор" : "⚠️ Ограниченные права";
            if (!isAdmin) { StatusMessage = "Запустите от имени администратора."; CurrentStatus = OperationStatus.Warning; }
            SelectedMenuIndex = 0;
        }

        private void SwitchView(int index)
        {
            switch (index)
            {
                case 0: _dashboardView ??= new DashboardViewModel(); CurrentView = _dashboardView; StatusMessage = "Главная"; break;
                case 1: _explorerView ??= new ExplorerViewModel(); CurrentView = _explorerView; StatusMessage = "Проводник"; break;
                case 2: _taskManagerView ??= new TaskManagerViewModel(); CurrentView = _taskManagerView; StatusMessage = "Диспетчер задач"; break;
                case 3: _consoleView ??= new ConsoleViewModel(); CurrentView = _consoleView; StatusMessage = "Консоль"; break;
                case 4: _utilitiesView ??= new UtilitiesViewModel(); CurrentView = _utilitiesView; StatusMessage = "Утилиты"; break;
                case 5: _historyView ??= new HistoryViewModel(); CurrentView = _historyView; StatusMessage = "История"; break;
                case 6: _cleanupView ??= new CleanupViewModel(); CurrentView = _cleanupView; StatusMessage = "Очистка"; break;
                case 7: _registryView ??= new RegistryViewModel(); CurrentView = _registryView; StatusMessage = "Реестр"; break;
                default: _dashboardView ??= new DashboardViewModel(); CurrentView = _dashboardView; StatusMessage = "Главная"; break;
            }
            CurrentStatus = OperationStatus.Ready;
        }
    }
}
