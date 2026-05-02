using System.IO;
using Microsoft.Win32;

namespace Sway;

public interface IRegistryService
{
    bool TrySetAutoRun(bool enabled);
}

public class RegistryService : IRegistryService
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "Sway";

    public bool TrySetAutoRun(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (key is null)
                return false;

            if (enabled)
            {
                var exePath = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, "Sway.exe");
                var command = $"\"{exePath}\" {App.AutoStart}";
                key.SetValue(RunValueName, command, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(RunValueName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
