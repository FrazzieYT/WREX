using System.Diagnostics;
using System.Security.Principal;

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
                Process.Start(psi);
            }
            catch { }
        }
    }
}