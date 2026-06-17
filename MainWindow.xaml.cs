using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
        }

        private void GitHubLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/FrazzieYT/WREX", UseShellExecute = true });
            }
            catch { }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        private void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;
            SettingsPopup.IsOpen = false;
            AlwaysOnTopButton.Content = this.Topmost ? "✅ Поверх всех окон (ВКЛ)" : "🔝 Поверх всех окон (ВЫКЛ)";
            var converter = new System.Windows.Media.BrushConverter();
            AlwaysOnTopButton.Background = this.Topmost
                ? (System.Windows.Media.Brush)converter.ConvertFromString("#0078D4")!
                : (System.Windows.Media.Brush)converter.ConvertFromString("#3E3E42")!;
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;

            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.StatusMessage = "Проверка обновлений...";
                vm.CurrentStatus = ViewModels.OperationStatus.Processing;

                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("WREX-App");
                    var response = await client.GetStringAsync("https://api.github.com/repos/FrazzieYT/WREX/releases/latest");
                    var json = JsonDocument.Parse(response);

                    if (json.RootElement.TryGetProperty("tag_name", out var tagName))
                    {
                        string latest = tagName.GetString() ?? "";
                        if (string.Compare(latest, "1.0.0", StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            if (MessageBox.Show($"Доступна версия: {latest}\n\nОткрыть страницу релиза?",
                                "Обновление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                if (json.RootElement.TryGetProperty("html_url", out var url))
                                    Process.Start(new ProcessStartInfo { FileName = url.GetString() ?? "", UseShellExecute = true });
                            }
                            vm.StatusMessage = $"Новая версия: {latest}";
                        }
                        else
                        {
                            vm.StatusMessage = "Актуальная версия";
                        }
                        vm.CurrentStatus = ViewModels.OperationStatus.Completed;
                    }
                }
                catch
                {
                    vm.StatusMessage = "Ошибка проверки обновлений";
                    vm.CurrentStatus = ViewModels.OperationStatus.Error;
                }
            }
        }

        private void StartupButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;
            StartupService.Toggle();
            bool enabled = StartupService.IsEnabled();

            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.StatusMessage = enabled ? "Автозагрузка: ВКЛ" : "Автозагрузка: ВЫКЛ";
                vm.CurrentStatus = ViewModels.OperationStatus.Completed;
            }
            TrayIconService.UpdateTooltip();
        }
    }
}
