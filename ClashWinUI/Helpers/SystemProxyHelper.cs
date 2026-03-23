using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClashWinUI.Helpers;

/// <summary>
/// Reads and writes the Windows system HTTP proxy setting via PowerShell child processes,
/// bypassing MSIX registry virtualisation for both reads and writes.
/// </summary>
internal static class SystemProxyHelper
{
    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(
        nint hInternet, int dwOption, nint lpBuffer, int dwBufferLength);

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH          = 37;

    public static bool IsEnabled()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NonInteractive -WindowStyle Hidden -Command \"(Get-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings').ProxyEnable\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        using var p = Process.Start(psi);
        var output = p?.StandardOutput.ReadToEnd().Trim();
        p?.WaitForExit(3000);
        return output == "1";
    }

    public static void Enable(int port)
    {
        RunPs($@"
$r = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings'
Set-ItemProperty $r ProxyEnable 1
Set-ItemProperty $r ProxyServer '127.0.0.1:{port}'
");
        Notify();
    }

    public static void Disable()
    {
        RunPs(@"
$r = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings'
Set-ItemProperty $r ProxyEnable 0
");
        Notify();
    }

    private static void RunPs(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NonInteractive -WindowStyle Hidden -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(3000);
    }

    private static void Notify()
    {
        InternetSetOption(nint.Zero, INTERNET_OPTION_SETTINGS_CHANGED, nint.Zero, 0);
        InternetSetOption(nint.Zero, INTERNET_OPTION_REFRESH, nint.Zero, 0);
    }
}
