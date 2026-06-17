using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class NotepadViewModel : ViewModelBase
    {
        private string _title = "Блокнот - Новый файл";
        private string _content = "";
        private string _currentFilePath = "";
        private string _statusInfo = "Готов";
        private bool _isModified;

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Content { get => _content; set { _content = value; _isModified = true; OnPropertyChanged(); UpdateTitle(); } }
        public string StatusInfo { get => _statusInfo; set => SetProperty(ref _statusInfo, value); }
        public string CurrentFilePath { get => _currentFilePath; set => SetProperty(ref _currentFilePath, value); }

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
                CurrentFilePath = path;
                _isModified = false;
                UpdateTitle();
                StatusInfo = $"Загружен: {path}";
            }
            catch (Exception ex) { StatusInfo = $"Ошибка: {ex.Message}"; }
        }

        private void NewFile()
        {
            Content = ""; CurrentFilePath = ""; _isModified = false;
            StatusInfo = "Новый файл";
            UpdateTitle();
        }

        private void OpenFile()
        {
            var dlg = new OpenFileDialog { Filter = "Текстовые файлы|*.txt;*.log;*.cs;*.xml;*.json;*.md|Все файлы|*.*" };
            if (dlg.ShowDialog() == true) LoadFile(dlg.FileName);
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(CurrentFilePath)) SaveFileAs();
            else
            {
                try { File.WriteAllText(CurrentFilePath, Content); _isModified = false; UpdateTitle(); StatusInfo = $"Сохранён: {CurrentFilePath}"; }
                catch (Exception ex) { StatusInfo = $"Ошибка: {ex.Message}"; }
            }
        }

        private void SaveFileAs()
        {
            var dlg = new SaveFileDialog { Filter = "Текстовые файлы|*.txt|Все файлы|*.*", DefaultExt = ".txt" };
            if (dlg.ShowDialog() == true)
            {
                try { File.WriteAllText(dlg.FileName, Content); CurrentFilePath = dlg.FileName; _isModified = false; UpdateTitle(); StatusInfo = $"Сохранён: {dlg.FileName}"; }
                catch (Exception ex) { StatusInfo = $"Ошибка: {ex.Message}"; }
            }
        }

        private void UpdateTitle()
        {
            var name = string.IsNullOrEmpty(CurrentFilePath) ? "Новый файл" : Path.GetFileName(CurrentFilePath);
            Title = $"Блокнот - {name}{(_isModified ? " *" : "")}";
        }
    }
}
