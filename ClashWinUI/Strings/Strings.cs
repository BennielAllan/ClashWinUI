using System.Collections.Generic;

namespace ClashWinUI;

/// <summary>
/// Localized strings for 中文 / English.
/// All translations are defined in the dictionary below;
/// accessor properties delegate via nameof for compile-time safety.
/// </summary>
public static class Strings
{
    private static readonly Dictionary<string, (string Zh, string En)> _ = new()
    {
        // App & Shell
        ["App_Title"]                     = ("ClashWinUI",                    "ClashWinUI"),
        ["Search_Placeholder"]            = ("搜索…",                         "Search..."),
        ["About_AppName"]                 = ("ClashWinUI",                    "ClashWinUI"),
        ["Language_English"]              = ("English",                       "English"),
        ["Language_Chinese"]              = ("中文",                           "中文"),

        // Navigation
        ["Nav_Home"]                      = ("首页",                          "Home"),
        ["Nav_Proxy"]                     = ("节点",                          "Nodes"),
        ["Nav_Subscription"]              = ("订阅",                          "Subscription"),
        ["Nav_Connections"]               = ("连接",                          "Connections"),
        ["Nav_Logs"]                      = ("日志",                          "Logs"),
        ["Nav_Settings"]                  = ("设置",                          "Settings"),

        // Settings
        ["Settings_Title"]                = ("设置",                          "Settings"),
        ["Settings_Appearance"]           = ("外观与行为",                    "Appearance & behavior"),
        ["Settings_AppTheme"]             = ("应用主题",                      "App theme"),
        ["Settings_AppTheme_Description"] = ("选择要显示的应用主题",          "Select which app theme to display"),
        ["Theme_Light"]                   = ("浅色",                          "Light"),
        ["Theme_Dark"]                    = ("深色",                          "Dark"),
        ["Theme_System"]                  = ("跟随系统",                      "Use system setting"),
        ["Settings_Language"]             = ("语言",                          "Language"),
        ["Settings_Language_Description"] = ("选择界面语言",                  "Choose display language"),
        ["Settings_About"]                = ("关于",                          "About"),
        ["Settings_About_Description"]    = ("基于 MIT 协议开源",             "Open source under MIT License"),
        ["About_GitHub"]                  = ("GitHub",                        "GitHub"),

        // Subscription
        ["Subscription_Import"]           = ("导入",                          "Import"),
        ["Subscription_New"]              = ("新建",                          "New"),
        ["Subscription_Open"]             = ("打开",                          "Open"),
        ["Subscription_List"]             = ("订阅列表",                      "Subscription list"),
        ["Subscription_ImportFromUrl"]    = ("从 URL 导入",                   "Import from URL"),
        ["Subscription_ImportFromFile"]   = ("从文件导入",                    "Import from file"),
        ["Subscription_NewProfile"]       = ("新建配置",                      "New profile"),
        ["Subscription_OpenFile"]         = ("打开文件",                      "Open file"),
        ["Subscription_UrlPlaceholder"]   = ("输入订阅或配置 URL…",          "Enter subscription or config URL..."),
        ["Subscription_Name"]             = ("名称",                          "Name"),
        ["Subscription_UrlOrPath"]        = ("URL / 路径",                    "URL / Path"),
        ["Subscription_Updated"]          = ("更新时间",                      "Updated"),
        ["Subscription_TypeRemote"]       = ("远程",                          "Remote"),
        ["Subscription_TypeLocal"]        = ("本地",                          "Local"),
        ["Subscription_Delete"]           = ("删除",                          "Delete"),
        ["Subscription_DeleteConfirm"]    = ("确定删除该订阅？",              "Delete this subscription?"),
        ["Subscription_EditInfo"]         = ("编辑信息",                      "Edit info"),
        ["Subscription_More"]             = ("更多",                          "More"),
        ["Subscription_UpdateIntervalMinutes"] = ("更新间隔（分钟）",         "Update interval (minutes)"),
        ["Subscription_Update"]           = ("更新",                          "Update"),
        ["Subscription_NoItems"]          = ("使用「导入」或「新建」添加配置文件。", "Use Import or New to add a profile."),
        ["Subscription_EmptyTitle"]       = ("暂无订阅",                      "No subscriptions yet"),
        ["Subscription_Refresh"]          = ("刷新",                          "Refresh"),
        ["Subscription_RefreshedNever"]   = ("从未刷新",                      "Never refreshed"),
        ["Subscription_UsageTotal"]       = ("用量 / 总量",                   "Usage / Total"),
        ["Subscription_SelectFirst"]      = ("请先选择一项订阅。",            "Please select a subscription first."),
        ["Subscription_NoCacheHint"]      = ("订阅尚未下载本地缓存，请先点击刷新按钮下载内容，再启动内核。",
                                              "Subscription has no local cache. Please refresh it first, then start the core."),

        // Common
        ["Common_Cancel"]                 = ("取消",                          "Cancel"),
        ["Common_Ok"]                     = ("确定",                          "OK"),
        ["Common_Refresh"]                = ("刷新",                          "Refresh"),
        ["Common_Close"]                  = ("关闭",                          "Close"),
        ["Common_Search"]                 = ("搜索…",                         "Search…"),
        ["Common_Loading"]                = ("加载中…",                       "Loading…"),

        // Proxy
        ["Proxy_Mode"]                    = ("代理模式",                      "Mode"),
        ["Proxy_Mode_Rule"]               = ("规则",                          "Rule"),
        ["Proxy_Mode_Global"]             = ("全局",                          "Global"),
        ["Proxy_Mode_Direct"]             = ("直连",                          "Direct"),
        ["Proxy_TestAll"]                 = ("全部测速",                      "Test All"),
        ["Proxy_TestGroup"]               = ("测速",                          "Test"),
        ["Proxy_Latency"]                 = ("延迟",                          "Latency"),
        ["Proxy_NotRunning"]              = ("内核未运行，请先在订阅页面启动内核。", "Core is not running. Please start the core from the Subscription page."),
        ["Proxy_Loading"]                 = ("正在加载代理组…",               "Loading proxy groups…"),
        ["Proxy_NoGroups"]                = ("没有可用的代理组",              "No proxy groups available"),
        ["Proxy_SelectNode"]              = ("选择节点",                      "Select node"),

        // Logs
        ["Logs_Level_All"]                = ("全部",                          "All"),
        ["Logs_Level_Debug"]              = ("Debug",                         "Debug"),
        ["Logs_Level_Info"]               = ("Info",                          "Info"),
        ["Logs_Level_Warn"]               = ("Warn",                          "Warn"),
        ["Logs_Level_Error"]              = ("Error",                         "Error"),
        ["Logs_Pause"]                    = ("暂停",                          "Pause"),
        ["Logs_Resume"]                   = ("继续",                          "Resume"),
        ["Logs_Clear"]                    = ("清空",                          "Clear"),
        ["Logs_Search"]                   = ("搜索日志…",                     "Search logs…"),
        ["Logs_NotRunning"]               = ("内核未运行",                    "Core is not running"),
        ["Logs_Empty"]                    = ("暂无日志",                      "No logs yet"),

        // Connections
        ["Connections_Total_Down"]        = ("总下载",                        "Total Down"),
        ["Connections_Total_Up"]          = ("总上传",                        "Total Up"),
        ["Connections_CloseAll"]          = ("关闭全部",                      "Close All"),
        ["Connections_CloseAllConfirm"]   = ("确定关闭全部连接？",            "Close all connections?"),
        ["Connections_Search"]            = ("搜索连接…",                     "Search connections…"),
        ["Connections_Empty"]             = ("暂无活跃连接",                  "No active connections"),
        ["Connections_NotRunning"]        = ("内核未运行",                    "Core is not running"),
        ["Connections_Rule"]              = ("规则",                          "Rule"),
        ["Connections_Chain"]             = ("链路",                          "Chain"),

        // Core
        ["Core_Start"]                    = ("启动内核",                      "Start Core"),
        ["Core_Stop"]                     = ("停止内核",                      "Stop Core"),
        ["Core_Running"]                  = ("运行中",                        "Running"),
        ["Core_Stopped"]                  = ("未运行",                        "Stopped"),
        ["Core_StartFailed"]              = ("内核启动失败",                  "Failed to start core"),

        // Home
        ["Home_ActiveSubscription"]       = ("当前订阅",                      "Active Subscription"),
        ["Home_NetworkMode"]              = ("网络模式",                      "Network Mode"),
        ["Home_SystemProxy"]              = ("系统代理",                      "System Proxy"),
        ["Home_SystemProxy_Description"]  = ("将系统代理指向 mihomo 入站端口", "Route system proxy through mihomo inbound port"),
        ["Home_TunMode"]                  = ("虚拟网卡 (TUN)",               "TUN Mode"),
        ["Home_TunMode_Description"]      = ("使用虚拟网卡接管所有流量",      "Capture all traffic via virtual TUN adapter"),
        ["Home_ProxyMode"]                = ("代理模式",                      "Proxy Mode"),
        ["Home_CurrentNode"]              = ("当前节点",                      "Current Node"),
        ["Home_NoSubscription"]           = ("无可用订阅",                    "No subscription available"),
    };

    private static string T(string key) =>
        _.TryGetValue(key, out var v)
            ? (Helpers.AppSettings.Language == "zh" ? v.Zh : v.En)
            : key;

    // ── Navigation ──────────────────────────────────────────────────────
    public static string Nav_Home         => T(nameof(Nav_Home));
    public static string Nav_Proxy        => T(nameof(Nav_Proxy));
    public static string Nav_Subscription => T(nameof(Nav_Subscription));
    public static string Nav_Connections  => T(nameof(Nav_Connections));
    public static string Nav_Logs         => T(nameof(Nav_Logs));
    public static string Nav_Settings     => T(nameof(Nav_Settings));

    // ── App & Shell ─────────────────────────────────────────────────────
    public static string App_Title        => T(nameof(App_Title));
    public static string Search_Placeholder => T(nameof(Search_Placeholder));
    public static string About_AppName    => T(nameof(About_AppName));
    public static string Language_English => T(nameof(Language_English));
    public static string Language_Chinese => T(nameof(Language_Chinese));

    // ── Settings ────────────────────────────────────────────────────────
    public static string Settings_Title               => T(nameof(Settings_Title));
    public static string Settings_Appearance          => T(nameof(Settings_Appearance));
    public static string Settings_AppTheme            => T(nameof(Settings_AppTheme));
    public static string Settings_AppTheme_Description => T(nameof(Settings_AppTheme_Description));
    public static string Theme_Light                  => T(nameof(Theme_Light));
    public static string Theme_Dark                   => T(nameof(Theme_Dark));
    public static string Theme_System                 => T(nameof(Theme_System));
    public static string Settings_Language            => T(nameof(Settings_Language));
    public static string Settings_Language_Description => T(nameof(Settings_Language_Description));
    public static string Settings_About               => T(nameof(Settings_About));
    public static string Settings_About_Description   => T(nameof(Settings_About_Description));
    public static string About_GitHub                 => T(nameof(About_GitHub));

    // ── Subscription ────────────────────────────────────────────────────
    public static string Subscription_Import              => T(nameof(Subscription_Import));
    public static string Subscription_New                 => T(nameof(Subscription_New));
    public static string Subscription_Open                => T(nameof(Subscription_Open));
    public static string Subscription_List                => T(nameof(Subscription_List));
    public static string Subscription_ImportFromUrl       => T(nameof(Subscription_ImportFromUrl));
    public static string Subscription_ImportFromFile      => T(nameof(Subscription_ImportFromFile));
    public static string Subscription_NewProfile          => T(nameof(Subscription_NewProfile));
    public static string Subscription_OpenFile            => T(nameof(Subscription_OpenFile));
    public static string Subscription_UrlPlaceholder      => T(nameof(Subscription_UrlPlaceholder));
    public static string Subscription_Name                => T(nameof(Subscription_Name));
    public static string Subscription_UrlOrPath           => T(nameof(Subscription_UrlOrPath));
    public static string Subscription_Updated             => T(nameof(Subscription_Updated));
    public static string Subscription_TypeRemote          => T(nameof(Subscription_TypeRemote));
    public static string Subscription_TypeLocal           => T(nameof(Subscription_TypeLocal));
    public static string Subscription_Delete              => T(nameof(Subscription_Delete));
    public static string Subscription_DeleteConfirm       => T(nameof(Subscription_DeleteConfirm));
    public static string Subscription_EditInfo            => T(nameof(Subscription_EditInfo));
    public static string Subscription_More                => T(nameof(Subscription_More));
    public static string Subscription_UpdateIntervalMinutes => T(nameof(Subscription_UpdateIntervalMinutes));
    public static string Subscription_Update              => T(nameof(Subscription_Update));
    public static string Subscription_NoItems             => T(nameof(Subscription_NoItems));
    public static string Subscription_EmptyTitle          => T(nameof(Subscription_EmptyTitle));
    public static string Subscription_Refresh             => T(nameof(Subscription_Refresh));
    public static string Subscription_RefreshedNever      => T(nameof(Subscription_RefreshedNever));
    public static string Subscription_UsageTotal          => T(nameof(Subscription_UsageTotal));
    public static string Subscription_SelectFirst         => T(nameof(Subscription_SelectFirst));
    public static string Subscription_NoCacheHint         => T(nameof(Subscription_NoCacheHint));

    // ── Common ──────────────────────────────────────────────────────────
    public static string Common_Cancel  => T(nameof(Common_Cancel));
    public static string Common_Ok      => T(nameof(Common_Ok));
    public static string Common_Refresh => T(nameof(Common_Refresh));
    public static string Common_Close   => T(nameof(Common_Close));
    public static string Common_Search  => T(nameof(Common_Search));
    public static string Common_Loading => T(nameof(Common_Loading));

    // ── Proxy ───────────────────────────────────────────────────────────
    public static string Proxy_Mode       => T(nameof(Proxy_Mode));
    public static string Proxy_Mode_Rule  => T(nameof(Proxy_Mode_Rule));
    public static string Proxy_Mode_Global => T(nameof(Proxy_Mode_Global));
    public static string Proxy_Mode_Direct => T(nameof(Proxy_Mode_Direct));
    public static string Proxy_TestAll    => T(nameof(Proxy_TestAll));
    public static string Proxy_TestGroup  => T(nameof(Proxy_TestGroup));
    public static string Proxy_Latency    => T(nameof(Proxy_Latency));
    public static string Proxy_NotRunning => T(nameof(Proxy_NotRunning));
    public static string Proxy_Loading    => T(nameof(Proxy_Loading));
    public static string Proxy_NoGroups   => T(nameof(Proxy_NoGroups));
    public static string Proxy_SelectNode => T(nameof(Proxy_SelectNode));

    // ── Logs ────────────────────────────────────────────────────────────
    public static string Logs_Level_All     => T(nameof(Logs_Level_All));
    public static string Logs_Level_Debug   => T(nameof(Logs_Level_Debug));
    public static string Logs_Level_Info    => T(nameof(Logs_Level_Info));
    public static string Logs_Level_Warn    => T(nameof(Logs_Level_Warn));
    public static string Logs_Level_Error   => T(nameof(Logs_Level_Error));
    public static string Logs_Pause         => T(nameof(Logs_Pause));
    public static string Logs_Resume        => T(nameof(Logs_Resume));
    public static string Logs_Clear         => T(nameof(Logs_Clear));
    public static string Logs_Search        => T(nameof(Logs_Search));
    public static string Logs_NotRunning    => T(nameof(Logs_NotRunning));
    public static string Logs_Empty         => T(nameof(Logs_Empty));

    // ── Connections ─────────────────────────────────────────────────────
    public static string Connections_Total_Down       => T(nameof(Connections_Total_Down));
    public static string Connections_Total_Up         => T(nameof(Connections_Total_Up));
    public static string Connections_CloseAll         => T(nameof(Connections_CloseAll));
    public static string Connections_CloseAllConfirm  => T(nameof(Connections_CloseAllConfirm));
    public static string Connections_Search           => T(nameof(Connections_Search));
    public static string Connections_Empty            => T(nameof(Connections_Empty));
    public static string Connections_NotRunning       => T(nameof(Connections_NotRunning));
    public static string Connections_Rule             => T(nameof(Connections_Rule));
    public static string Connections_Chain            => T(nameof(Connections_Chain));

    // ── Core ────────────────────────────────────────────────────────────
    public static string Core_Start      => T(nameof(Core_Start));
    public static string Core_Stop       => T(nameof(Core_Stop));
    public static string Core_Running    => T(nameof(Core_Running));
    public static string Core_Stopped    => T(nameof(Core_Stopped));
    public static string Core_StartFailed => T(nameof(Core_StartFailed));

    // ── Home ────────────────────────────────────────────────────────────
    public static string Home_ActiveSubscription       => T(nameof(Home_ActiveSubscription));
    public static string Home_NetworkMode              => T(nameof(Home_NetworkMode));
    public static string Home_SystemProxy              => T(nameof(Home_SystemProxy));
    public static string Home_SystemProxy_Description  => T(nameof(Home_SystemProxy_Description));
    public static string Home_TunMode                  => T(nameof(Home_TunMode));
    public static string Home_TunMode_Description      => T(nameof(Home_TunMode_Description));
    public static string Home_ProxyMode                => T(nameof(Home_ProxyMode));
    public static string Home_CurrentNode              => T(nameof(Home_CurrentNode));
    public static string Home_NoSubscription           => T(nameof(Home_NoSubscription));
}
