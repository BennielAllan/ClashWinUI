using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ClashWinUI.Helpers;

/// <summary>
/// Reads and writes the Windows system HTTP proxy setting via the registry.
/// Changes are propagated to running processes via WinInet notification.
/// </summary>
internal static class SystemProxyHelper
{
    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(
        nint hInternet, int dwOption, nint lpBuffer, int dwBufferLength);

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;
    private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue("ProxyEnable") is int v && v == 1;
        }
        catch { return false; }
    }

    public static void Enable(int port)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
            if (key == null) return;
            key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
            key.SetValue("ProxyServer", $"127.0.0.1:{port}", RegistryValueKind.String);
            Notify();
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
            if (key == null) return;
            key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
            Notify();
        }
        catch { }
    }

    private static void Notify()
    {
        InternetSetOption(nint.Zero, INTERNET_OPTION_SETTINGS_CHANGED, nint.Zero, 0);
        InternetSetOption(nint.Zero, INTERNET_OPTION_REFRESH, nint.Zero, 0);
    }
}
