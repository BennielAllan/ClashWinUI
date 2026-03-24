using System;

namespace ClashWinUI.Helpers;

/// <summary>
/// Formatting for subscription list: relative time (refreshed ago) and usage/total.
/// </summary>
public static class SubscriptionDisplayHelper
{
    private static bool IsZh => AppSettings.Language == "zh";

    /// <summary>Returns relative time like "5 minutes ago" or "5 分钟前".</summary>
    public static string GetRefreshedAgo(DateTimeOffset? updatedAt)
    {
        if (updatedAt == null) return Strings.Subscription_RefreshedNever;
        var delta = DateTimeOffset.Now - updatedAt.Value;
        if (delta.TotalSeconds < 60)
            return IsZh ? "刚刚" : "Just now";
        if (delta.TotalMinutes < 60)
            return string.Format(IsZh ? "{0} 分钟前" : "{0} min ago", (int)delta.TotalMinutes);
        if (delta.TotalHours < 24)
            return string.Format(IsZh ? "{0} 小时前" : "{0} hr ago", (int)delta.TotalHours);
        if (delta.TotalDays < 30)
            return string.Format(IsZh ? "{0} 天前" : "{0} days ago", (int)delta.TotalDays);
        return updatedAt.Value.LocalDateTime.ToString("g");
    }

    /// <summary>Formats usage and total bytes, e.g. "1.2 GB / 50 GB" or "— / 无限".</summary>
    public static string FormatUsageTotal(long? usageBytes, long? totalBytes)
    {
        var usage = usageBytes.HasValue ? FormatBytes(usageBytes.Value) : "—";
        var total = totalBytes.HasValue ? FormatBytes(totalBytes.Value) : (IsZh ? "无限" : "Unlimited");
        return $"{usage} / {total}";
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double v = bytes;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return i == 0 ? $"{v:F0} {units[i]}" : $"{v:F2} {units[i]}";
    }
}
