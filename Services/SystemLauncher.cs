using System.Diagnostics;
using System.Windows;
using SystemManager.Services;

namespace SystemManager.Services
{
    public static class SystemLauncher
    {
        public static void Launch(string fileName, string arguments = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                });

                HistoryService.Log("Запуск программы", 
                    string.IsNullOrEmpty(arguments) ? fileName : $"{fileName} {arguments}", 
                    "Process");
            }
            catch (System.Exception ex)
            {
                HistoryService.Log("Ошибка запуска", $"{fileName}: {ex.Message}", "Process");
                MessageBox.Show($"Не удалось запустить {fileName}:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}