using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;

namespace SystemManager.Services
{
    public static class SystemRestoreService
    {
        public static List<RestorePointInfo> GetRestorePoints()
        {
            var points = new List<RestorePointInfo>();
            try
            {
                if (RegistryService.IsWinRE()) return points;

                using var searcher = new ManagementObjectSearcher(
                    "root\\default",
                    "SELECT * FROM SystemRestore");

                foreach (ManagementObject obj in searcher.Get())
                {
                    points.Add(new RestorePointInfo
                    {
                        SequenceNumber = Convert.ToUInt32(obj["SequenceNumber"]),
                        Description = obj["Description"]?.ToString() ?? "",
                        CreationTime = ManagementDateTimeConverter.ToDateTime(
                            obj["CreationTime"]?.ToString() ?? ""),
                        RestorePointType = obj["RestorePointType"]?.ToString() ?? "",
                        EventType = obj["EventType"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения точек восстановления: {ex.Message}", "Ошибка");
            }
            return points;
        }

        public static bool CreateRestorePoint(string description, int type = 12)
        {
            if (RegistryService.IsWinRE())
            {
                MessageBox.Show("Точки восстановления недоступны в WinRE.", "Информация");
                return false;
            }
            try
            {
                var scope = new ManagementScope(@"\\.\root\default");
                scope.Connect();

                using var classObj = new ManagementClass(scope,
                    new ManagementPath("SystemRestore"), null);

                using var inParams = classObj.GetMethodParameters("CreateRestorePoint");
                inParams["Description"] = description;
                inParams["RestorePointType"] = type;
                inParams["EventType"] = 100;

                classObj.InvokeMethod("CreateRestorePoint", inParams, null);

                HistoryService.Log("Создана точка восстановления", description, "System");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания точки восстановления: {ex.Message}", "Ошибка");
                return false;
            }
        }

        public static bool RestoreTo(uint sequenceNumber)
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Checkpoint-Computer -Description 'Pre-restore checkpoint'\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                process.Start();

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "rstrui.exe",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                HistoryService.Log("Запущен откат к точке восстановления",
                    $"Sequence: {sequenceNumber}", "System");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска восстановления: {ex.Message}", "Ошибка");
                return false;
            }
        }
    }

    public class RestorePointInfo
    {
        public uint SequenceNumber { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreationTime { get; set; }
        public string RestorePointType { get; set; } = "";
        public string EventType { get; set; } = "";

        public string FormattedTime => CreationTime.ToString("dd.MM.yyyy HH:mm:ss");
        public string TypeDisplay => RestorePointType switch
        {
            "0" => "APPLICATION_INSTALL",
            "1" => "APPLICATION_UNINSTALL",
            "10" => "DEVICE_DRIVER_INSTALL",
            "12" => "MODIFY_SETTINGS",
            _ => RestorePointType
        };
    }
}
