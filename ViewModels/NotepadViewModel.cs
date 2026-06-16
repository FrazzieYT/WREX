using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class NotepadViewModel : INotifyPropertyChanged
    {
        private string _title = "Блокнот - Новый файл";
        private string _content = "";
        private string _currentFilePath = "";
        private string _statusInfo = "Готов";
        private bool _isModified = false;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                _isModified = true;
                OnPropertyChanged();
                UpdateTitle();
            }
        }

        public string StatusInfo
        {
            get => _statusInfo;
            set { _statusInfo = value; OnPropertyChanged(); }
        }

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }

        public NotepadViewModel()
        {
            NewCommand = new RelayCommand(_ => NewFile());
            OpenCommand = new RelayCommand(_ => OpenFile());
            SaveCommand = new RelayCommand(_ => SaveFile());
            SaveAsCommand = new RelayCommand(_ => SaveFileAs());
        }

        public void LoadFile(string path)
        {
            try
            {
                Content = File.ReadAllText(path);
                _currentFilePath = path;
                _isModified = false;
                UpdateTitle();
                StatusInfo = $"Открыт: {path}";
                HistoryService.Log("Блокнот: открыт файл", path, "Notepad");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка открытия файла: {ex.Message}", "Блокнот");
                StatusInfo = "Ошибка открытия файла";
            }
        }

        private void NewFile()
        {
            if (_isModified)
            {
                var result = System.Windows.MessageBox.Show(
                    "Сохранить изменения перед созданием нового файла?",
                    "Блокнот",
                    System.Windows.MessageBoxButton.YesNoCancel);

                if (result == System.Windows.MessageBoxResult.Cancel) return;
                if (result == System.Windows.MessageBoxResult.Yes) SaveFile();
            }

            Content = "";
            _currentFilePath = "";
            _isModified = false;
            Title = "Блокнот - Новый файл";
            StatusInfo = "Новый файл";
        }

        private void OpenFile()
        {
            if (_isModified)
            {
                var result = System.Windows.MessageBox.Show(
                    "Сохранить изменения перед открытием другого файла?",
                    "Блокнот",
                    System.Windows.MessageBoxButton.YesNoCancel);

                if (result == System.Windows.MessageBoxResult.Cancel) return;
                if (result == System.Windows.MessageBoxResult.Yes) SaveFile();
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Открыть файл"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveFileAs();
                return;
            }

            try
            {
                File.WriteAllText(_currentFilePath, Content);
                _isModified = false;
                UpdateTitle();
                StatusInfo = $"Сохранено: {_currentFilePath}";
                HistoryService.Log("Блокнот: сохранен файл", _currentFilePath, "Notepad");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Блокнот");
                StatusInfo = "Ошибка сохранения";
            }
        }

        private void SaveFileAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Сохранить как"
            };

            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                dialog.FileName = Path.GetFileName(_currentFilePath);
            }

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                SaveFile();
            }
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) 
                ? "Новый файл" 
                : Path.GetFileName(_currentFilePath);
            
            string modified = _isModified ? "*" : "";
            Title = $"Блокнот - {modified}{fileName}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}