using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using ClashWinUI.Helpers;
using ClashWinUI.Models;

namespace ClashWinUI.Services;

public sealed class TrayIconService : IDisposable
{
    public static TrayIconService Instance { get; } = new();
    private TrayIconService() { }

    // ── Constants ────────────────────────────────────────────────────────────

    private const uint WM_TRAYICON = 0x8000;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;

    // NIM
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    // Menu
    private const uint MF_CHECKED = 0x00000008;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint MF_POPUP = 0x00000010;
    private const uint MF_GRAYED = 0x00000001;
    private const uint MF_STRING = 0x00000000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    // Menu item IDs
    private const int IDM_SYSTEM_PROXY = 1001;
    private const int IDM_TUN_MODE = 1002;
    private const int IDM_NODES_BASE = 2000;
    private const int IDM_QUIT = 3002;

    // ShowWindow
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    // ── Win32 Types ──────────────────────────────────────────────────────────

    private delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public WNDPROC lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    // ── Win32 P/Invoke ───────────────────────────────────────────────────────

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW wcex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int reserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, uint attrSize);

    private const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    // ── State ────────────────────────────────────────────────────────────────

    private IntPtr _messageHwnd;
    private WNDPROC? _wndProc;
    private IntPtr _trayIcon;
    private bool _disposed;

    private bool _isSystemProxyEnabled;
    private bool _isTunEnabled;
    private int _cachedProxyPort = 7890;
    private readonly List<ProxyGroup> _cachedProxyGroups = new();

    // ── Initialize / Dispose ─────────────────────────────────────────────────

    public void Initialize()
    {
        if (_messageHwnd != IntPtr.Zero) return;

        _wndProc = WndProc;
        _trayIcon = LoadTrayIcon("ClashWinUI.ico");

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = _wndProc,
            lpszClassName = "ClashWinUI_TrayHost",
            hInstance = Marshal.GetHINSTANCE(typeof(TrayIconService).Module)
        };
        RegisterClassExW(ref wc);

        _messageHwnd = CreateWindowExW(
            0, "ClashWinUI_TrayHost", "",
            0, 0, 0, 0, 0,
            (IntPtr)(-3),
            IntPtr.Zero, Marshal.GetHINSTANCE(typeof(TrayIconService).Module), IntPtr.Zero);

        AddTrayIcon();

        MihomoService.Instance.RunningStateChanged += OnCoreStateChanged;
        AppSettings.LanguageChanged += OnLanguageChanged;

        _ = RefreshStateAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        MihomoService.Instance.RunningStateChanged -= OnCoreStateChanged;
        AppSettings.LanguageChanged -= OnLanguageChanged;

        RemoveTrayIcon();
        if (_trayIcon != IntPtr.Zero) DestroyIcon(_trayIcon);
        if (_messageHwnd != IntPtr.Zero) DestroyWindow(_messageHwnd);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void HideMainWindow()
    {
        var win = App.MainWindow;
        if (win == null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        ShowWindow(hwnd, SW_HIDE);
    }

    public void ShowMainWindow()
    {
        var win = App.MainWindow;
        if (win == null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        ShowWindow(hwnd, SW_SHOW);
        SetForegroundWindow(hwnd);
        win.Activate();
    }

    // ── Tray Icon ────────────────────────────────────────────────────────────

    private string GetAssetPath(string filename)
    {
        try
        {
            var pkgPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            return Path.Combine(pkgPath, "Assets", filename);
        }
        catch
        {
            var dir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            return Path.Combine(dir, "Assets", filename);
        }
    }

    private IntPtr LoadTrayIcon(string filename)
    {
        var path = GetAssetPath(filename);
        return LoadImage(IntPtr.Zero, path, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
    }

    private NOTIFYICONDATAW CreateNid()
    {
        return new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _messageHwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _trayIcon,
            szTip = "ClashWinUI"
        };
    }

    private void AddTrayIcon()
    {
        var nid = CreateNid();
        Shell_NotifyIconW(NIM_ADD, ref nid);
        nid.uFlags = NIF_TIP;
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    private void RemoveTrayIcon()
    {
        var nid = CreateNid();
        Shell_NotifyIconW(NIM_DELETE, ref nid);
    }

    // ── Window Procedure ─────────────────────────────────────────────────────

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            var mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
            if (mouseMsg == WM_LBUTTONUP)
            {
                ToggleMainWindow();
                return IntPtr.Zero;
            }
            if (mouseMsg == WM_RBUTTONUP)
            {
                ShowContextMenu(hWnd);
                return IntPtr.Zero;
            }
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    // ── Context Menu (Win32 + dark mode) ────────────────────────────────────

    private void ShowContextMenu(IntPtr hWnd)
    {
        _ = RefreshStateAsync();

        // Apply dark/light mode to menu
        int darkMode = ThemeHelper.IsDarkTheme() ? 1 : 0;
        DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, (uint)Marshal.SizeOf<int>());

        var menu = CreatePopupMenu();

        // System Proxy
        uint sysFlags = MF_STRING | (_isSystemProxyEnabled ? MF_CHECKED : 0);
        AppendMenuW(menu, sysFlags, (IntPtr)IDM_SYSTEM_PROXY, Strings.Tray_SystemProxy);

        // TUN Mode
        uint tunFlags = MF_STRING | (_isTunEnabled ? MF_CHECKED : 0);
        if (!MihomoService.Instance.IsRunning) tunFlags |= MF_GRAYED;
        AppendMenuW(menu, tunFlags, (IntPtr)IDM_TUN_MODE, Strings.Tray_TunMode);

        AppendMenuW(menu, MF_SEPARATOR, 0, "");

        // Node selector
        if (MihomoService.Instance.IsRunning && _cachedProxyGroups.Count > 0)
        {
            var nodeMenu = CreatePopupMenu();
            int menuId = IDM_NODES_BASE;
            foreach (var group in _cachedProxyGroups)
            {
                var subMenu = CreatePopupMenu();
                int startId = menuId;
                foreach (var node in group.Nodes)
                {
                    uint nodeFlags = MF_STRING | (group.Now == node.Name ? MF_CHECKED : 0);
                    AppendMenuW(subMenu, nodeFlags, (IntPtr)menuId++, node.Name);
                }
                AppendMenuW(nodeMenu, MF_POPUP, subMenu, group.Name);
            }
            AppendMenuW(menu, MF_POPUP, nodeMenu, Strings.Tray_SelectNode);
        }

        AppendMenuW(menu, MF_SEPARATOR, 0, "");

        // Quit
        AppendMenuW(menu, MF_STRING, (IntPtr)IDM_QUIT, Strings.Tray_Quit);

        SetForegroundWindow(hWnd);
        GetCursorPos(out var pt);
        uint cmd = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
        DestroyMenu(menu);

        if (cmd != 0)
            HandleMenuCommand((int)cmd);
    }

    // ── Menu Command Handler ─────────────────────────────────────────────────

    private void HandleMenuCommand(int menuId)
    {
        switch (menuId)
        {
            case IDM_SYSTEM_PROXY:
                _ = ToggleSystemProxyAsync();
                break;
            case IDM_TUN_MODE:
                _ = ToggleTunModeAsync();
                break;
            case IDM_QUIT:
                _ = QuitApplicationAsync();
                break;
            default:
                if (menuId >= IDM_NODES_BASE)
                    _ = SelectNodeAsync(menuId);
                break;
        }
    }

    private void ToggleMainWindow()
    {
        var win = App.MainWindow;
        if (win == null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        if (IsWindowVisible(hwnd))
            HideMainWindow();
        else
            ShowMainWindow();
    }

    private async Task ToggleSystemProxyAsync()
    {
        try
        {
            if (_isSystemProxyEnabled)
            {
                await Task.Run(() => SystemProxyHelper.Disable());
                _isSystemProxyEnabled = false;
            }
            else
            {
                await Task.Run(() => SystemProxyHelper.Enable(_cachedProxyPort));
                _isSystemProxyEnabled = true;
            }
        }
        catch { }
    }

    private async Task ToggleTunModeAsync()
    {
        if (!MihomoService.Instance.IsRunning) return;
        try
        {
            bool desired = !_isTunEnabled;
            var (ok, _) = await MihomoService.Instance.SetTunAsync(desired);
            if (ok) _isTunEnabled = desired;
        }
        catch { }
    }

    private async Task SelectNodeAsync(int menuId)
    {
        int index = menuId - IDM_NODES_BASE;
        foreach (var group in _cachedProxyGroups)
        {
            int count = group.Nodes.Count;
            if (index < count)
            {
                try
                {
                    await MihomoService.Instance.SelectProxyAsync(group.Name, group.Nodes[index].Name);
                    group.Now = group.Nodes[index].Name;
                }
                catch { }
                return;
            }
            index -= count;
        }
    }

    private async Task QuitApplicationAsync()
    {
        try
        {
            if (MihomoService.Instance.IsRunning)
                await MihomoService.Instance.StopAsync();
        }
        catch { }

        Dispose();
        Application.Current.Exit();
    }

    // ── State Refresh ────────────────────────────────────────────────────────

    private async Task RefreshStateAsync()
    {
        if (!MihomoService.Instance.IsRunning) return;
        try
        {
            var config = await MihomoService.Instance.GetConfigAsync();
            if (config != null)
            {
                _isTunEnabled = config.Tun?.Enable ?? false;
                _cachedProxyPort = config.MixedPort > 0 ? config.MixedPort
                                   : config.Port > 0 ? config.Port : 7890;
            }
            _isSystemProxyEnabled = await Task.Run(SystemProxyHelper.IsEnabled);
            _cachedProxyGroups.Clear();
            var groups = await MihomoService.Instance.GetProxyGroupsAsync();
            foreach (var g in groups) _cachedProxyGroups.Add(g);
        }
        catch { _cachedProxyGroups.Clear(); }
    }

    private void OnCoreStateChanged(object? _, EventArgs __)
    {
        _ = RefreshStateAsync();
    }

    private void OnLanguageChanged() { }
}
