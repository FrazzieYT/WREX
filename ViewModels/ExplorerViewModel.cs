using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class ExplorerViewModel : INotifyPropertyChanged
    {
        private string _currentPath = string.Empty;
        private ObservableCollection<FileSystemItem> _items = new();
        private FileSystemItem? _selectedItem;
        private FileSystemItem? _clipboardItem;
        private bool _isCutOperation;
        
        private readonly bool _isWinRe;
        private readonly string? _offlineWindowsPath;
        
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _currentPath = value;
                OnPropertyChanged();
                LoadItems();
                HistoryService.Log("Навигация", $"Открыта папка: {value}", "Navigation");
            }
        }

        public ObservableCollection<FileSystemItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public FileSystemItem? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public bool IsWinRe => _isWinRe;
        public string? OfflineWindowsPath => _offlineWindowsPath;

        public ICommand NavigateCommand { get; }
        public ICommand DoubleClickCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PropertiesCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand CreateFileCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateToOfflineWindowsCommand { get; }
        public ICommand NavigateToRootCommand { get; }

        public ExplorerViewModel()
        {
            _isWinRe = RegistryService.IsWinRE();
            _offlineWindowsPath = RegistryService.DetectOfflineWindowsPath();
            
            _currentPath = _isWinRe ? "ROOT" : ":: {20D04FE0-3AEA-1069-A2D8-08002B30309D}";

            OpenCommand = new RelayCommand(param => OpenItem(param as FileSystemItem));
            CutCommand = new RelayCommand(param => CutItem(param as FileSystemItem));
            CopyCommand = new RelayCommand(param => CopyItem(param as FileSystemItem));
            PasteCommand = new RelayCommand(_ => PasteItem());
            RenameCommand = new RelayCommand(param => RenameItem(param as FileSystemItem));
            DeleteCommand = new RelayCommand(param => DeleteItem(param as FileSystemItem));
            PropertiesCommand = new RelayCommand(param => ShowProperties(param as FileSystemItem));
            CreateFolderCommand = new RelayCommand(_ => CreateNewFolder());
            CreateFileCommand = new RelayCommand(_ => CreateNewFile());
            RefreshCommand = new RelayCommand(_ => LoadItems());
            NavigateToOfflineWindowsCommand = new RelayCommand(_ => NavigateToOfflineWindows());
            NavigateToRootCommand = new RelayCommand(_ => { CurrentPath = "ROOT"; });

            NavigateCommand = new RelayCommand(param => NavigateTo(param?.ToString() ?? string.Empty));
            DoubleClickCommand = new RelayCommand(_ => NavigateTo(SelectedItem?.FullPath ?? string.Empty));

            LoadItems();
            HistoryService.Log("Открыт проводник", _isWinRe ? "WinRE среда обнаружена" : "Пользователь открыл файловый менеджер", "Navigation");
        }

        private void NavigateToOfflineWindows()
        {
            if (!string.IsNullOrEmpty(_offlineWindowsPath) && Directory.Exists(_offlineWindowsPath))
            {
                CurrentPath = _offlineWindowsPath;
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Не удалось найти установленную Windows на доступных дисках.",
                    "WinRE", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void OpenItem(FileSystemItem? item)
        {
            if (item == null) return;
            NavigateTo(item.FullPath);
        }

        private void CutItem(FileSystemItem? item)
        {
            if (item == null) return;
            _clipboardItem = item;
            _isCutOperation = true;
            HistoryService.Log("Вырезано", item.FullPath, "File");
        }

        private void CopyItem(FileSystemItem? item)
        {
            if (item == null) return;
            _clipboardItem = item;
            _isCutOperation = false;
            HistoryService.Log("Скопировано", item.FullPath, "File");
        }

        private void PasteItem()
        {
            if (_clipboardItem == null) return;

            try
            {
                string destinationPath = Path.Combine(CurrentPath, _clipboardItem.Name);

                if (_isCutOperation)
                {
                    if (_clipboardItem.IsDirectory)
                        Directory.Move(_clipboardItem.FullPath, destinationPath);
                    else
                        File.Move(_clipboardItem.FullPath, destinationPath);

                    _clipboardItem = null;
                    _isCutOperation = false;
                }
                else
                {
                    if (_clipboardItem.IsDirectory)
                        CopyDirectory(_clipboardItem.FullPath, destinationPath);
                    else
                        File.Copy(_clipboardItem.FullPath, destinationPath, true);
                }

                LoadItems();
                HistoryService.Log("Вставлено", destinationPath, "File");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка вставки: {ex.Message}", "Проводник");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }

        private void RenameItem(FileSystemItem? item)
        {
            if (item == null) return;
            
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новое имя: ", "Переименование", item.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

            try
            {
                string? dirName = Path.GetDirectoryName(item.FullPath);
                if (string.IsNullOrEmpty(dirName)) return;
                
                string newPath = Path.Combine(dirName, newName);

                if (item.IsDirectory)
                    Directory.Move(item.FullPath, newPath);
                else
                    File.Move(item.FullPath, newPath);

                LoadItems();
                HistoryService.Log("Переименовано", $"{item.Name} → {newName}", "File");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка переименования: {ex.Message}", "Проводник");
            }
        }

        private void DeleteItem(FileSystemItem? item)
        {
            if (item == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Удалить {item.Name}?", "Подтверждение", System.Windows.MessageBoxButton.YesNo);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.FullPath, true);
                else
                    File.Delete(item.FullPath);

                LoadItems();
                HistoryService.Log("Удалено", item.FullPath, "File");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка удаления: {ex.Message}", "Проводник");
            }
        }

        private void ShowProperties(FileSystemItem? item)
        {
            if (item == null) return;

            string info = item.IsDirectory
                ? $"Папка: {item.Name}\nПуть: {item.FullPath}"
                : $"Файл: {item.Name}\nПуть: {item.FullPath}";

            if (item.IsDirectory)
            {
                try
                {
                    var files = Directory.GetFiles(item.FullPath, "*", SearchOption.AllDirectories);
                    long totalSize = 0L;
                    foreach (var f in files)
                    {
                        try { totalSize += new FileInfo(f).Length; } 
                        catch { /* Игнорируем ошибки доступа к отдельным файлам */ }
                    }
                    info += $"\nФайлов: {files.Length}\nРазмер: {FormatSize(totalSize)}";
                }
                catch { /* Игнорируем ошибки доступа к паке */ }
            }

            System.Windows.MessageBox.Show(info, "Свойства");
        }

        private void CreateNewFolder()
        {
            string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите имя папки: ", "Новая папка", "Новая папка");

            if (string.IsNullOrWhiteSpace(folderName)) return;

            try
            {
                Directory.CreateDirectory(Path.Combine(CurrentPath, folderName));
                LoadItems();
                HistoryService.Log("Создана папка", folderName, "File");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка создания: {ex.Message}", "Проводник");
            }
        }

        private void CreateNewFile()
        {
            string fileName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите имя файла: ", "Новый файл", "Новый файл.txt");

            if (string.IsNullOrWhiteSpace(fileName)) return;

            try
            {
                File.Create(Path.Combine(CurrentPath, fileName)).Close();
                LoadItems();
                HistoryService.Log("Создан файл", fileName, "File");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка создания: {ex.Message}", "Проводник");
            }
        }

        private void NavigateTo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (Directory.Exists(path))
            {
                CurrentPath = path;
            }
            else if (File.Exists(path))
            {
                SystemLauncher.Launch(path);
                HistoryService.Log("Открыт файл", path, "File");
            }
        }

        private void LoadItems()
        {
            var newItems = new ObservableCollection<FileSystemItem>();
            
            try
            {
                if (CurrentPath == "ROOT" || CurrentPath == ":: {20D04FE0-3AEA-1069-A2D8-08002B30309D}")
                {
                    if (_isWinRe && !string.IsNullOrEmpty(_offlineWindowsPath))
                    {
                        newItems.Add(new FileSystemItem
                        {
                            Name = $"🖥️ Целевая Windows ({_offlineWindowsPath})",
                            FullPath = _offlineWindowsPath,
                            Icon = "🖥️",
                            IsDirectory = true
                        });
                    }

                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            string label = string.Empty;
                            try { label = drive.VolumeLabel; } 
                            catch { /* Игнорируем ошибки доступа к метке тома */ }

                            string driveLabel = drive.Name.TrimEnd('\\');
                            if (drive.Name.StartsWith("X:\\", StringComparison.OrdinalIgnoreCase))
                                driveLabel = "X: (WinRE)";
                            else if (!string.IsNullOrEmpty(label))
                                driveLabel = $"{drive.Name.TrimEnd('\\')} ({label})";

                            newItems.Add(new FileSystemItem
                            {
                                Name = driveLabel,
                                FullPath = drive.RootDirectory.FullName,
                                Icon = "💽",
                                IsDirectory = true
                            });
                        }
                    }
                }
                else if (Directory.Exists(CurrentPath))
                {
                    newItems.Add(new FileSystemItem
                    {
                        Name = ".. (корневой уровень)",
                        FullPath = "ROOT",
                        Icon = "⬆️",
                        IsDirectory = true
                    });

                    var parent = Directory.GetParent(CurrentPath);
                    if (parent != null)
                    {
                        newItems.Add(new FileSystemItem
                        {
                            Name = ".. (вверх)",
                            FullPath = parent.FullName,
                            Icon = "🔼",
                            IsDirectory = true
                        });
                    }

                    foreach (var dir in Directory.GetDirectories(CurrentPath))
                    {
                        newItems.Add(new FileSystemItem
                        {
                            Name = Path.GetFileName(dir),
                            FullPath = dir,
                            Icon = "📁",
                            IsDirectory = true
                        });
                    }
                    
                    foreach (var file in Directory.GetFiles(CurrentPath))
                    {
                        newItems.Add(new FileSystemItem
                        {
                            Name = Path.GetFileName(file),
                            FullPath = file,
                            Icon = "📄",
                            IsDirectory = false
                        });
                    }
                }
                else
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            newItems.Add(new FileSystemItem
                            {
                                Name = drive.Name,
                                FullPath = drive.RootDirectory.FullName,
                                Icon = "💽",
                                IsDirectory = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            { 
                System.Windows.MessageBox.Show($"Ошибка доступа: {ex.Message}", "Проводник");
                HistoryService.Log("Ошибка доступа", ex.Message, "File");
            }
            
            Items = newItems;
        }

        private static string FormatSize(long bytes)
        {
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)   
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}