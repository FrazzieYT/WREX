using System.IO;

namespace SystemManager.Services
{
    public static class FormatUtils
    {
        public static string FormatSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        public static string FormatSizeRu(long bytes)
        {
            if (bytes == 0) return "0 Б";
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        public static string GetFileIcon(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".txt" or ".log" or ".md" => "📝",
                ".cs" or ".vb" or ".fs" => "🔧",
                ".js" or ".ts" or ".py" => "📜",
                ".html" or ".css" => "🌐",
                ".xaml" or ".xml" or ".json" or ".ini" or ".config" or ".yaml" or ".yml" => "📋",
                ".exe" or ".msi" => "⚙️",
                ".dll" => "📚",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".ico" or ".webp" or ".svg" => "🖼️",
                ".mp3" or ".wav" or ".flac" or ".ogg" or ".aac" => "🎵",
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".webm" => "🎬",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
                ".doc" or ".docx" => "📄",
                ".xls" or ".xlsx" => "📊",
                ".ppt" or ".pptx" => "📊",
                ".pdf" or ".epub" => "📕",
                ".bat" or ".cmd" or ".ps1" or ".sh" => "💻",
                ".reg" => "🔧",
                _ => "📄"
            };
        }

        public static bool IsArchiveExtension(string ext)
        {
            return ext is ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz";
        }

        public static bool ShouldIgnorePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            var lower = path.ToLowerInvariant();
            return lower.Contains("\\windows\\temp\\") ||
                   lower.Contains("\\windows\\prefetch\\") ||
                   lower.Contains("\\$recycle.bin\\") ||
                   lower.Contains("\\system volume information\\") ||
                   lower.Contains("\\appdata\\local\\temp\\") ||
                   lower.Contains("\\appdata\\local\\microsoft\\windows\\explorer\\") ||
                   lower.Contains("\\appdata\\local\\microsoft\\windows\\inetcache\\") ||
                   lower.Contains("\\appdata\\local\\crashdumps\\") ||
                   lower.Contains("\\inetpub\\logs\\") ||
                   lower.Contains("\\appdata\\local\\microsoft\\windows\\explorer\\thumbcache_") ||
                   lower.EndsWith("\\ntuser.dat");
        }
    }
}
