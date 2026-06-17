using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager
{
    public partial class MainWindow : Window
    {
        private bool _isAlwaysOnTop = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
        }

        private void GitHubLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/FrazzieYT/WREX",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть ссылку в браузере.\n\nОшибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        private void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            this.Topmost = _isAlwaysOnTop;

            SettingsPopup.IsOpen = false;

            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.IsTopmost = !vm.IsTopmost;

                this.Topmost = vm.IsTopmost;
                vm.StatusMessage = vm.IsTopmost ?
                    "Режим «Поверх всех окон» включен" :
                    "Режим «Поверх всех окон» выключен";

                vm.CurrentStatus = ViewModels.OperationStatus.Ready;
            }
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;

            var viewModel = DataContext as ViewModels.MainViewModel;
            if (viewModel != null)
            {
                viewModel.StatusMessage = "Проверка обновлений...";
                viewModel.CurrentStatus = ViewModels.OperationStatus.Processing;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WREX-App");

                var response = await client.GetStringAsync("https://api.github.com/repos/FrazzieYT/WREX/releases/latest");
                var json = JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("tag_name", out var tagName))
                {
                    string latestVersion = tagName.GetString() ?? "неизвестно";
                    string currentVersion = "1.0.0";

                    if (string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        var result = MessageBox.Show(
                            $"Доступна новая версия: {latestVersion}\n\nОткрыть страницу релиза?",
                            "Обновление доступно",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            if (json.RootElement.TryGetProperty("html_url", out var htmlUrl))
                            {
                                string? releaseUrl = htmlUrl.GetString();
                                if (!string.IsNullOrEmpty(releaseUrl))
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = releaseUrl,
                                        UseShellExecute = true
                                    });
                                }
                            }
                        }

                        if (viewModel != null)
                        {
                            viewModel.StatusMessage = $"Найдена новая версия: {latestVersion}";
                            viewModel.CurrentStatus = ViewModels.OperationStatus.Completed;
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            $"У вас установлена актуальная версия ({currentVersion})",
                            "Обновления не требуются",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        if (viewModel != null)
                        {
                            viewModel.StatusMessage = "Установлена актуальная версия";
                            viewModel.CurrentStatus = ViewModels.OperationStatus.Ready;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось проверить обновления.\n\nОшибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (viewModel != null)
                {
                    viewModel.StatusMessage = "Ошибка проверки обновлений";
                    viewModel.CurrentStatus = ViewModels.OperationStatus.Error;
                }
            }
        }

        private void StartupButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;

            StartupService.Toggle();
            bool isEnabled = StartupService.IsEnabled();

            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.StatusMessage = isEnabled
                    ? "Добавлено в автозагрузку"
                    : "Удалено из автозагрузки";
                vm.CurrentStatus = ViewModels.OperationStatus.Completed;
            }

            TrayIconService.UpdateTooltip();
        }
    }
}
