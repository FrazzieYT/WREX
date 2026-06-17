using System;
using Microsoft.Win32;

namespace SystemManager.Services
{
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WREX";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void Enable()
        {
            try
            {
                var exePath = Environment.ProcessPath ?? "";
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
            catch { }
        }

        public static void Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.DeleteValue(AppName, false);
            }
            catch { }
        }

        public static void Toggle()
        {
            if (IsEnabled())
                Disable();
            else
                Enable();
        }
    }
}
