using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class ConsoleViewModel : INotifyPropertyChanged, IDisposable
    {
        private Process? _process;
        private string _currentInput = string.Empty;
        private string _output = string.Empty;
        private bool _isCmd = true;
        private bool _disposed;

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
        
        public ObservableCollection<QuickCommand> QuickCommands { get; } = new();

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
            
            bool isWinRe = RegistryService.IsWinRE();
            string? offlineWindowsPath = RegistryService.DetectOfflineWindowsPath();
            
            string offBootDir = isWinRe && !string.IsNullOrEmpty(offlineWindowsPath) 
                ? (Path.GetPathRoot(offlineWindowsPath)?.TrimEnd('\\') ?? string.Empty) + "\\" 
                : string.Empty;
            string offWinDir = isWinRe ? offlineWindowsPath ?? string.Empty : string.Empty;
            
            QuickCommands = new ObservableCollection<QuickCommand>
            {
                new() { Name = "SFC Scan", Command = isWinRe ? $"sfc /scannow /offbootdir={offBootDir} /offwindir={offWinDir}" : "sfc /scannow", Category = "Диагностика" },
                new() { Name = "DISM Restore", Command = isWinRe ? $"DISM /Offline /Image={offWinDir} /Cleanup-Image /RestoreHealth" : "DISM /Online /Cleanup-Image /RestoreHealth", Category = "Диагностика" },
                
                new() { Name = "Check Disk", Command = "chkdsk C: /f /r", Category = "Диагностика" },
                new() { Name = "System Info", Command = "systeminfo", Category = "Диагностика" },
                
                new() { Name = "Flush DNS", Command = "ipconfig /flushdns", Category = "Сеть" },
                new() { Name = "Reset Winsock", Command = "netsh winsock reset", Category = "Сеть" },
                new() { Name = "Reset IP", Command = "netsh int ip reset", Category = "Сеть" },
                new() { Name = "IP Config", Command = "ipconfig /all", Category = "Сеть" },
                new() { Name = "Netstat", Command = "netstat -ano", Category = "Сеть" },
                
                new() { Name = "Safe Mode ON", Command = "bcdedit /set {default} safeboot minimal", Category = "Загрузка" },
                new() { Name = "Safe Mode OFF", Command = "bcdedit /deletevalue {default} safeboot", Category = "Загрузка" },
                new() { Name = "Fix MBR", Command = "bootrec /fixmbr", Category = "Загрузка" },
                new() { Name = "Fix Boot", Command = "bootrec /fixboot", Category = "Загрузка" },
                new() { Name = "Rebuild BCD", Command = "bootrec /rebuildbcd", Category = "Загрузка" },
                
                new() { Name = "Task List", Command = "tasklist", Category = "Процессы" },
            };

            if (!isWinRe)
            {
                QuickCommands.Add(new() { Name = "Kill Explorer", Command = "taskkill /f /im explorer.exe", Category = "Процессы" });
                QuickCommands.Add(new() { Name = "Start Explorer", Command = "start explorer.exe", Category = "Процессы" });
            }

            QuickCommands.Add(new() { Name = "Shutdown", Command = "shutdown /s /t 0", Category = "Система" });
            QuickCommands.Add(new() { Name = "Restart", Command = "shutdown /r /t 0", Category = "Система" });
            QuickCommands.Add(new() { Name = "Recovery Menu", Command = "shutdown /r /o /t 0", Category = "Система" });

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
                _process = new Process
                {
                    StartInfo =
                    {
                        FileName = _isCmd ? "cmd.exe" : "powershell.exe",
                        Arguments = _isCmd ? "/q" : "-NoExit -NoLogo",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.GetEncoding(866),
                        StandardErrorEncoding = System.Text.Encoding.GetEncoding(866)
                    }
                };
                
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                
                _process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null) AppendOutput(e.Data);
                };
                _process.ErrorDataReceived += (_, e) =>
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
            if (_process is null || _process.HasExited)
            {
                StartShell();
            }

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
            CurrentInput = string.Empty;
        }

        private void AppendOutput(string text)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                Output += text + Environment.NewLine;
            });
        }

        private void Clear() => Output = string.Empty;

        private void RestartShell()
        {
            KillProcess();
            Output = string.Empty;
            StartShell();
        }

        private void SaveOutput()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
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
                if (_process is { HasExited: false })
                {
                    _process.Kill();
                    _process.WaitForExit(1000);
                }
            }
            catch
            {
                // Игнорируем
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            KillProcess();
            _disposed = true;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? name = null)   
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}