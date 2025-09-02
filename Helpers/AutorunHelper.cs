using Microsoft.Win32;

public static class AutorunHelper
{
    public static void EnableAutorun(string appName, string exePath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", true);

        key?.SetValue(appName, $"\"{exePath}\"");
    }

    public static void DisableAutorun(string appName)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", true);

        if (key?.GetValue(appName) != null)
            key.DeleteValue(appName);
    }

    public static bool IsAutorunEnabled(string appName)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", false);

        return key?.GetValue(appName) != null;
    }

}