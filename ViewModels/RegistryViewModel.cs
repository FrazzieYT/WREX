using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SystemManager.Models;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class RegistryViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<RegistryTreeNode> _rootKeys = new();
        private readonly ObservableCollection<RegistryValueItem> _values = new();
        private readonly ObservableCollection<RegistrySearchResult> _searchResults = new();
        private readonly ObservableCollection<FavoriteRegistryEntry> _favorites = new();
        private readonly HashSet<string> _recentlyChangedKeys = new(StringComparer.OrdinalIgnoreCase);

        private RegistryTreeNode? _selectedNode;
        private RegistryValueItem? _selectedValue;
        private string _searchText = string.Empty;
        private bool _isSearching;
        private bool _searchKeys = true;
        private bool _searchValues = true;
        private bool _searchValueData = true;
        private string _statusMessage = "Готово";

        public ObservableCollection<RegistryTreeNode> RootKeys => _rootKeys;
        public ObservableCollection<RegistryValueItem> Values => _values;
        public ObservableCollection<RegistrySearchResult> SearchResults => _searchResults;
        public ObservableCollection<FavoriteRegistryEntry> Favorites => _favorites;

        public RegistryTreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                _selectedNode = value;
                OnPropertyChanged();
                LoadValues();
            }
        }

        public RegistryValueItem? SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
            }
        }

        public bool SearchKeys
        {
            get => _searchKeys;
            set
            {
                _searchKeys = value;
                OnPropertyChanged();
            }
        }

        public bool SearchValues
        {
            get => _searchValues;
            set
            {
                _searchValues = value;
                OnPropertyChanged();
            }
        }

        public bool SearchValueData
        {
            get => _searchValueData;
            set
            {
                _searchValueData = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand CreateKeyCommand { get; }
        public ICommand DeleteKeyCommand { get; }
        public ICommand RenameKeyCommand { get; }
        public ICommand CreateValueCommand { get; }
        public ICommand DeleteValueCommand { get; }
        public ICommand RenameValueCommand { get; }
        public ICommand EditValueCommand { get; }
        public ICommand AddFavoriteCommand { get; }
        public ICommand RemoveFavoriteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NavigateToFavoriteCommand { get; }
        public ICommand NavigateToSearchResultCommand { get; }
        public ICommand CopyKeyPathCommand { get; }
        public ICommand ExportKeyCommand { get; }
        public ICommand CopyValueNameCommand { get; }
        public ICommand CopyValueDataCommand { get; }
        public ICommand CopySearchResultPathCommand { get; }
        public ICommand LoadOfflineHiveCommand { get; }
        public ICommand UnloadOfflineHiveCommand { get; }
        public ICommand CompareBranchesCommand { get; }
        public ICommand ImportKeyCommand { get; }

        public RegistryViewModel()
        {
            CreateKeyCommand = new RelayCommand(_ => CreateKey());
            DeleteKeyCommand = new RelayCommand(_ => DeleteKey());
            RenameKeyCommand = new RelayCommand(_ => RenameKey());
            CreateValueCommand = new RelayCommand(_ => CreateValue());
            DeleteValueCommand = new RelayCommand(param => DeleteValue(param as RegistryValueItem));
            RenameValueCommand = new RelayCommand(param => RenameValue(param as RegistryValueItem));
            EditValueCommand = new RelayCommand(param => EditValue(param as RegistryValueItem));
            AddFavoriteCommand = new RelayCommand(param => AddFavorite(param as RegistryValueItem));
            RemoveFavoriteCommand = new RelayCommand(param => RemoveFavorite(param as FavoriteRegistryEntry));
            SearchCommand = new RelayCommand(_ => Search());
            NavigateToFavoriteCommand = new RelayCommand(param => NavigateToFavorite(param as FavoriteRegistryEntry));
            NavigateToSearchResultCommand = new RelayCommand(param => NavigateToSearchResult(param as RegistrySearchResult));
            CopyKeyPathCommand = new RelayCommand(_ => CopyKeyPath());
            ExportKeyCommand = new RelayCommand(_ => ExportKey());
            CopyValueNameCommand = new RelayCommand(param => CopyValueName(param as RegistryValueItem));
            CopyValueDataCommand = new RelayCommand(param => CopyValueData(param as RegistryValueItem));
            CopySearchResultPathCommand = new RelayCommand(param => CopySearchResultPath(param as RegistrySearchResult));
            LoadOfflineHiveCommand = new RelayCommand(param => LoadOfflineHive(param?.ToString()));
            UnloadOfflineHiveCommand = new RelayCommand(param => UnloadOfflineHive(param?.ToString()));
            CompareBranchesCommand = new RelayCommand(_ => CompareBranches());
            ImportKeyCommand = new RelayCommand(_ => ImportKey());

            LoadRootKeys();
            LoadFavorites();

            HistoryService.Log("Открыт редактор реестра", "Пользователь открыл вкладку реестра", "Registry");
        }

        private void LoadRootKeys()
        {
            _rootKeys.Clear();
            foreach (var hive in RegistryService.GetRootHives())
            {
                _rootKeys.Add(new RegistryTreeNode
                {
                    Hive = hive,
                    Name = RegistryService.HiveToString(hive),
                    FullPath = string.Empty
                });
            }
        }

        private void LoadOfflineHive(string? hiveName)
        {
            if (string.IsNullOrWhiteSpace(hiveName))
            {
                var dialog = new InputDialog("Загрузка offline hive",
                    "Введите имя hive (SYSTEM, SOFTWARE, SAM, SECURITY, DEFAULT):");
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
                {
                    hiveName = dialog.InputText.ToUpper();
                }
                else
                {
                    return;
                }
            }

            if (RegistryService.LoadOfflineHive(hiveName))
            {
                _rootKeys.Add(new RegistryTreeNode
                {
                    Hive = RegistryHive.LocalMachine,
                    Name = $"Offline_{hiveName} (загружен)",
                    FullPath = $"Offline_{hiveName}"
                });
                StatusMessage = $"Hive '{hiveName}' загружен как HKLM\\Offline_{hiveName}";
            }
            else
            {
                MessageBox.Show($"Не удалось загрузить hive '{hiveName}'. Убедитесь, что целевая Windows найдена.",
                    "Ошибка");
                StatusMessage = $"Ошибка загрузки hive: {hiveName}";
            }
        }

        private void UnloadOfflineHive(string? hiveName)
        {
            if (string.IsNullOrWhiteSpace(hiveName)) return;

            if (RegistryService.UnloadOfflineHive(hiveName))
            {
                var node = _rootKeys.FirstOrDefault(n => n.Name.Contains($"Offline_{hiveName}"));
                if (node != null) _rootKeys.Remove(node);
                StatusMessage = $"Hive '{hiveName}' выгружен";
            }
        }

        public void LoadChildren(RegistryTreeNode parentNode)
        {
            if (!parentNode.Hive.HasValue || parentNode.ChildrenLoaded) return;

            try
            {
                var subKeys = RegistryService.GetSubKeyNames(parentNode.Hive.Value, parentNode.FullPath);
                parentNode.Children.Clear();
                foreach (var subKey in subKeys.OrderBy(k => k))
                {
                    var childPath = string.IsNullOrEmpty(parentNode.FullPath)
                        ? subKey
                        : $"{parentNode.FullPath}\\{subKey}";
                    parentNode.Children.Add(new RegistryTreeNode
                    {
                        Hive = parentNode.Hive.Value,
                        Name = subKey,
                        FullPath = childPath
                    });
                }

                parentNode.ChildrenLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки подключей: {ex.Message}");
            }
        }

        private void CompareBranches()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите ключ для сравнения";
                return;
            }

            try
            {
                var baseKey = SelectedNode.Hive.Value switch
                {
                    RegistryHive.ClassesRoot => Registry.ClassesRoot,
                    RegistryHive.CurrentUser => Registry.CurrentUser,
                    RegistryHive.LocalMachine => Registry.LocalMachine,
                    RegistryHive.Users => Registry.Users,
                    RegistryHive.CurrentConfig => Registry.CurrentConfig,
                    _ => null
                };

                if (baseKey == null) return;

                using var key = baseKey.OpenSubKey(SelectedNode.FullPath, false);
                if (key == null) return;

                var values = new List<string>();
                foreach (var valueName in key.GetValueNames())
                {
                    var val = key.GetValue(valueName);
                    var kind = key.GetValueKind(valueName);
                    values.Add($"{valueName} ({kind}) = {val}");
                }

                var subKeys = key.GetSubKeyNames();

                var report = $"Ключ: {SelectedNode.Hive}\\{SelectedNode.FullPath}\n\n" +
                             $"Подключи ({subKeys.Length}):\n" +
                             string.Join("\n", subKeys.Select(k => $"  {k}")) +
                             $"\n\nЗначения ({values.Count}):\n" +
                             string.Join("\n", values.Select(v => $"  {v}"));

                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"RegistryExport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, report);

                MessageBox.Show($"Экспортировано в:\n{path}\n\n{report}",
                    $"Сравнение ветки: {SelectedNode.Name}",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                StatusMessage = $"Ветка экспортирована: {SelectedNode.FullPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        private void LoadValues()
        {
            _values.Clear();
            if (SelectedNode == null || !SelectedNode.Hive.HasValue) return;

            try
            {
                var valueNames = RegistryService.GetValueNames(SelectedNode.Hive.Value, SelectedNode.FullPath);
                foreach (var valueName in valueNames.OrderBy(v => v))
                {
                    var value = RegistryService.GetValue(SelectedNode.Hive.Value, SelectedNode.FullPath, valueName);
                    var kind =
                        RegistryService.GetValueKind(SelectedNode.Hive.Value, SelectedNode.FullPath, valueName) ??
                        RegistryValueKind.String;

                    _values.Add(new RegistryValueItem
                    {
                        Name = valueName,
                        Value = value,
                        Kind = kind,
                        FullPath = SelectedNode.FullPath,
                        Hive = SelectedNode.Hive.Value
                    });
                }

                StatusMessage = $"Загружено {_values.Count} значений";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        private void CreateKey()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите ключ для создания подключей";
                return;
            }

            var dialog = new InputDialog("Создание ключа", "Введите имя нового ключа:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    var newPath = string.IsNullOrEmpty(SelectedNode.FullPath)
                        ? dialog.InputText
                        : $"{SelectedNode.FullPath}\\{dialog.InputText}";
                    RegistryService.CreateKey(SelectedNode.Hive.Value, newPath);
                    SelectedNode.ChildrenLoaded = false;
                    LoadChildren(SelectedNode);
                    StatusMessage = $"Ключ создан: {newPath}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания ключа: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void DeleteKey()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue || SelectedNode.IsHive)
            {
                StatusMessage = "Выберите ключ для удаления";
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить ключ {SelectedNode.FullPath}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryService.DeleteKey(SelectedNode.Hive.Value, SelectedNode.FullPath, true);
                    StatusMessage = $"Ключ удален: {SelectedNode.FullPath}";

                    var parentPath = Path.GetDirectoryName(SelectedNode.FullPath) ?? string.Empty;
                    var parentNode = FindNode(_rootKeys, SelectedNode.Hive.Value, parentPath);
                    if (parentNode != null)
                    {
                        parentNode.ChildrenLoaded = false;
                        LoadChildren(parentNode);
                        SelectedNode = parentNode;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления ключа: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void RenameKey()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue || SelectedNode.IsHive)
            {
                StatusMessage = "Выберите ключ для переименования";
                return;
            }

            var dialog = new InputDialog("Переименование ключа", "Введите новое имя ключа:", SelectedNode.Name);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    var parentPath = Path.GetDirectoryName(SelectedNode.FullPath) ?? string.Empty;
                    var newPath = string.IsNullOrEmpty(parentPath)
                        ? dialog.InputText
                        : $"{parentPath}\\{dialog.InputText}";

                    RegistryService.CreateKey(SelectedNode.Hive.Value, newPath);

                    var valueNames = RegistryService.GetValueNames(SelectedNode.Hive.Value, SelectedNode.FullPath);
                    foreach (var valueName in valueNames)
                    {
                        var value = RegistryService.GetValue(SelectedNode.Hive.Value, SelectedNode.FullPath, valueName);
                        var kind = RegistryService.GetValueKind(SelectedNode.Hive.Value, SelectedNode.FullPath,
                            valueName) ?? RegistryValueKind.String;
                        RegistryService.SetValue(SelectedNode.Hive.Value, newPath, valueName, value!, kind);
                    }

                    RegistryService.DeleteKey(SelectedNode.Hive.Value, SelectedNode.FullPath, true);
                    StatusMessage = $"Ключ переименован: {SelectedNode.Name} → {dialog.InputText}";

                    var parentNode = FindNode(_rootKeys, SelectedNode.Hive.Value, parentPath);
                    if (parentNode != null)
                    {
                        parentNode.ChildrenLoaded = false;
                        LoadChildren(parentNode);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка переименования: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void CreateValue()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите ключ для создания значения";
                return;
            }

            var dialog = new InputDialog("Создание значения", "Введите имя нового значения:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    RegistryService.SetValue(SelectedNode.Hive.Value, SelectedNode.FullPath, dialog.InputText,
                        string.Empty, RegistryValueKind.String);
                    LoadValues();
                    StatusMessage = $"Значение создано: {dialog.InputText}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания значения: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void DeleteValue(RegistryValueItem? valueItem)
        {
            if (valueItem == null || SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите значение для удаления";
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить значение {valueItem.Name}?", "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryService.DeleteValue(SelectedNode.Hive.Value, SelectedNode.FullPath, valueItem.Name);
                    LoadValues();
                    StatusMessage = $"Значение удалено: {valueItem.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления значения: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void RenameValue(RegistryValueItem? valueItem)
        {
            if (valueItem == null || SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите значение для переименования";
                return;
            }

            var dialog = new InputDialog("Переименование значения", "Введите новое имя значения:", valueItem.Name);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    RegistryService.SetValue(SelectedNode.Hive.Value, SelectedNode.FullPath, dialog.InputText,
                        valueItem.Value!, valueItem.Kind);
                    RegistryService.DeleteValue(SelectedNode.Hive.Value, SelectedNode.FullPath, valueItem.Name);
                    LoadValues();
                    StatusMessage = $"Значение переименовано: {valueItem.Name} → {dialog.InputText}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка переименования: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void EditValue(RegistryValueItem? valueItem)
        {
            if (valueItem == null || SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите значение для редактирования";
                return;
            }

            var dialog = new EditValueDialog(valueItem.Name, valueItem.ValueDisplay, valueItem.Kind);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    RegistryService.SetValue(SelectedNode.Hive.Value, SelectedNode.FullPath, valueItem.Name,
                        dialog.NewValue, dialog.NewKind);
                    LoadValues();
                    StatusMessage = $"Значение изменено: {valueItem.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения значения: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void AddFavorite(RegistryValueItem? item)
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue)
            {
                StatusMessage = "Выберите ключ для добавления в избранное";
                return;
            }

            var valueName = item?.Name ?? SelectedValue?.Name;
            RegistryService.AddToFavorites(SelectedNode.Hive.Value, SelectedNode.FullPath, valueName);
            LoadFavorites();
            StatusMessage = "Добавлено в избранное";
        }

        private void RemoveFavorite(FavoriteRegistryEntry? entry)
        {
            if (entry == null)
            {
                StatusMessage = "Выберите элемент для удаления из избранного";
                return;
            }

            RegistryService.RemoveFromFavorites(entry);
            LoadFavorites();
            StatusMessage = "Удалено из избранного";
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "Введите поисковый запрос";
                return;
            }

            IsSearching = true;
            StatusMessage = "Поиск...";

            Task.Run(() =>
            {
                try
                {
                    var results = RegistryService.SearchRegistry(SearchText, null, SearchKeys, SearchValues,
                        SearchValueData, 100);

                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        _searchResults.Clear();
                        foreach (var result in results) _searchResults.Add(result);
                        StatusMessage = $"Найдено {results.Count} результатов";
                    });
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher?.Invoke(() => StatusMessage = $"Ошибка поиска: {ex.Message}");
                }
                finally
                {
                    Application.Current?.Dispatcher?.Invoke(() => IsSearching = false);
                }
            });
        }

        private void NavigateToFavorite(FavoriteRegistryEntry? entry)
        {
            if (entry == null) return;

            var node = FindNode(_rootKeys, entry.Hive, entry.KeyPath);
            if (node != null)
            {
                SelectedNode = node;
                StatusMessage = $"Переход к: {entry.DisplayName}";
            }
            else
            {
                StatusMessage = "Ключ не найден в дереве";
            }
        }

        private void NavigateToSearchResult(RegistrySearchResult? result)
        {
            if (result == null) return;

            try
            {
                var hiveNode = _rootKeys.FirstOrDefault(n => n.Hive == result.Hive);
                if (hiveNode == null) return;

                if (!hiveNode.ChildrenLoaded) LoadChildren(hiveNode);
                hiveNode.IsExpanded = true;

                var parts = result.KeyPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                var current = hiveNode;

                foreach (var part in parts)
                {
                    var child = current.Children.FirstOrDefault(c =>
                        c.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                    if (child == null) break;

                    if (!child.ChildrenLoaded) LoadChildren(child);
                    child.IsExpanded = true;
                    current = child;
                }

                current.IsSelected = true;
                SelectedNode = current;
                StatusMessage = $"Переход к: {result.FullPath}";
            }
            catch
            {
                StatusMessage = "Ключ не найден";
            }
        }

        private RegistryTreeNode? FindNode(ObservableCollection<RegistryTreeNode> nodes, RegistryHive hive, string path)
        {
            foreach (var node in nodes)
            {
                if (node.Hive == hive && node.FullPath == path)
                {
                    if (!node.ChildrenLoaded) LoadChildren(node);
                    return node;
                }

                if (node.Hive == hive && !string.IsNullOrEmpty(path) && path.StartsWith(node.FullPath + "\\"))
                {
                    if (!node.ChildrenLoaded) LoadChildren(node);
                    var found = FindNode(node.Children, hive, path);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private void LoadFavorites()
        {
            _favorites.Clear();
            foreach (var fav in RegistryService.GetFavorites())
            {
                _favorites.Add(fav);
            }
        }

        private void CopyKeyPath()
        {
            if (SelectedNode != null)
            {
                Clipboard.SetText(SelectedNode.FullPath);
                HistoryService.Log("Скопирован путь ключа", SelectedNode.FullPath, "Registry");
            }
        }

        private async void ExportKey()
        {
            if (SelectedNode == null || !SelectedNode.Hive.HasValue) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Файлы реестра (*.reg)|*.reg",
                DefaultExt = "reg",
                FileName = $"{SelectedNode.Name}.reg"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "Экспорт...";
                    var hiveName = SelectedNode.Hive.Value switch
                    {
                        Microsoft.Win32.RegistryHive.ClassesRoot => "HKCR",
                        Microsoft.Win32.RegistryHive.CurrentUser => "HKCU",
                        Microsoft.Win32.RegistryHive.LocalMachine => "HKLM",
                        Microsoft.Win32.RegistryHive.Users => "HKU",
                        Microsoft.Win32.RegistryHive.CurrentConfig => "HKCC",
                        _ => ""
                    };
                    var fullPath = $"{hiveName}\\{SelectedNode.FullPath}";

                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "reg.exe",
                            Arguments = $"export \"{fullPath}\" \"{dialog.FileName}\" /y",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var process = Process.Start(psi);
                        process?.WaitForExit();
                    });

                    HistoryService.Log("Экспорт ключа реестра", $"Путь: {fullPath} -> {dialog.FileName}", "Registry");
                    StatusMessage = "Ключ экспортирован в " + System.IO.Path.GetFileName(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private async void ImportKey()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Файлы реестра (*.reg)|*.reg|Все файлы (*.*)|*.*",
                DefaultExt = "reg",
                Title = "Импорт ключа реестра"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    $"Импортировать файл?\n\n{dialog.FileName}\n\n⚠️ Это может изменить настройки системы!",
                    "Подтверждение импорта",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    StatusMessage = "Импорт...";
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "reg.exe",
                            Arguments = $"import \"{dialog.FileName}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var process = Process.Start(psi);
                        process?.WaitForExit();
                    });

                    HistoryService.Log("Импорт ключа реестра", $"Файл: {dialog.FileName}", "Registry");
                    StatusMessage = "Импорт завершён: " + System.IO.Path.GetFileName(dialog.FileName);
                    LoadRootKeys();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка");
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void CopyValueName(RegistryValueItem? item)
        {
            if (item != null)
            {
                Clipboard.SetText(item.Name);
                HistoryService.Log("Скопировано имя значения", item.Name, "Registry");
            }
        }

        private void CopyValueData(RegistryValueItem? item)
        {
            if (item != null)
            {
                Clipboard.SetText(item.ValueDisplay);
                HistoryService.Log("Скопированы данные значения", item.ValueDisplay, "Registry");
            }
        }

        private void CopySearchResultPath(RegistrySearchResult? result)
        {
            if (result != null)
            {
                Clipboard.SetText(result.FullPath);
                HistoryService.Log("Скопирован путь из поиска", result.FullPath, "Registry");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}