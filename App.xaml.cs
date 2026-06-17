using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SystemManager
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.ShowInTaskbar = false;
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
            if (_mainWindow != null) { _mainWindow.Closing -= MainWindow_Closing; _mainWindow.Close(); }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { Services.FileMonitorService.Stop(); } catch { }
            try { Services.RegistryMonitorService.Stop(); } catch { }
            try { Services.TrayIconService.Dispose(); } catch { }
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }
    }
}
