using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class ServiceManagerViewModel : ViewModelBase
    {
        private ObservableCollection<ServiceInfo> _services = new();
        private ObservableCollection<ServiceInfo> _filteredServices = new();
        private ServiceInfo? _selectedService;
        private string _searchText = "";
        private string _statusMessage = "";

        public ObservableCollection<ServiceInfo> Services
        {
            get => _filteredServices;
            set => SetProperty(ref _filteredServices, value);
        }

        public ServiceInfo? SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand RestartServiceCommand { get; }
        public ICommand SetAutoCommand { get; }
        public ICommand SetManualCommand { get; }
        public ICommand SetDisabledCommand { get; }

        public ServiceManagerViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadServicesAsync());
            StartServiceCommand = new RelayCommand(async _ => await StartServiceAsync(), _ => SelectedService?.Status != "Running");
            StopServiceCommand = new RelayCommand(async _ => await StopServiceAsync(), _ => SelectedService?.Status == "Running");
            RestartServiceCommand = new RelayCommand(async _ => await RestartServiceAsync(), _ => SelectedService?.Status == "Running");
            SetAutoCommand = new RelayCommand(async _ => await SetStartModeAsync("Auto"));
            SetManualCommand = new RelayCommand(async _ => await SetStartModeAsync("Manual"));
            SetDisabledCommand = new RelayCommand(async _ => await SetStartModeAsync("Disabled"));
            _ = LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            StatusMessage = "Загрузка служб...";
            _services = new ObservableCollection<ServiceInfo>(await Task.Run(() => WindowsServiceManager.GetAllServices()));
            ApplyFilter();
            StatusMessage = $"Загружено: {_services.Count} служб";
        }

        private void ApplyFilter()
        {
            Services = string.IsNullOrWhiteSpace(_searchText)
                ? new ObservableCollection<ServiceInfo>(_services)
                : new ObservableCollection<ServiceInfo>(_services.Where(s =>
                    s.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task StartServiceAsync()
        {
            if (SelectedService == null) return;
            StatusMessage = $"Запуск: {SelectedService.DisplayName}...";
            await Task.Run(() => WindowsServiceManager.StartService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task StopServiceAsync()
        {
            if (SelectedService == null) return;
            StatusMessage = $"Остановка: {SelectedService.DisplayName}...";
            await Task.Run(() => WindowsServiceManager.StopService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task RestartServiceAsync()
        {
            if (SelectedService == null) return;
            StatusMessage = $"Перезапуск: {SelectedService.DisplayName}...";
            await Task.Run(() => WindowsServiceManager.RestartService(SelectedService.Name));
            await LoadServicesAsync();
        }

        private async Task SetStartModeAsync(string mode)
        {
            if (SelectedService == null) return;
            StatusMessage = $"Изменение: {SelectedService.DisplayName} -> {mode}...";
            await Task.Run(() => WindowsServiceManager.SetStartMode(SelectedService.Name, mode));
            await LoadServicesAsync();
        }
    }
}
