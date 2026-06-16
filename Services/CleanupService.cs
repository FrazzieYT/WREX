using System;
using System.IO;
using SystemManager.Services;

namespace SystemManager.Services
{
    public static class CleanupService
    {
        private static string GetTargetWindowsPath()
        {
            if (RegistryService.IsWinRE())
            {
                var offline = RegistryService.DetectOfflineWindowsPath();
                if (!string.IsNullOrEmpty(offline))
                    return offline;
            }
            return Environment.SystemDirectory + @"\..";
        }

        public static CleanupResult CleanTemp()
        {
            var result = new CleanupResult { OperationName = "Очистка Temp" };
            var tempPath = Path.GetTempPath();

            try
            {
                HistoryService.Log("Начата очистка Temp", tempPath, "Cleanup");
                CleanDirectory(tempPath, result);
                HistoryService.Log("Очистка Temp завершена",
                    $"Удалено: {result.DeletedFiles} файлов, {result.DeletedDirectories} папок. " +
                    $"Освобождено: {FormatSize(result.FreedBytes)}", "Cleanup");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка очистки Temp", ex.Message, "Cleanup");
                result.Error = ex.Message;
            }
            return result;
        }

        public static CleanupResult CleanPrefetch()
        {
            var result = new CleanupResult { OperationName = "Очистка Prefetch" };
            var prefetchPath = Path.Combine(GetTargetWindowsPath(), "Prefetch");

            try
            {
                prefetchPath = Path.GetFullPath(prefetchPath);
                HistoryService.Log("Начата очистка Prefetch", prefetchPath, "Cleanup");

                if (Directory.Exists(prefetchPath))
                {
                    foreach (var file in Directory.GetFiles(prefetchPath, "*.pf"))
                    {
                        try
                        {
                            result.FreedBytes += new FileInfo(file).Length;
                            File.Delete(file);
                            result.DeletedFiles++;
                        }
                        catch { result.SkippedItems++; }
                    }
                }

                HistoryService.Log("Очистка Prefetch завершена",
                    $"Удалено: {result.DeletedFiles} файлов. Освобождено: {FormatSize(result.FreedBytes)}",
                    "Cleanup");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка очистки Prefetch", ex.Message, "Cleanup");
                result.Error = ex.Message;
            }
            return result;
        }

        public static CleanupResult CleanRecent()
        {
            var result = new CleanupResult { OperationName = "Очистка Recent" };

            try
            {
                string recentPath;
                if (RegistryService.IsWinRE())
                {
                    var targetWin = GetTargetWindowsPath();
                    recentPath = Path.Combine(targetWin, "..", "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Recent");
                    if (!Directory.Exists(recentPath))
                    {
                        result.OperationName = "Очистка Recent (пропущено)";
                        return result;
                    }
                }
                else
                {
                    recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
                }

                HistoryService.Log("Начата очистка Recent", recentPath, "Cleanup");

                if (Directory.Exists(recentPath))
                {
                    foreach (var file in Directory.GetFiles(recentPath))
                    {
                        try
                        {
                            File.Delete(file);
                            result.DeletedFiles++;
                        }
                        catch { result.SkippedItems++; }
                    }
                }

                HistoryService.Log("Очистка Recent завершена",
                    $"Удалено: {result.DeletedFiles} ярлыков", "Cleanup");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка очистки Recent", ex.Message, "Cleanup");
                result.Error = ex.Message;
            }
            return result;
        }

        public static CleanupResult CleanWindowsTemp()
        {
            var result = new CleanupResult { OperationName = "Очистка Windows\\Temp" };
            var winTempPath = Path.Combine(GetTargetWindowsPath(), "Temp");

            try
            {
                winTempPath = Path.GetFullPath(winTempPath);
                HistoryService.Log("Начата очистка Windows\\Temp", winTempPath, "Cleanup");

                if (Directory.Exists(winTempPath))
                    CleanDirectory(winTempPath, result);

                HistoryService.Log("Очистка Windows\\Temp завершена",
                    $"Удалено: {result.DeletedFiles} файлов, {result.DeletedDirectories} папок. " +
                    $"Освобождено: {FormatSize(result.FreedBytes)}", "Cleanup");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка очистки Windows\\Temp", ex.Message, "Cleanup");
                result.Error = ex.Message;
            }
            return result;
        }

        public static CleanupResult RunFullCleanup()
        {
            HistoryService.Log("Начата полная очистка системы", "", "Cleanup");

            var totalResult = new CleanupResult { OperationName = "Полная очистка" };
            var results = new[]
            {
                CleanTemp(),
                CleanPrefetch(),
                CleanRecent(),
                CleanWindowsTemp()
            };

            foreach (var r in results)
            {
                totalResult.DeletedFiles += r.DeletedFiles;
                totalResult.DeletedDirectories += r.DeletedDirectories;
                totalResult.FreedBytes += r.FreedBytes;
                totalResult.SkippedItems += r.SkippedItems;
            }

            HistoryService.Log("Полная очистка завершена",
                $"Всего удалено: {totalResult.DeletedFiles} файлов, " +
                $"{totalResult.DeletedDirectories} папок. " +
                $"Освобождено: {FormatSize(totalResult.FreedBytes)}", "Cleanup");

            return totalResult;
        }

        private static void CleanDirectory(string path, CleanupResult result)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    Directory.Delete(dir, true);
                    result.DeletedDirectories++;
                }
                catch { result.SkippedItems++; }
            }

            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    result.FreedBytes += new FileInfo(file).Length;
                    File.Delete(file);
                    result.DeletedFiles++;
                }
                catch { result.SkippedItems++; }
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    public class CleanupResult
    {
        public string OperationName { get; set; } = "";
        public int DeletedFiles { get; set; }
        public int DeletedDirectories { get; set; }
        public long FreedBytes { get; set; }
        public int SkippedItems { get; set; }
        public string? Error { get; set; }
    }
}