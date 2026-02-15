using System;
using System.Diagnostics;

namespace ClashWinUI.Helpers;

public static partial class ProcessInfoHelper
{
    private static readonly FileVersionInfo? FileVersionInfo =
        Process.GetCurrentProcess().MainModule?.FileVersionInfo;

    public static string Version => GetVersion()?.ToString() ?? string.Empty;
    public static string ProductName => FileVersionInfo?.ProductName ?? "ClashWinUI";
    public static Version? GetVersion()
    {
        return FileVersionInfo is null
            ? null
            : new Version(
                FileVersionInfo.FileMajorPart,
                FileVersionInfo.FileMinorPart,
                FileVersionInfo.FileBuildPart,
                FileVersionInfo.FilePrivatePart);
    }
}
