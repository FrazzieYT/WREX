using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class QuickCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ConsoleViewModel : INotifyPropertyChanged, IDisposable
    {
        private Process? _process;
        private string _currentInput = "";
        private string _output = "";
        private bool _isCmd = true;
        private bool _disposed = false;

        public string CurrentInput
        {
            get => _currentInput;
            set { _currentInput = value; OnPropertyChanged(); }
        }

        public string Output
        {
            get => _output;
            set { _output = value; OnPropertyChanged(); }
        }

        public bool IsCmd
        {
            get => _isCmd;
            set
            {
                _isCmd = value;
                OnPropertyChanged();
                RestartShell();
            }
        }

        public string ShellName => _isCmd ? "CMD" : "PowerShell";

        public ObservableCollection<QuickCommand> QuickCommands { get; }

        public ICommand ExecuteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand SaveOutputCommand { get; }
        public ICommand InsertCommandCommand { get; }

        public ConsoleViewModel()
        {
            ExecuteCommand = new RelayCommand(_ => Execute(), _ => !string.IsNullOrWhiteSpace(CurrentInput));
            ClearCommand = new RelayCommand(_ => Clear());
            RestartCommand = new RelayCommand(_ => RestartShell());
            SaveOutputCommand = new RelayCommand(_ => SaveOutput());
            InsertCommandCommand = new RelayCommand(InsertCommand);

            bool isWinRE = RegistryService.IsWinRE();
            string offlineWindowsPath = RegistryService.DetectOfflineWindowsPath();
            
            // Формируем пути для offline-команд в WinRE
            string offBootDir = isWinRE && !string.IsNullOrEmpty(offlineWindowsPath) 
                ? (System.IO.Path.GetPathRoot(offlineWindowsPath)?.TrimEnd('\\') ?? "") + "\\" 
                : "";
            string offWinDir = isWinRE ? offlineWindowsPath ?? "" : "";

            QuickCommands = new ObservableCollection<QuickCommand>
            {
                // Диагностика системы (адаптировано для WinRE)
                new QuickCommand { 
                    Name = "SFC Scan", 
                    Command = isWinRE ? $"sfc /scannow /offbootdir={offBootDir} /offwindir={offWinDir}" : "sfc /scannow", 
                    Category = "Диагностика" 
                },
                new QuickCommand { 
                    Name = "DISM Restore", 
                    Command = isWinRE ? $"DISM /Offline /Image={offWinDir} /Cleanup-Image /RestoreHealth" : "DISM /Online /Cleanup-Image /RestoreHealth", 
                    Category = "Диагностика" 
                },
                new QuickCommand { Name = "Check Disk", Command = "chkdsk C: /f /r", Category = "Диагностика" },
                new QuickCommand { Name = "System Info", Command = "systeminfo", Category = "Диагностика" },
                
                // Сеть
                new QuickCommand { Name = "Flush DNS", Command = "ipconfig /flushdns", Category = "Сеть" },
                new QuickCommand { Name = "Reset Winsock", Command = "netsh winsock reset", Category = "Сеть" },
                new QuickCommand { Name = "Reset IP", Command = "netsh int ip reset", Category = "Сеть" },
                new QuickCommand { Name = "IP Config", Command = "ipconfig /all", Category = "Сеть" },
                new QuickCommand { Name = "Netstat", Command = "netstat -ano", Category = "Сеть" },
                
                // Загрузка
                new QuickCommand { Name = "Safe Mode ON", Command = "bcdedit /set {default} safeboot minimal", Category = "Загрузка" },
                new QuickCommand { Name = "Safe Mode OFF", Command = "bcdedit /deletevalue {default} safeboot", Category = "Загрузка" },
                new QuickCommand { Name = "Fix MBR", Command = "bootrec /fixmbr", Category = "Загрузка" },
                new QuickCommand { Name = "Fix Boot", Command = "bootrec /fixboot", Category = "Загрузка" },
                new QuickCommand { Name = "Rebuild BCD", Command = "bootrec /rebuildbcd", Category = "Загрузка" },
                
                // Процессы
                new QuickCommand { Name = "Task List", Command = "tasklist", Category = "Процессы" },
            };

            // Команды explorer.exe добавляем только в обычной ОС
            if (!isWinRE)
            {
                QuickCommands.Add(new QuickCommand { Name = "Kill Explorer", Command = "taskkill /f /im explorer.exe", Category = "Процессы" });
                QuickCommands.Add(new QuickCommand { Name = "Start Explorer", Command = "start explorer.exe", Category = "Процессы" });
            }

            // Система
            QuickCommands.Add(new QuickCommand { Name = "Shutdown", Command = "shutdown /s /t 0", Category = "Система" });
            QuickCommands.Add(new QuickCommand { Name = "Restart", Command = "shutdown /r /t 0", Category = "Система" });
            QuickCommands.Add(new QuickCommand { Name = "Recovery Menu", Command = "shutdown /r /o /t 0", Category = "Система" });

            StartShell();
        }

        private void InsertCommand(object? parameter)
        {
            if (parameter is string cmd)
            {
                CurrentInput = cmd;
            }
        }

        private void StartShell()
        {
            try
            {
                _process = new Process();
                _process.StartInfo.FileName = _isCmd ? "cmd.exe" : "powershell.exe";
                _process.StartInfo.Arguments = _isCmd ? "/q" : "-NoExit -NoLogo";
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.CreateNoWindow = true;
                
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                _process.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding(866);
                _process.StartInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding(866);
        
                _process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) AppendOutput(e.Data);
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null) AppendOutput($"[ОШИБКА] {e.Data}");
                };
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                AppendOutput($"=== {ShellName} запущен ===");
            }
            catch (Exception ex)
            {
                AppendOutput($"Ошибка запуска: {ex.Message}");
            }
        }

        private void Execute()
        {
            if (_process == null || _process.HasExited)
                StartShell();

            var cmd = CurrentInput.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            AppendOutput($"> {cmd}");
            HistoryService.Log("Выполнена команда", $"{ShellName}: {cmd}", "Console");

            try
            {
                _process!.StandardInput.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                AppendOutput($"Ошибка: {ex.Message}");
            }
            CurrentInput = "";
        }

        private void AppendOutput(string text)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                Output += text + Environment.NewLine;
            });
        }

        private void Clear() => Output = "";

        private void RestartShell()
        {
            KillProcess();
            Output = "";
            StartShell();
        }

        private void SaveOutput()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = "txt",
                FileName = Path.Combine(AppPaths.ExportDirectory, $"console_output_{DateTime.Now:yyyyMMdd_HHmmss}.txt")
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, Output, System.Text.Encoding.UTF8);
                    HistoryService.Log("Сохранён вывод консоли", $"Файл: {dialog.FileName}", "Console");
                    AppendOutput($"[СИСТЕМА] Вывод успешно сохранён в {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    AppendOutput($"[ОШИБКА] Не удалось сохранить файл: {ex.Message}");
                }
            }
        }

        private void KillProcess()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(1000);
                }
            }
            catch { }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                KillProcess();
                _disposed = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)  
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}