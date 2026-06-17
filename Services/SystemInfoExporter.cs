using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace SystemManager.Services
{
    public static class SystemInfoExporter
    {
        public static string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("  WREX - System Information Report");
            sb.AppendLine($"  {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            sb.AppendLine("▸ ОС");
            sb.AppendLine($"  Имя: {Environment.MachineName}");
            sb.AppendLine($"  Пользователь: {Environment.UserName}");
            sb.AppendLine($"  ОС: {Environment.OSVersion}");
            sb.AppendLine($"  64-bit: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"  .NET: {Environment.Version}");
            sb.AppendLine($"  Системная папка: {Environment.SystemDirectory}");
            sb.AppendLine($"  Рабочая папка: {Environment.CurrentDirectory}");
            sb.AppendLine();

            sb.AppendLine("▸ Процессор");
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    sb.AppendLine($"  Название: {obj["Name"]}");
                    sb.AppendLine($"  Ядра: {obj["NumberOfCores"]}");
                    sb.AppendLine($"  Потоки: {obj["NumberOfLogicalProcessors"]}");
                    sb.AppendLine($"  Частота: {obj["MaxClockSpeed"]} MHz");
                }
            }
            catch { sb.AppendLine("  Н/Д"); }
            sb.AppendLine();

            sb.AppendLine("▸ ОЗУ");
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var total = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) / 1024;
                    var free = Convert.ToUInt64(obj["FreePhysicalMemory"]) / 1024;
                    sb.AppendLine($"  Всего: {total} МБ");
                    sb.AppendLine($"  Свободно: {free} МБ");
                    sb.AppendLine($"  Использовано: {total - free} МБ ({(total - free) * 100 / total}%)");
                }
            }
            catch { sb.AppendLine("  Н/Д"); }
            sb.AppendLine();

            sb.AppendLine("▸ Диски");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                try
                {
                    var free = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                    var total = drive.TotalSize / (1024 * 1024 * 1024);
                    sb.AppendLine($"  {drive.Name} {drive.VolumeLabel} — {free} ГБ свободно из {total} ГБ");
                }
                catch { }
            }
            sb.AppendLine();

            sb.AppendLine("▸ Сеть");
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    var ip = nic.GetIPProperties();
                    if (ip.UnicastAddresses.Count == 0) continue;
                    sb.AppendLine($"  {nic.Name}:");
                    foreach (var addr in ip.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            sb.AppendLine($"    IPv4: {addr.Address}");
                    }
                }
            }
            catch { sb.AppendLine("  Н/Д"); }
            sb.AppendLine();

            sb.AppendLine("▸ Процессы (топ-10 по памяти)");
            try
            {
                var processes = Process.GetProcesses();
                Array.Sort(processes, (a, b) =>
                {
                    try { return b.WorkingSet64.CompareTo(a.WorkingSet64); }
                    catch { return 0; }
                });
                int count = 0;
                foreach (var p in processes)
                {
                    if (count++ >= 10) break;
                    try
                    {
                        var mb = p.WorkingSet64 / 1024 / 1024;
                        sb.AppendLine($"  {p.ProcessName,-25} PID:{p.Id,-8} {mb} МБ");
                    }
                    catch { }
                }
            }
            catch { sb.AppendLine("  Н/Д"); }
            sb.AppendLine();

            sb.AppendLine("▸ Запущенные службы");
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE State='Running'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    sb.AppendLine($"  {obj["DisplayName"]}");
                }
            }
            catch { sb.AppendLine("  Н/Д"); }
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("  Отчёт создан WREX System Manager");
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        public static void ExportToFile(string path)
        {
            File.WriteAllText(path, GenerateReport(), Encoding.UTF8);
        }
    }
}
