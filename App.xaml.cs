using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SystemManager
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
        private Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "WREX_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("WREX уже запущен.", "WREX", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            DispatcherUnhandledException += (_, args) => { args.Handled = true; };
            AppDomain.CurrentDomain.UnhandledException += (_, args) => { };

            try { Services.FileMonitorService.Start(); } catch { }
            try { Services.RegistryMonitorService.Start(); } catch { }
            try
            {
                Services.TrayIconService.Initialize();
                Services.TrayIconService.OnShowWindow += ShowMainWindow;
                Services.TrayIconService.OnExitApp += ExitApplication;
                Services.TrayIconService.OnToggleStartup += () => Services.StartupService.Toggle();
            }
            catch { }

            bool startMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

            _mainWindow = new MainWindow();

            if (startMinimized)
            {
                _mainWindow.ShowInTaskbar = false;
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.Show();
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
            }

            _mainWindow.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            _mainWindow?.Hide();
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closing += MainWindow_Closing;
            }
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.ShowInTaskbar = true;
            _mainWindow.Activate();
        }

        private void ExitApplication()
        {
            try { Services.FileMonitorService.Stop(); } catch { }
            try { Services.RegistryMonitorService.Stop(); } catch { }
            try { Services.TrayIconService.Dispose(); } catch { }
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
            if (_mainWindow != null) { _mainWindow.Closing -= MainWindow_Closing; _mainWindow.Close(); }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { Services.FileMonitorService.Stop(); } catch { }
            try { Services.RegistryMonitorService.Stop(); } catch { }
            try { Services.TrayIconService.Dispose(); } catch { }
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
