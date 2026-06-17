using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SystemManager.Services;
using SystemManager.Views;

namespace SystemManager.ViewModels
{
    public class ExplorerViewModel : ViewModelBase, IDisposable
    {
        private string _currentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        private ObservableCollection<FileSystemItem> _items = new();
        private ObservableCollection<FileSystemItem> _filteredItems = new();
        private FileSystemItem? _selectedItem;
        private FileSystemItem? _clipboardItem;
        private bool _isCutOperation;
        private string _searchText = "";
        private string _statusText = "";
        private string _previewText = "";
        private FileSystemWatcher? _folderWatcher;
        private bool _isDualPanel;
        private string _secondPanelPath = "";
        private ObservableCollection<FileSystemItem> _secondPanelItems = new();
        private string _secondPanelSearch = "";
        private FileSystemItem? _selectedItemPanel2;

        public string CurrentPath
        {
            get => _currentPath;
            set { if (SetProperty(ref _currentPath, value)) { LoadItems(); UpdateWatcher(); } }
        }

        public ObservableCollection<FileSystemItem> Items
        {
            get => _items;
            set { _items = value; ApplyFilter(); OnPropertyChanged(); }
        }

        public ObservableCollection<FileSystemItem> FilteredItems
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(); }
        }

        public FileSystemItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedItemInfo));
                LoadPreview();
            }
        }

        public string PreviewText
        {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
        }

        public string SelectedItemInfo => SelectedItem == null ? "" :
            SelectedItem.IsDirectory ? $"Папка: {SelectedItem.Name}" :
            $"{SelectedItem.Name} — {SelectedItem.FormattedSize}";

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsDualPanel
        {
            get => _isDualPanel;
            set => SetProperty(ref _isDualPanel, value);
        }

        public string SecondPanelPath
        {
            get => _secondPanelPath;
            set { if (SetProperty(ref _secondPanelPath, value)) LoadSecondPanelItems(); }
        }

        public ObservableCollection<FileSystemItem> SecondPanelItems
        {
            get => _secondPanelItems;
            set => SetProperty(ref _secondPanelItems, value);
        }

        public string SecondPanelSearch
        {
            get => _secondPanelSearch;
            set { _secondPanelSearch = value; OnPropertyChanged(); ApplySecondPanelFilter(); }
        }

        public FileSystemItem? SelectedItemPanel2
        {
            get => _selectedItemPanel2;
            set => SetProperty(ref _selectedItemPanel2, value);
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
        public ICommand ToggleDualPanelCommand { get; }
        public ICommand ExtractArchiveCommand { get; }
        public ICommand AddToArchiveCommand { get; }
        public ICommand CalculateHashCommand { get; }
        public ICommand NavigateSecondPanelCommand { get; }
        public ICommand CopyToSecondPanelCommand { get; }
        public ICommand MoveToSecondPanelCommand { get; }

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
            ToggleDualPanelCommand = new RelayCommand(_ => IsDualPanel = !IsDualPanel);
            ExtractArchiveCommand = new RelayCommand(param => ExtractArchive(param as FileSystemItem));
            AddToArchiveCommand = new RelayCommand(param => AddToArchive(param as FileSystemItem));
            CalculateHashCommand = new RelayCommand(async param => await ShowHashesAsync(param as FileSystemItem));
            NavigateSecondPanelCommand = new RelayCommand(param => NavigateToSecondPanel(param?.ToString() ?? ""));
            CopyToSecondPanelCommand = new RelayCommand(_ => CopySelectedToSecondPanel());
            MoveToSecondPanelCommand = new RelayCommand(_ => MoveSelectedToSecondPanel());
            NavigateCommand = new RelayCommand(param => NavigateTo(param?.ToString() ?? ""));
            DoubleClickCommand = new RelayCommand(_ => NavigateTo(SelectedItem?.FullPath ?? ""));
            NavigateHomeCommand = new RelayCommand(_ => NavigateHome());

            SecondPanelPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            LoadItems();
        }

        private void NavigateHome() => CurrentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

        private void OpenInNotepad(FileSystemItem? item)
        {
            if (item == null || item.IsDirectory) return;
            try
            {
                FileMonitorService.MarkWrexOperation(item.FullPath);
                var win = new NotepadView();
                var vm = new NotepadViewModel();
                vm.LoadFile(item.FullPath);
                win.DataContext = vm;
                win.Owner = Application.Current?.MainWindow;
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }

        private void OpenItem(FileSystemItem? item)
        {
            if (item == null) return;
            if (item.IsArchive && File.Exists(item.FullPath)) { BrowseArchive(item.FullPath); return; }
            NavigateTo(item.FullPath);
        }

        private void BrowseArchive(string archivePath)
        {
            try
            {
                if (!archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return;
                using var archive = ZipFile.OpenRead(archivePath);
                var newItems = new ObservableCollection<FileSystemItem>();
                foreach (var entry in archive.Entries)
                {
                    bool isDir = string.IsNullOrEmpty(entry.Name);
                    string name = isDir ? entry.FullName.TrimEnd('/') : entry.Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    newItems.Add(new FileSystemItem
                    {
                        Name = name, FullPath = entry.FullName,
                        Icon = isDir ? "📁" : FormatUtils.GetFileIcon(name),
                        IsDirectory = isDir, FileSize = entry.Length,
                        FormattedSize = FormatUtils.FormatSize(entry.Length),
                        ArchiveParentPath = archivePath
                    });
                }
                Items = newItems;
                StatusText = $"📦 {Path.GetFileName(archivePath)} ({archive.Entries.Count} элементов)";
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка чтения архива: {ex.Message}", "Архив"); }
        }

        private void ExtractArchive(FileSystemItem? item)
        {
            if (item?.ArchiveParentPath == null) return;
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Выберите папку для извлечения" };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ZipFile.ExtractToDirectory(item.ArchiveParentPath, dialog.SelectedPath, true);
                    HistoryService.Log("Извлечение архива", $"{item.ArchiveParentPath} -> {dialog.SelectedPath}", "File");
                    MessageBox.Show("Архив успешно извлечён!", "Готово");
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка извлечения: {ex.Message}", "Ошибка"); }
        }

        private void AddToArchive(FileSystemItem? item)
        {
            if (item == null) return;
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "ZIP (*.zip)|*.zip", DefaultExt = ".zip", FileName = $"{item.Name}.zip" };
                if (dlg.ShowDialog() == true)
                {
                    using var archive = ZipFile.Open(dlg.FileName, ZipArchiveMode.Create);
                    if (item.IsDirectory) AddDirToZip(archive, item.FullPath, item.Name);
                    else archive.CreateEntryFromFile(item.FullPath, item.Name);
                    HistoryService.Log("Создание архива", $"{item.FullPath} -> {dlg.FileName}", "File");
                    MessageBox.Show("Архив создан!", "Готово");
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private static void AddDirToZip(ZipArchive archive, string sourceDir, string entryName)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
                archive.CreateEntryFromFile(file, Path.Combine(entryName, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(sourceDir))
                AddDirToZip(archive, dir, Path.Combine(entryName, Path.GetFileName(dir)));
        }

        private async Task ShowHashesAsync(FileSystemItem? item)
        {
            if (item == null || item.IsDirectory) return;
            try
            {
                StatusText = "Вычисление хэшей...";
                var (md5, sha1, sha256) = await Task.Run(() =>
                {
                    using var stream = File.OpenRead(item.FullPath);
                    using var m = MD5.Create(); using var s1 = SHA1.Create(); using var s256 = SHA256.Create();
                    var md5H = Convert.ToHexString(m.ComputeHash(stream)); stream.Position = 0;
                    var sha1H = Convert.ToHexString(s1.ComputeHash(stream)); stream.Position = 0;
                    var sha256H = Convert.ToHexString(s256.ComputeHash(stream));
                    return (md5H, sha1H, sha256H);
                });
                MessageBox.Show($"Файл: {item.Name}\n\nMD5:\n{md5}\n\nSHA-1:\n{sha1}\n\nSHA-256:\n{sha256}",
                    "Хэш-суммы", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText = "Хэши вычислены";
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); StatusText = ""; }
        }

        private void CutItem(FileSystemItem? item) { if (item != null) { _clipboardItem = item; _isCutOperation = true; } }
        private void CopyItem(FileSystemItem? item) { if (item != null) { _clipboardItem = item; _isCutOperation = false; } }

        private void PasteItem()
        {
            if (_clipboardItem == null) return;
            try
            {
                var dest = Path.Combine(CurrentPath, _clipboardItem.Name);
                FileMonitorService.MarkWrexOperation(_clipboardItem.FullPath);
                FileMonitorService.MarkWrexOperation(dest);
                if (_isCutOperation) { if (_clipboardItem.IsDirectory) Directory.Move(_clipboardItem.FullPath, dest); else File.Move(_clipboardItem.FullPath, dest); _clipboardItem = null; _isCutOperation = false; }
                else { if (_clipboardItem.IsDirectory) CopyDir(_clipboardItem.FullPath, dest); else File.Copy(_clipboardItem.FullPath, dest, true); }
                LoadItems();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка вставки: {ex.Message}", "Проводник"); }
        }

        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        private void RenameItem(FileSystemItem? item)
        {
            if (item == null) return;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("Введите новое имя:", "Переименование", item.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;
            try
            {
                string newPath = Path.Combine(Path.GetDirectoryName(item.FullPath)!, newName);
                FileMonitorService.MarkWrexOperation(item.FullPath); FileMonitorService.MarkWrexOperation(newPath);
                if (item.IsDirectory) Directory.Move(item.FullPath, newPath); else File.Move(item.FullPath, newPath);
                LoadItems();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Проводник"); }
        }

        private void DeleteItem(FileSystemItem? item)
        {
            if (item == null) return;
            if (MessageBox.Show($"Удалить {item.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                FileMonitorService.MarkWrexOperation(item.FullPath);
                if (item.IsDirectory) Directory.Delete(item.FullPath, true); else File.Delete(item.FullPath);
                LoadItems();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Проводник"); }
        }

        private void ShowProperties(FileSystemItem? item)
        {
            if (item == null) return;
            string info = item.IsDirectory ? $"Папка: {item.Name}\nПуть: {item.FullPath}" :
                $"Файл: {item.Name}\nПуть: {item.FullPath}\nРазмер: {item.FormattedSize}\nСоздан: {new FileInfo(item.FullPath).CreationTime:dd.MM.yyyy HH:mm:ss}\nИзменён: {new FileInfo(item.FullPath).LastWriteTime:dd.MM.yyyy HH:mm:ss}";
            MessageBox.Show(info, "Свойства", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateNewFolder()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Имя папки:", "Новая папка", "Новая папка");
            if (string.IsNullOrWhiteSpace(name)) return;
            try { var p = Path.Combine(CurrentPath, name); FileMonitorService.MarkWrexOperation(p); Directory.CreateDirectory(p); LoadItems(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Проводник"); }
        }

        private void CreateNewFile()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Имя файла:", "Новый файл", "Новый файл.txt");
            if (string.IsNullOrWhiteSpace(name)) return;
            try { var p = Path.Combine(CurrentPath, name); FileMonitorService.MarkWrexOperation(p); File.Create(p).Close(); LoadItems(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Проводник"); }
        }

        private void NavigateTo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (Directory.Exists(path)) { CurrentPath = path; return; }
            if (!File.Exists(path)) return;
            var ext = Path.GetExtension(path).ToLower();
            if (FormatUtils.IsArchiveExtension(ext)) { BrowseArchive(path); return; }
            string[] textExt = { ".txt", ".log", ".cs", ".xml", ".json", ".md", ".ini", ".config", ".xaml", ".html", ".css", ".js" };
            if (Array.Exists(textExt, e => e == ext)) OpenInNotepad(new FileSystemItem { FullPath = path, Name = Path.GetFileName(path) });
            else { FileMonitorService.MarkWrexOperation(path); SystemLauncher.Launch(path); }
        }

        private void NavigateToSecondPanel(string path) { if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) SecondPanelPath = path; }

        private void CopySelectedToSecondPanel()
        {
            if (SelectedItem == null || string.IsNullOrEmpty(SecondPanelPath)) return;
            try
            {
                var dest = Path.Combine(SecondPanelPath, SelectedItem.Name);
                FileMonitorService.MarkWrexOperation(SelectedItem.FullPath); FileMonitorService.MarkWrexOperation(dest);
                if (SelectedItem.IsDirectory) CopyDir(SelectedItem.FullPath, dest); else File.Copy(SelectedItem.FullPath, dest, true);
                LoadSecondPanelItems(); MessageBox.Show($"Скопировано: {SelectedItem.Name}", "Готово");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private void MoveSelectedToSecondPanel()
        {
            if (SelectedItem == null || string.IsNullOrEmpty(SecondPanelPath)) return;
            try
            {
                var dest = Path.Combine(SecondPanelPath, SelectedItem.Name);
                FileMonitorService.MarkWrexOperation(SelectedItem.FullPath); FileMonitorService.MarkWrexOperation(dest);
                if (SelectedItem.IsDirectory) Directory.Move(SelectedItem.FullPath, dest); else File.Move(SelectedItem.FullPath, dest);
                LoadItems(); LoadSecondPanelItems(); MessageBox.Show($"Перемещено: {SelectedItem.Name}", "Готово");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private void ApplyFilter()
        {
            FilteredItems = string.IsNullOrWhiteSpace(_searchText)
                ? new ObservableCollection<FileSystemItem>(_items)
                : new ObservableCollection<FileSystemItem>(_items.Where(i => i.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
            StatusText = $"{FilteredItems.Count} элементов" + (string.IsNullOrWhiteSpace(_searchText) ? "" : $" (фильтр: \"{_searchText}\")");
        }

        private void ApplySecondPanelFilter()
        {
            if (string.IsNullOrWhiteSpace(_secondPanelSearch)) { OnPropertyChanged(nameof(SecondPanelItems)); return; }
            SecondPanelItems = new ObservableCollection<FileSystemItem>(_secondPanelItems.Where(i => i.Name.Contains(_secondPanelSearch, StringComparison.OrdinalIgnoreCase)));
        }

        private void LoadItems()
        {
            var newItems = new ObservableCollection<FileSystemItem>();
            try
            {
                if (CurrentPath == "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}")
                {
                    foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                        newItems.Add(new FileSystemItem { Name = drive.Name, FullPath = drive.Name, Icon = "💽", IsDirectory = true, FormattedSize = $"{FormatUtils.FormatSize(drive.AvailableFreeSpace)} свободно из {FormatUtils.FormatSize(drive.TotalSize)}" });
                }
                else if (Directory.Exists(CurrentPath))
                {
                    foreach (var dir in Directory.GetDirectories(CurrentPath))
                    {
                        try { newItems.Add(new FileSystemItem { Name = Path.GetFileName(dir), FullPath = dir, Icon = "📁", IsDirectory = true, FormattedSize = new DirectoryInfo(dir).GetFileSystemInfos().Length + " элементов" }); } catch { }
                    }
                    foreach (var file in Directory.GetFiles(CurrentPath))
                    {
                        try { var fi = new FileInfo(file); var ext = Path.GetExtension(file).ToLower(); newItems.Add(new FileSystemItem { Name = Path.GetFileName(file), FullPath = file, Icon = FormatUtils.IsArchiveExtension(ext) ? "📦" : FormatUtils.GetFileIcon(Path.GetFileName(file)), IsDirectory = false, IsArchive = FormatUtils.IsArchiveExtension(ext), FileSize = fi.Length, FormattedSize = FormatUtils.FormatSize(fi.Length) }); } catch { }
                    }
                }
                else { CurrentPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"; return; }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка доступа: {ex.Message}", "Проводник"); }
            Items = newItems;
            StatusText = $"{FilteredItems.Count} элементов";
        }

        private void LoadSecondPanelItems()
        {
            var newItems = new ObservableCollection<FileSystemItem>();
            try
            {
                if (!string.IsNullOrEmpty(SecondPanelPath) && Directory.Exists(SecondPanelPath))
                {
                    foreach (var dir in Directory.GetDirectories(SecondPanelPath)) { try { newItems.Add(new FileSystemItem { Name = Path.GetFileName(dir), FullPath = dir, Icon = "📁", IsDirectory = true }); } catch { } }
                    foreach (var file in Directory.GetFiles(SecondPanelPath)) { try { var fi = new FileInfo(file); newItems.Add(new FileSystemItem { Name = Path.GetFileName(file), FullPath = file, Icon = FormatUtils.GetFileIcon(Path.GetFileName(file)), IsDirectory = false, FileSize = fi.Length, FormattedSize = FormatUtils.FormatSize(fi.Length) }); } catch { } }
                }
            } catch { }
            SecondPanelItems = newItems;
        }

        private void UpdateWatcher()
        {
            _folderWatcher?.Dispose(); _folderWatcher = null;
            if (CurrentPath.StartsWith("::") || !Directory.Exists(CurrentPath)) return;
            try
            {
                _folderWatcher = new FileSystemWatcher(CurrentPath) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite, IncludeSubdirectories = false, EnableRaisingEvents = true };
                _folderWatcher.Created += (_, _) => Application.Current?.Dispatcher?.BeginInvoke(LoadItems);
                _folderWatcher.Deleted += (_, _) => Application.Current?.Dispatcher?.BeginInvoke(LoadItems);
                _folderWatcher.Renamed += (_, _) => Application.Current?.Dispatcher?.BeginInvoke(LoadItems);
                _folderWatcher.Changed += (_, _) => Application.Current?.Dispatcher?.BeginInvoke(LoadItems);
            } catch { }
        }

        public void Dispose() { _folderWatcher?.Dispose(); }

        private void LoadPreview()
        {
            if (SelectedItem == null || SelectedItem.IsDirectory) { PreviewText = ""; return; }
            try
            {
                var ext = Path.GetExtension(SelectedItem.FullPath).ToLower();
                string[] textExts = { ".txt", ".log", ".cs", ".xml", ".json", ".md", ".ini", ".config", ".xaml", ".html", ".css", ".js", ".py", ".bat", ".cmd", ".ps1", ".reg" };
                if (Array.Exists(textExts, e => e == ext))
                {
                    var content = File.ReadAllText(SelectedItem.FullPath);
                    PreviewText = content.Length > 5000 ? content[..5000] + "\n\n... (обрезано)" : content;
                }
                else
                {
                    PreviewText = $"Файл: {SelectedItem.Name}\nРазмер: {SelectedItem.FormattedSize}\nТип: {ext}";
                }
            }
            catch { PreviewText = "Не удалось загрузить предпросмотр"; }
        }
    }
}
