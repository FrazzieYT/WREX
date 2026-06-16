using System.ComponentModel;
using System.Runtime.CompilerServices;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public enum OperationStatus
    {
        Ready,
        Processing,
        Completed,
        Error,
        Warning
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private object? _currentView;
        private int _selectedMenuIndex;
        private string _statusMessage = "Готов";
        private string _adminStatus = "  ";
        private OperationStatus _currentStatus = OperationStatus.Ready;
        
        private bool _isTopmost; 
        
        private readonly DashboardViewModel _dashboardView;
        private readonly ExplorerViewModel _explorerView;
        private readonly TaskManagerViewModel _taskManagerView;
        private readonly ConsoleViewModel _consoleView;
        private readonly UtilitiesViewModel _utilitiesView;
        private readonly HistoryViewModel _historyView;
        private readonly CleanupViewModel _cleanupView;
        
        public bool IsTopmost
        {
            get => _isTopmost;
            set
            {
                if (_isTopmost != value)
                {
                    _isTopmost = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set
            {
                if (_selectedMenuIndex != value)
                {
                    _selectedMenuIndex = value;
                    OnPropertyChanged();
                    SwitchView(value);
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string AdminStatus
        {
            get => _adminStatus;
            set { _adminStatus = value; OnPropertyChanged(); }
        }

        public OperationStatus CurrentStatus
        {
            get => _currentStatus;
            set { _currentStatus = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            _dashboardView = new DashboardViewModel();
            _explorerView = new ExplorerViewModel();
            _taskManagerView = new TaskManagerViewModel();
            _consoleView = new ConsoleViewModel();
            _utilitiesView = new UtilitiesViewModel();
            _historyView = new HistoryViewModel();
            _cleanupView = new CleanupViewModel();
            
            SelectedMenuIndex = 0;
            CheckAdminRights();
        }
  
        private void SwitchView(int index)
        {
            switch (index)
            {
                case 0:
                    CurrentView = _dashboardView;
                    StatusMessage = "Загружено: Главная";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 1:
                    CurrentView = _explorerView;
                    StatusMessage = "Загружено: Проводник";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 2:
                    CurrentView = _taskManagerView;
                    StatusMessage = "Загружено: Диспетчер памяти";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 3:
                    CurrentView = _consoleView;
                    StatusMessage = "Загружено: Консоль";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 4:
                    CurrentView = _utilitiesView;
                    StatusMessage = "Загружено: Утилиты";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 5:
                    CurrentView = _historyView;
                    StatusMessage = "Загружено: История";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 6:
                    CurrentView = _cleanupView;
                    StatusMessage = "Загружено: Очистка";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                case 7:
                    CurrentView = new RegistryViewModel();
                    StatusMessage = "Редактор реестра";
                    CurrentStatus = OperationStatus.Ready;
                    break;
                default:
                    CurrentView = _dashboardView;
                    CurrentStatus = OperationStatus.Ready;
                    break;
            }
        }

        private void CheckAdminRights()
        {
            var isAdmin = SecurityHelper.IsAdministrator();
            AdminStatus = isAdmin ? "🔒 Права администратора" : "⚠️ Ограниченные права";

            if (!isAdmin)
            {
                StatusMessage = "Некоторые функции ограничены. Запустите от имени администратора.";
                CurrentStatus = OperationStatus.Warning;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}