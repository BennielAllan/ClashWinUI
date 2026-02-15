namespace ClashWinUI;

/// <summary>
/// Simple localized strings for 中文 / English.
/// </summary>
public static class Strings
{
    private static bool IsZh => Helpers.AppSettings.Language == "zh";

    public static string Settings_Title => IsZh ? "设置" : "Settings";
    public static string Settings_Appearance => IsZh ? "外观与行为" : "Appearance & behavior";
    public static string Settings_AppTheme => IsZh ? "应用主题" : "App theme";
    public static string Settings_AppTheme_Description => IsZh ? "选择要显示的应用主题" : "Select which app theme to display";
    public static string Theme_Light => IsZh ? "浅色" : "Light";
    public static string Theme_Dark => IsZh ? "深色" : "Dark";
    public static string Theme_System => IsZh ? "跟随系统" : "Use system setting";
    public static string Settings_Language => IsZh ? "语言" : "Language";
    public static string Settings_Language_Description => IsZh ? "选择界面语言" : "Choose display language";
    public static string Settings_About => IsZh ? "关于" : "About";
    public static string Settings_About_Description => IsZh ? "ClashWinUI - WinUI 3 版本" : "ClashWinUI - WinUI 3 version";

    // Navigation sidebar
    public static string Nav_Home => IsZh ? "首页" : "Home";
    public static string Nav_Proxy => IsZh ? "代理" : "Proxy";
    public static string Nav_Subscription => IsZh ? "订阅" : "Subscription";
    public static string Nav_Connections => IsZh ? "连接" : "Connections";
    public static string Nav_Rules => IsZh ? "规则" : "Rules";
    public static string Nav_Logs => IsZh ? "日志" : "Logs";
    public static string Nav_Test => IsZh ? "测试" : "Test";
    public static string Nav_Settings => IsZh ? "设置" : "Settings";
}
