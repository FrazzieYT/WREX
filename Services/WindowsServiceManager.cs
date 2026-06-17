using System;
using System.Collections.Generic;
using System.Management;
using System.ServiceProcess;
using System.Text;

#pragma warning disable CA1416

namespace SystemManager.Services
{
    public static class WindowsServiceManager
    {
        public static List<ServiceInfo> GetAllServices()
        {
            var services = new List<ServiceInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
                foreach (ManagementObject obj in searcher.Get())
                {
                    services.Add(new ServiceInfo
                    {
                        Name = obj["Name"]?.ToString() ?? "",
                        DisplayName = obj["DisplayName"]?.ToString() ?? "",
                        Status = obj["State"]?.ToString() ?? "Unknown",
                        StartMode = obj["StartMode"]?.ToString() ?? "Unknown",
                        PathName = obj["PathName"]?.ToString() ?? "",
                        Description = obj["Description"]?.ToString() ?? "",
                        ProcessId = obj["ProcessId"]?.ToString() ?? "0",
                        Account = obj["StartName"]?.ToString() ?? ""
                    });
                }
            }
            catch { }
            return services;
        }

        public static bool StartService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                return true;
            }
            catch { return false; }
        }

        public static bool StopService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                return true;
            }
            catch { return false; }
        }

        public static bool RestartService(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                return true;
            }
            catch { return false; }
        }

        public static bool SetStartMode(string serviceName, string mode)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Service WHERE Name='{serviceName}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var inParams = obj.GetMethodParameters("ChangeStartMode");
                    inParams["StartMode"] = mode;
                    obj.InvokeMethod("ChangeStartMode", inParams, null);
                }
                return true;
            }
            catch { return false; }
        }
    }

    public class ServiceInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Status { get; set; } = "";
        public string StartMode { get; set; } = "";
        public string PathName { get; set; } = "";
        public string Description { get; set; } = "";
        public string ProcessId { get; set; } = "0";
        public string Account { get; set; } = "";

        public string StatusIcon => Status switch
        {
            "Running" => "🟢",
            "Stopped" => "🔴",
            "Paused" => "🟡",
            _ => "⚪"
        };

        public string StartModeIcon => StartMode switch
        {
            "Auto" => "🔧",
            "Manual" => "✋",
            "Disabled" => "🚫",
            _ => "❓"
        };
    }
}
