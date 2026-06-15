using System;
using System.IO;
using SystemManager.Services;

namespace SystemManager.Services
{
    public static class FileSystemService
    {
        public static void CreateFile(string path, string content = "")
        {
            try
            {
                File.WriteAllText(path, content);
                HistoryService.Log("Создан файл", path, "File");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка создания файла", $"{path}: {ex.Message}", "File");
                throw;
            }
        }

        public static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    HistoryService.Log("Удалён файл", path, "File");
                }
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка удаления файла", $"{path}: {ex.Message}", "File");
                throw;
            }
        }

        public static void CopyFile(string source, string destination, bool overwrite = false)
        {
            try
            {
                File.Copy(source, destination, overwrite);
                HistoryService.Log("Копирование файла", $"{source} → {destination}", "File");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка копирования файла", $"{source} → {destination}: {ex.Message}", "File");
                throw;
            }
        }

        public static void MoveFile(string source, string destination)
        {
            try
            {
                File.Move(source, destination);
                HistoryService.Log("Перемещение файла", $"{source} → {destination}", "File");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка перемещения файла", $"{source} → {destination}: {ex.Message}", "File");
                throw;
            }
        }

        public static void CreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                HistoryService.Log("Создана папка", path, "File");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка создания папки", $"{path}: {ex.Message}", "File");
                throw;
            }
        }

        public static void DeleteDirectory(string path, bool recursive = true)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                    HistoryService.Log("Удалена папка", path, "File");
                }
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка удаления папки", $"{path}: {ex.Message}", "File");
                throw;
            }
        }

        public static void WriteToFile(string path, string content, bool append = false)
        {
            try
            {
                if (append)
                    File.AppendAllText(path, content);
                else
                    File.WriteAllText(path, content);

                HistoryService.Log("Запись в файл", $"{path} ({(append ? "дописано" : "перезаписано")})", "File");
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка записи в файл", $"{path}: {ex.Message}", "File");
                throw;
            }
        }

        public static string ReadFile(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                HistoryService.Log("Чтение файла", path, "File");
                return content;
            }
            catch (Exception ex)
            {
                HistoryService.Log("Ошибка чтения файла", $"{path}: {ex.Message}", "File");
                throw;
            }
        }
    }
}   