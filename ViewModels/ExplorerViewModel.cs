using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SystemManager.Services;
using SystemManager.Views;

namespace SystemManager.ViewModels
{
    public class ExplorerViewModel : INotifyPropertyChanged
    {
        private string _currentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        private ObservableCollection<FileSystemItem> _items = new();
        private FileSystemItem? _selectedItem;
        private FileSystemItem? _clipboardItem;
        private bool _isCutOperation;

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _currentPath = value;
                OnPropertyChanged();
                LoadItems();
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

        public ICommand NavigateCommand { get; }
        public ICommand DoubleClickCommand { get; }
        public ICommand NavigateHomeCommand { get; }

        public ICommand OpenCommand { get; }
        public ICommand OpenInNotepadCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PropertiesCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand CreateFileCommand { get; }
        public ICommand RefreshCommand { get; }

        public ExplorerViewModel()
        {
            OpenCommand = new RelayCommand(param => OpenItem(param as FileSystemItem));
            OpenInNotepadCommand = new RelayCommand(param => OpenInNotepad(param as FileSystemItem));
            CutCommand = new RelayCommand(param => CutItem(param as FileSystemItem));
            CopyCommand = new RelayCommand(param => CopyItem(param as FileSystemItem));
            PasteCommand = new RelayCommand(_ => PasteItem());
            RenameCommand = new RelayCommand(param => RenameItem(param as FileSystemItem));
            DeleteCommand = new RelayCommand(param => DeleteItem(param as FileSystemItem));
            PropertiesCommand = new RelayCommand(param => ShowProperties(param as FileSystemItem));
            CreateFolderCommand = new RelayCommand(_ => CreateNewFolder());
            CreateFileCommand = new RelayCommand(_ => CreateNewFile());
            RefreshCommand = new RelayCommand(_ => LoadItems());

            NavigateCommand = new RelayCommand(param => NavigateTo(param?.ToString() ?? ""));
            DoubleClickCommand = new RelayCommand(_ => NavigateTo(SelectedItem?.FullPath ?? ""));
            NavigateHomeCommand = new RelayCommand(_ => NavigateHome());
            
            LoadItems();
        }

        private void NavigateHome()
        {
            CurrentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        }

        private void OpenInNotepad(FileSystemItem? item)
        {
            if (item == null || item.IsDirectory) return;

            try
            {
                FileMonitorService.MarkWrexOperation(item.FullPath);

                var notepadWindow = new NotepadView();
                var notepadVm = new NotepadViewModel();

                notepadVm.LoadFile(item.FullPath);
                notepadWindow.DataContext = notepadVm;
                 
                notepadWindow.Owner = System.Windows.Application.Current?.MainWindow;
                notepadWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Open Notepad Error");
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
        }

        private void CopyItem(FileSystemItem? item)
        {
            if (item == null) return;
            _clipboardItem = item;
            _isCutOperation = false;
        }

        private void PasteItem()
        {
            if (_clipboardItem == null) return;

            try
            {
                string destinationPath = Path.Combine(CurrentPath, _clipboardItem.Name);
                
                FileMonitorService.MarkWrexOperation(_clipboardItem.FullPath);
                FileMonitorService.MarkWrexOperation(destinationPath);

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
                "Введите новое имя:", "Переименование", item.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

            try
            {
                string newPath = Path.Combine(Path.GetDirectoryName(item.FullPath)!, newName);
                
                FileMonitorService.MarkWrexOperation(item.FullPath);
                FileMonitorService.MarkWrexOperation(newPath);

                if (item.IsDirectory)
                    Directory.Move(item.FullPath, newPath);
                else
                    File.Move(item.FullPath, newPath);

                LoadItems();
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
                $"Удалить {item.Name}?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                FileMonitorService.MarkWrexOperation(item.FullPath);

                if (item.IsDirectory)
                    Directory.Delete(item.FullPath, true);
                else
                    File.Delete(item.FullPath);

                LoadItems();
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
                : $"Файл: {item.Name}\nПуть: {item.FullPath}\nРазмер: {item.FormattedSize}";

            System.Windows.MessageBox.Show(info, "Свойства");
        }

        private void CreateNewFolder()
        {
            string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите имя папки:", "Новая папка", "Новая папка");

            if (string.IsNullOrWhiteSpace(folderName)) return;

            try
            {
                var newPath = Path.Combine(CurrentPath, folderName);
                
                FileMonitorService.MarkWrexOperation(newPath);
                
                Directory.CreateDirectory(newPath);
                LoadItems();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка создания: {ex.Message}", "Проводник");
            }
        }

        private void CreateNewFile()
        {
            string fileName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите имя файла:", "Новый файл", "Новый файл.txt");

            if (string.IsNullOrWhiteSpace(fileName)) return;

            try
            {
                var newPath = Path.Combine(CurrentPath, fileName);
                
                FileMonitorService.MarkWrexOperation(newPath);
                
                File.Create(newPath).Close();
                LoadItems();
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
                string extension = Path.GetExtension(path).ToLower();
                string[] textExtensions = { ".txt", ".log", ".cs", ".xml", ".json", ".md", ".ini", ".config", ".xaml", ".html", ".css", ".js" };
                
                if (Array.Exists(textExtensions, ext => ext == extension))
                {
                    OpenInNotepad(new FileSystemItem { FullPath = path, Name = Path.GetFileName(path) });
                }
                else
                {
                    FileMonitorService.MarkWrexOperation(path);
                    SystemLauncher.Launch(path);
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }

        private void LoadItems()
        {
            var newItems = new ObservableCollection<FileSystemItem>();
            try
            {
                if (CurrentPath == "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}")
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            newItems.Add(new FileSystemItem
                            {
                                Name = drive.Name,
                                FullPath = drive.Name,
                                Icon = "💽",
                                IsDirectory = true,
                                FileSize = 0,
                                FormattedSize = FormatFileSize(drive.TotalSize)
                            });
                        }
                    }
                }
                else if (Directory.Exists(CurrentPath))
                {
                    foreach (var dir in Directory.GetDirectories(CurrentPath))
                    {
                        newItems.Add(new FileSystemItem
                        {
                            Name = Path.GetFileName(dir),
                            FullPath = dir,
                            Icon = "📁",
                            IsDirectory = true,
                            FileSize = 0,
                            FormattedSize = "<Папка>"
                        });
                    }
                    
                    foreach (var file in Directory.GetFiles(CurrentPath))
                    {
                        var fileInfo = new FileInfo(file);
                        newItems.Add(new FileSystemItem
                        {
                            Name = Path.GetFileName(file),
                            FullPath = file,
                            Icon = "📄",
                            IsDirectory = false,
                            FileSize = fileInfo.Length,
                            FormattedSize = FormatFileSize(fileInfo.Length)
                        });
                    }
                }
                else
                {
                    CurrentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка доступа: {ex.Message}", "Проводник");
            }
            Items = newItems;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}