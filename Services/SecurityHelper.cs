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
}