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
    public static string Subscription_DeleteConfirm => IsZh ? "确定删除该订阅？" : "Delete this subscription?";
    public static string Subscription_EditInfo => IsZh ? "编辑信息" : "Edit info";
    public static string Subscription_More => IsZh ? "更多" : "More";
    public static string Subscription_UpdateIntervalMinutes => IsZh ? "更新间隔（分钟）" : "Update interval (minutes)";
    public static string Subscription_Update => IsZh ? "更新" : "Update";
    public static string Subscription_NoItems => IsZh ? "使用「导入」或「新建」添加配置文件。" : "Use Import or New to add a profile.";
    public static string Subscription_EmptyTitle => IsZh ? "暂无订阅" : "No subscriptions yet";
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

    // Proxy page
    public static string Proxy_Mode => IsZh ? "代理模式" : "Mode";
    public static string Proxy_Mode_Rule => IsZh ? "规则" : "Rule";
    public static string Proxy_Mode_Global => IsZh ? "全局" : "Global";
    public static string Proxy_Mode_Direct => IsZh ? "直连" : "Direct";
    public static string Proxy_TestAll => IsZh ? "全部测速" : "Test All";
    public static string Proxy_TestGroup => IsZh ? "测速" : "Test";
    public static string Proxy_Latency => IsZh ? "延迟" : "Latency";
    public static string Proxy_NotRunning => IsZh ? "内核未运行，请先在订阅页面启动内核。" : "Core is not running. Please start the core from the Subscription page.";
    public static string Proxy_Loading => IsZh ? "正在加载代理组…" : "Loading proxy groups…";
    public static string Proxy_NoGroups => IsZh ? "没有可用的代理组" : "No proxy groups available";
    public static string Proxy_SelectNode => IsZh ? "选择节点" : "Select node";

    // Logs page
    public static string Logs_Level_All => IsZh ? "全部" : "All";
    public static string Logs_Level_Debug => "Debug";
    public static string Logs_Level_Info => "Info";
    public static string Logs_Level_Warn => "Warn";
    public static string Logs_Level_Error => "Error";
    public static string Logs_Pause => IsZh ? "暂停" : "Pause";
    public static string Logs_Resume => IsZh ? "继续" : "Resume";
    public static string Logs_Clear => IsZh ? "清空" : "Clear";
    public static string Logs_Search => IsZh ? "搜索日志…" : "Search logs…";
    public static string Logs_NotRunning => IsZh ? "内核未运行" : "Core is not running";
    public static string Logs_Empty => IsZh ? "暂无日志" : "No logs yet";

    // Connections page
    public static string Connections_Total_Down => IsZh ? "总下载" : "Total Down";
    public static string Connections_Total_Up => IsZh ? "总上传" : "Total Up";
    public static string Connections_CloseAll => IsZh ? "关闭全部" : "Close All";
    public static string Connections_CloseAllConfirm => IsZh ? "确定关闭全部连接？" : "Close all connections?";
    public static string Connections_Search => IsZh ? "搜索连接…" : "Search connections…";
    public static string Connections_Empty => IsZh ? "暂无活跃连接" : "No active connections";
    public static string Connections_NotRunning => IsZh ? "内核未运行" : "Core is not running";
    public static string Connections_Rule => IsZh ? "规则" : "Rule";
    public static string Connections_Chain => IsZh ? "链路" : "Chain";

    // Core / Mihomo
    public static string Core_Start => IsZh ? "启动内核" : "Start Core";
    public static string Core_Stop => IsZh ? "停止内核" : "Stop Core";
    public static string Core_Running => IsZh ? "运行中" : "Running";
    public static string Core_Stopped => IsZh ? "未运行" : "Stopped";
    public static string Core_StartFailed => IsZh ? "内核启动失败" : "Failed to start core";
    public static string Subscription_NoCacheHint => IsZh
        ? "订阅尚未下载本地缓存，请先点击刷新按钮下载内容，再启动内核。"
        : "Subscription has no local cache. Please refresh it first, then start the core.";

    // Home page
    public static string Home_ActiveSubscription => IsZh ? "当前订阅" : "Active Subscription";
    public static string Home_NetworkMode => IsZh ? "网络模式" : "Network Mode";
    public static string Home_SystemProxy => IsZh ? "系统代理" : "System Proxy";
    public static string Home_SystemProxy_Description => IsZh ? "将系统代理指向 mihomo 入站端口" : "Route system proxy through mihomo inbound port";
    public static string Home_TunMode => IsZh ? "虚拟网卡 (TUN)" : "TUN Mode";
    public static string Home_TunMode_Description => IsZh ? "使用虚拟网卡接管所有流量" : "Capture all traffic via virtual TUN adapter";
    public static string Home_ProxyMode => IsZh ? "代理模式" : "Proxy Mode";
    public static string Home_CurrentNode => IsZh ? "当前节点" : "Current Node";
    public static string Home_NoSubscription => IsZh ? "无可用订阅" : "No subscription available";

    // Common
    public static string Common_Refresh => IsZh ? "刷新" : "Refresh";
    public static string Common_Close => IsZh ? "关闭" : "Close";
    public static string Common_Search => IsZh ? "搜索…" : "Search…";
    public static string Common_Loading => IsZh ? "加载中…" : "Loading…";
}
