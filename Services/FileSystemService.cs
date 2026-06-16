using System;
using System.IO;
using SystemManager.Services;

namespace SystemManager.Services
{
    public static class FileSystemService
    {
        public static void CreateFile(string path, string content = "")
        {
            FileMonitorService.MarkWrexOperation(path);
            File.WriteAllText(path, content);
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                FileMonitorService.MarkWrexOperation(path);
                File.Delete(path);
            }
        }

        public static void CopyFile(string source, string destination, bool overwrite = false)
        {
            FileMonitorService.MarkWrexOperation(source);
            FileMonitorService.MarkWrexOperation(destination);
            File.Copy(source, destination, overwrite);
        }

        public static void MoveFile(string source, string destination)
        {
            FileMonitorService.MarkWrexOperation(source);
            FileMonitorService.MarkWrexOperation(destination);
            File.Move(source, destination);
        }

        public static void CreateDirectory(string path)
        {
            FileMonitorService.MarkWrexOperation(path);
            Directory.CreateDirectory(path);
        }

        public static void DeleteDirectory(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
            {
                FileMonitorService.MarkWrexOperation(path);
                Directory.Delete(path, recursive);
            }
        }

        public static void WriteToFile(string path, string content, bool append = false)
        {
            FileMonitorService.MarkWrexOperation(path);
            if (append) File.AppendAllText(path, content);
            else File.WriteAllText(path, content);
        }

        public static string ReadFile(string path)
        {
            FileMonitorService.MarkWrexOperation(path);
            return File.ReadAllText(path);
        }
    }
}   