using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SystemManager.Services
{
    public static class TrayIconService
    {
        private static NotifyIcon? _trayIcon;
        private static bool _isInitialized;

        public static event Action? OnShowWindow;
        public static event Action? OnExitApp;
        public static event Action? OnToggleStartup;

        public static bool IsVisible => _trayIcon != null;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "WREX - System Manager";
            _trayIcon.Icon = LoadIcon();
            _trayIcon.Visible = true;

            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Открыть WREX");
            showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
            showItem.Click += (_, _) => OnShowWindow?.Invoke();
            menu.Items.Add(showItem);

            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Автозагрузка");
            startupItem.Checked = StartupService.IsEnabled();
            startupItem.Click += (_, _) =>
            {
                OnToggleStartup?.Invoke();
                startupItem.Checked = StartupService.IsEnabled();
                UpdateTooltip();
            };
            menu.Items.Add(startupItem);

            var monitoringItem = new ToolStripMenuItem("Мониторинг");
            monitoringItem.Checked = FileMonitorService.IsMonitoring;
            monitoringItem.Click += (_, _) =>
            {
                if (FileMonitorService.IsMonitoring)
                {
                    FileMonitorService.Stop();
                    monitoringItem.Checked = false;
                }
                else
                {
                    FileMonitorService.Start();
                    monitoringItem.Checked = true;
                }
                UpdateTooltip();
            };
            menu.Items.Add(monitoringItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (_, _) => OnExitApp?.Invoke();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, _) => OnShowWindow?.Invoke();

            UpdateTooltip();
        }

        public static void UpdateTooltip()
        {
            if (_trayIcon == null) return;
            string monitoring = FileMonitorService.IsMonitoring ? "ВКЛ" : "ВЫКЛ";
            string startup = StartupService.IsEnabled() ? "ВКЛ" : "ВЫКЛ";
            _trayIcon.Text = $"WREX\nМониторинг: {monitoring}\nАвтозагрузка: {startup}";
        }

        public static void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public static void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private static Icon LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                    return new Icon(iconPath);
            }
            catch { }

            return SystemIcons.Application;
        }
    }
}
