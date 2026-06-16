using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;

namespace SystemManager.Services
{
    public static class SecurityHelper
    {
        public static bool IsAdministrator()
        {
#pragma warning disable CA1416
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416
        }
    }

    public static class SystemLauncher
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        public static void Launch(string path, string arguments = "")
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = arguments,
                    UseShellExecute = true
                };

                var process = Process.Start(psi);

                if (process != null)
                {
                    ThreadPool.QueueUserWorkItem(_ => BringProcessToFront(process));
                }
            }
            catch { }
        }

        private static void BringProcessToFront(Process process)
        {
            try
            {
                for (int i = 0; i < 50; i++)
                {
                    try
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                            break;
                    }
                    catch { }
                    Thread.Sleep(100);
                }

                process.Refresh();
                IntPtr hWnd = process.MainWindowHandle;
                
                if (hWnd == IntPtr.Zero)
                {
                    uint pid = (uint)process.Id;
                    EnumWindows((h, _) =>
                    {
                        GetWindowThreadProcessId(h, out uint windowPid);
                        if (windowPid == pid && IsWindowVisible(h))
                        {
                            hWnd = h;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                }

                if (hWnd != IntPtr.Zero)
                {
                    var app = Application.Current;
                    bool isMainTopmost = false;
                    
                    if (app != null && app.MainWindow != null)
                    {
                        app.Dispatcher.Invoke(() =>
                        {
                            isMainTopmost = app.MainWindow.Topmost;
                        });
                    }

                    if (isMainTopmost)
                    {
                        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }
                    else
                    {
                        var foreGroundHwnd = GetForegroundWindow();
                        uint foreGroundThreadId = GetWindowThreadProcessId(foreGroundHwnd, out _);
                        uint currentThreadId = GetWindowThreadProcessId(hWnd, out _);

                        if (foreGroundThreadId != 0 && foreGroundThreadId != currentThreadId)
                        {
                            AttachThreadInput(currentThreadId, foreGroundThreadId, true);
                            SetForegroundWindow(hWnd);
                            AttachThreadInput(currentThreadId, foreGroundThreadId, false);
                        }
                        else
                        {
                            SetForegroundWindow(hWnd);
                        }
                    }
                }
            }
            catch { }
        }
    }
}