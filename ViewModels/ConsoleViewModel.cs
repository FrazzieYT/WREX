using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using SystemManager.Services;

namespace SystemManager.ViewModels
{
    public class ConsoleViewModel : ViewModelBase, IDisposable
    {
        private Process? _process;
        private string _currentInput = "";
        private string _output = "";
        private bool _isCmd = true;
        private bool _disposed;

        public string CurrentInput { get => _currentInput; set => SetProperty(ref _currentInput, value); }
        public string Output { get => _output; set => SetProperty(ref _output, value); }
        public bool IsCmd { get => _isCmd; set { if (SetProperty(ref _isCmd, value)) RestartShell(); } }
        public ObservableCollection<string> History { get; } = new();
        public ObservableCollection<QuickCommand> QuickCommands { get; } = new();

        public ICommand ExecuteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand SaveOutputCommand { get; }
        public ICommand InsertCommandCommand { get; }

        public ConsoleViewModel()
        {
            ExecuteCommand = new RelayCommand(_ => Execute());
            ClearCommand = new RelayCommand(_ => Output = "");
            RestartCommand = new RelayCommand(_ => RestartShell());
            SaveOutputCommand = new RelayCommand(_ => SaveOutput());
            InsertCommandCommand = new RelayCommand(param => { if (param is string cmd) CurrentInput = cmd; });

            InitQuickCommands();
            StartShell();
        }

        private void InitQuickCommands()
        {
            QuickCommands.Add(new QuickCommand { Name = "ipconfig", Command = "ipconfig /all", Category = "Сеть" });
            QuickCommands.Add(new QuickCommand { Name = "ping", Command = "ping 8.8.8.8", Category = "Сеть" });
            QuickCommands.Add(new QuickCommand { Name = "systeminfo", Command = "systeminfo", Category = "Система" });
            QuickCommands.Add(new QuickCommand { Name = "tasklist", Command = "tasklist", Category = "Система" });
            QuickCommands.Add(new QuickCommand { Name = "sfc /scannow", Command = "sfc /scannow", Category = "Ремонт" });
            QuickCommands.Add(new QuickCommand { Name = "DISM", Command = "DISM /Online /Cleanup-Image /RestoreHealth", Category = "Ремонт" });
            QuickCommands.Add(new QuickCommand { Name = "chkdsk", Command = "chkdsk C: /f /r", Category = "Диски" });
            QuickCommands.Add(new QuickCommand { Name = "driverquery", Command = "driverquery /v", Category = "Система" });
            QuickCommands.Add(new QuickCommand { Name = "netstat", Command = "netstat -ano", Category = "Сеть" });
            QuickCommands.Add(new QuickCommand { Name = "hostname", Command = "hostname", Category = "Система" });
        }

        private void StartShell()
        {
            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = IsCmd ? "cmd.exe" : "powershell.exe",
                        Arguments = IsCmd ? "/k chcp 65001" : "-NoLogo -NoProfile",
                        UseShellExecute = false, RedirectStandardInput = true,
                        RedirectStandardOutput = true, RedirectStandardError = true,
                        CreateNoWindow = true, StandardOutputEncoding = System.Text.Encoding.UTF8
                    },
                    EnableRaisingEvents = true
                };
                _process.OutputDataReceived += (_, e) => { if (e.Data != null) AppendOutput(e.Data); };
                _process.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendOutput(e.Data); };
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex) { AppendOutput($"Ошибка: {ex.Message}"); }
        }

        private void Execute()
        {
            if (string.IsNullOrWhiteSpace(CurrentInput) || _process == null) return;
            try
            {
                History.Add(CurrentInput);
                _process.StandardInput.WriteLine(CurrentInput);
                _process.StandardInput.Flush();
                CurrentInput = "";
            }
            catch (Exception ex) { AppendOutput($"Ошибка: {ex.Message}"); }
        }

        private void RestartShell()
        {
            try { _process?.Kill(); } catch { }
            _process?.Dispose(); _process = null;
            StartShell();
        }

        private void AppendOutput(string text)
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
            {
                Output += text + "\n";
                if (Output.Length > 50000) Output = Output[^20000..];
            });
        }

        private void SaveOutput()
        {
            try
            {
                var path = Path.Combine(AppPaths.ExportDirectory, $"console_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, Output);
                System.Windows.MessageBox.Show($"Сохранено: {path}", "Готово");
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try { _process?.Kill(); } catch { }
            _process?.Dispose(); _disposed = true;
        }
    }
}
