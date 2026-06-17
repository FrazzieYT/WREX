using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace SystemManager.Services
{
    public static class RecoveryTools
    {
        public static bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string EnableUAC()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key == null) return "Не удалось открыть ключ реестра";
                key.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
                key.SetValue("ConsentPromptBehaviorAdmin", 2, RegistryValueKind.DWord);
                return "UAC включён. Требуется перезагрузка.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string DisableUAC()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key == null) return "Не удалось открыть ключ реестра";
                key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                return "UAC отключён. Требуется перезагрузка.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string EnableTaskManager()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                key?.DeleteValue("DisableTaskMgr", false);
                return "Диспетчер задач включён.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string DisableTaskManager()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                key?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                return "Диспетчер задач отключён.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string EnableRegistryEditor()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                key?.DeleteValue("DisableRegistryTools", false);
                return "Редактор реестра включён.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string DisableRegistryEditor()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                key?.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);
                return "Редактор реестра отключён.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string EnableCMD()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true);
                key?.DeleteValue("DisableCMD", false);
                return "Командная строка включена.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string DisableCMD()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true);
                key?.SetValue("DisableCMD", 1, RegistryValueKind.DWord);
                return "Командная строка отключена.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string EnableControlPanel()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                key?.DeleteValue("NoControlPanel", false);
                return "Панель управления включена.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string DisableControlPanel()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                key?.SetValue("NoControlPanel", 1, RegistryValueKind.DWord);
                return "Панель управления отключена.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string ShowHiddenFiles()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
                key?.SetValue("Hidden", 1, RegistryValueKind.DWord);
                return "Скрытые файлы показываются.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string ShowFileExtensions()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
                key?.SetValue("HideFileExt", 0, RegistryValueKind.DWord);
                return "Расширения файлов показываются.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string ResetWindowsDefender()
        {
            try
            {
                RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $false; Set-MpPreference -DisableBehaviorMonitoring $false; Set-MpPreference -DisableIOAVProtection $false; Set-MpPreference -DisableScriptScanning $false");
                return "Windows Defender сброшен и включён.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string ResetFirewall()
        {
            try
            {
                RunPowerShell("netsh advfirewall reset all");
                return "Брандмауэр сброшен к настройкам по умолчанию.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string RepairWindows()
        {
            try
            {
                RunPowerShell("sfc /scannow; DISM /Online /Cleanup-Image /RestoreHealth");
                return "Запущен процесс восстановления Windows...";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string ResetHosts()
        {
            try
            {
                var hostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
                var backupPath = hostsPath + ".bak";
                if (!File.Exists(backupPath)) File.Copy(hostsPath, backupPath);
                File.WriteAllText(hostsPath, "# Copyright (c) 1993-2009 Microsoft Corp.\n#\n# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.\n#\n# localhost name resolution is handled within DNS itself.\n# 127.0.0.1       localhost\n# ::1             localhost\n");
                return "Файл hosts сброшен к стандартному.";
            }
            catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
        }

        public static string EnableShowAllFolders()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{FullFolderReason}", true);
                return "Полная папка рабочего стола включена.";
            }
            catch { return "Ключ не найден или недоступен."; }
        }

        private static void RunPowerShell(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = Process.Start(psi);
            process?.WaitForExit(30000);
        }
    }
}
