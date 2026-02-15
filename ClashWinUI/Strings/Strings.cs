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

    // Subscription page
    public static string Subscription_Import => IsZh ? "导入" : "Import";
    public static string Subscription_New => IsZh ? "新建" : "New";
    public static string Subscription_Open => IsZh ? "打开" : "Open";
    public static string Subscription_List => IsZh ? "订阅列表" : "Subscription list";
    public static string Subscription_ImportFromUrl => IsZh ? "从 URL 导入" : "Import from URL";
    public static string Subscription_ImportFromFile => IsZh ? "从文件导入" : "Import from file";
    public static string Subscription_NewProfile => IsZh ? "新建配置" : "New profile";
    public static string Subscription_OpenFile => IsZh ? "打开文件" : "Open file";
    public static string Subscription_UrlPlaceholder => IsZh ? "输入订阅或配置 URL…" : "Enter subscription or config URL...";
    public static string Subscription_Name => IsZh ? "名称" : "Name";
    public static string Subscription_UrlOrPath => IsZh ? "URL / 路径" : "URL / Path";
    public static string Subscription_Updated => IsZh ? "更新时间" : "Updated";
    public static string Subscription_TypeRemote => IsZh ? "远程" : "Remote";
    public static string Subscription_TypeLocal => IsZh ? "本地" : "Local";
    public static string Subscription_Delete => IsZh ? "删除" : "Delete";
    public static string Subscription_Update => IsZh ? "更新" : "Update";
    public static string Subscription_NoItems => IsZh ? "暂无订阅，点击「导入」或「新建」添加。" : "No subscriptions. Use Import or New to add.";
    public static string Subscription_Refresh => IsZh ? "刷新" : "Refresh";
    public static string Subscription_RefreshedNever => IsZh ? "从未刷新" : "Never refreshed";
    public static string Subscription_UsageTotal => IsZh ? "用量 / 总量" : "Usage / Total";
    public static string Common_Cancel => IsZh ? "取消" : "Cancel";
    public static string Common_Ok => IsZh ? "确定" : "OK";
    public static string Subscription_SelectFirst => IsZh ? "请先选择一项订阅。" : "Please select a subscription first.";
    public static string Nav_Connections => IsZh ? "连接" : "Connections";
    public static string Nav_Rules => IsZh ? "规则" : "Rules";
    public static string Nav_Logs => IsZh ? "日志" : "Logs";
    public static string Nav_Test => IsZh ? "测试" : "Test";
    public static string Nav_Settings => IsZh ? "设置" : "Settings";

    // App & shell
    public static string App_Title => "ClashWinUI";
    public static string Search_Placeholder => IsZh ? "搜索…" : "Search...";
    public static string About_AppName => "ClashWinUI";

    // Language display names (for language selector)
    public static string Language_English => "English";
    public static string Language_Chinese => "中文";
}
