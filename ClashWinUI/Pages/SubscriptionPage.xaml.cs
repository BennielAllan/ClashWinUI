using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using ClashWinUI.Helpers;
using ClashWinUI.Models;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class SubscriptionPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string PageTitle => Strings.Nav_Subscription;
    public string Subscription_New => Strings.Subscription_New;
    public string Subscription_NoItems => Strings.Subscription_NoItems;
    public string Subscription_EmptyTitle => Strings.Subscription_EmptyTitle;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ────────────────────────────────────────────────────────────────────────

    public ObservableCollection<SubscriptionItem> SubscriptionItems => SubscriptionService.Instance.Items;

    public SubscriptionPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SubscriptionService.Instance.Items.CollectionChanged += (_, _) => UpdateEmptyState();
    }

    private async void OnLoaded(object _, RoutedEventArgs __)
    {
        await SubscriptionService.Instance.LoadAsync();
        UpdateEmptyState();
        // Sync ListView selection with active item
        var active = SubscriptionService.Instance.ActiveItem;
        if (active != null)
            SubscriptionListView.SelectedItem = active;
    }

    private void SubscriptionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubscriptionListView.SelectedItem is SubscriptionItem item)
            SubscriptionService.Instance.SetActive(item);
    }

    private void UpdateEmptyState()
    {
        var isEmpty = SubscriptionItems.Count == 0;
        EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        SubscriptionListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        CommandBarPanel.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void NewButton_Click(object _, RoutedEventArgs __)
    {
        var nameBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_Name,
            Width = 400,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var urlBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_UrlPlaceholder,
            Width = 400,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var intervalBox = new NumberBox
        {
            Value = 10,
            Minimum = 0,
            SmallChange = 1,
            LargeChange = 60,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            Width = 180,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_Name });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "URL", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(urlBox);
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_UpdateIntervalMinutes, Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(intervalBox);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Subscription_NewProfile,
            Content = panel,
            PrimaryButtonText = Strings.Subscription_New,
            CloseButtonText = Strings.Common_Cancel
        };
        dialog.PrimaryButtonClick += async (_, _) =>
        {
            var name = nameBox.Text?.Trim();
            var url = urlBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (!string.IsNullOrEmpty(url))
            {
                // Try to download the subscription content first
                try
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ClashWinUI");
                    var response = await http.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var newItem = new SubscriptionItem
                    {
                        Name = name,
                        UrlOrPath = url,
                        IsRemote = true,
                        UpdateIntervalMinutes = double.IsNaN(intervalBox.Value) ? 0 : (int)Math.Max(0, intervalBox.Value),
                        UpdatedAt = DateTimeOffset.Now
                    };
                    var profilesDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ClashWinUI", "profiles");
                    System.IO.Directory.CreateDirectory(profilesDir);
                    var cachePath = System.IO.Path.Combine(profilesDir, $"{newItem.Id}.yaml");
                    var content = await response.Content.ReadAsStringAsync();
                    await System.IO.File.WriteAllTextAsync(cachePath, content);
                    newItem.CachedConfigPath = cachePath;

                    if (response.Headers.TryGetValues("Subscription-Userinfo", out var values))
                        ParseSubscriptionUserinfo(string.Join(" ", values), newItem);

                    SubscriptionService.Instance.Add(newItem);
                    UpdateEmptyState();
                }
                catch
                {
                    // Download failed, don't add the subscription
                }
            }
            else
            {
                SubscriptionService.Instance.Add(new SubscriptionItem
                {
                    Name = name,
                    UrlOrPath = string.Empty,
                    IsRemote = false,
                    UpdateIntervalMinutes = double.IsNaN(intervalBox.Value) ? 0 : (int)Math.Max(0, intervalBox.Value),
                    UpdatedAt = DateTimeOffset.Now
                });
                UpdateEmptyState();
            }
        };
        await dialog.ShowAsync();
    }

    private async void SubscriptionRefresh_Click(object sender, RoutedEventArgs _)
    {
        if ((sender as FrameworkElement)?.Tag is not SubscriptionItem item) return;
        item.IsRefreshing = true;
        try { await RefreshSubscriptionAsync(item); }
        finally { item.IsRefreshing = false; }
    }

    private void SubscriptionMore_Click(object sender, RoutedEventArgs _)
    {
        if ((sender as FrameworkElement)?.Tag is not SubscriptionItem item) return;
        var button = (FrameworkElement)sender;
        var flyout = new MenuFlyout();

        var openUrlItem = new MenuFlyoutItem { Text = Strings.Subscription_Open };
        openUrlItem.Click += async (_, _) => await OpenItemAsync(item);
        flyout.Items.Add(openUrlItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var editItem = new MenuFlyoutItem { Text = Strings.Subscription_EditInfo };
        editItem.Click += async (_, _) => await ShowEditSubscriptionDialogAsync(item);
        flyout.Items.Add(editItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var deleteItem = new MenuFlyoutItem { Text = Strings.Subscription_Delete };
        deleteItem.Click += async (_, _) =>
        {
            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = Strings.Subscription_Delete,
                Content = Strings.Subscription_DeleteConfirm,
                PrimaryButtonText = Strings.Common_Ok,
                CloseButtonText = Strings.Common_Cancel
            };
            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                SubscriptionService.Instance.Remove(item);
                UpdateEmptyState();
            }
        };
        flyout.Items.Add(deleteItem);
        flyout.ShowAt(button);
    }

    private async Task ShowEditSubscriptionDialogAsync(SubscriptionItem item)
    {
        var nameBox = new TextBox
        {
            Text = item.Name,
            PlaceholderText = Strings.Subscription_Name,
            Width = 400,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var urlBox = new TextBox
        {
            Text = item.UrlOrPath,
            PlaceholderText = Strings.Subscription_UrlPlaceholder,
            Width = 400,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var intervalBox = new NumberBox
        {
            Value = item.UpdateIntervalMinutes,
            Minimum = 0,
            SmallChange = 1,
            LargeChange = 60,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            Width = 180,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_Name });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_UrlOrPath, Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(urlBox);
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_UpdateIntervalMinutes, Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(intervalBox);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Subscription_EditInfo,
            Content = panel,
            PrimaryButtonText = Strings.Common_Ok,
            CloseButtonText = Strings.Common_Cancel
        };
        dialog.PrimaryButtonClick += (_, _) =>
        {
            item.Name = nameBox.Text?.Trim() ?? item.Name;
            item.UrlOrPath = urlBox.Text?.Trim() ?? item.UrlOrPath;
            item.UpdateIntervalMinutes = (int)Math.Max(0, intervalBox.Value);
            _ = SubscriptionService.Instance.SaveAsync();
        };
        await dialog.ShowAsync();
    }

    private async Task OpenItemAsync(SubscriptionItem item)
    {
        if (!item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath))
        {
            if (System.IO.File.Exists(item.UrlOrPath))
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(item.UrlOrPath);
                await Launcher.LaunchFileAsync(file, new LauncherOptions { DisplayApplicationPicker = true });
            }
        }
        else if (item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath))
        {
            await Launcher.LaunchUriAsync(new Uri(item.UrlOrPath));
        }
    }

    private static async Task RefreshSubscriptionAsync(SubscriptionItem item)
    {
        try
        {
            if (item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath))
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ClashWinUI");
                var response = await http.GetAsync(item.UrlOrPath);
                response.EnsureSuccessStatusCode();

                // Save YAML content to local cache so mihomo can load it.
                var profilesDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ClashWinUI", "profiles");
                System.IO.Directory.CreateDirectory(profilesDir);
                var cachePath = System.IO.Path.Combine(profilesDir, $"{item.Id}.yaml");
                var content = await response.Content.ReadAsStringAsync();
                await System.IO.File.WriteAllTextAsync(cachePath, content);
                item.CachedConfigPath = cachePath;

                item.UpdatedAt = DateTimeOffset.Now;
                if (response.Headers.TryGetValues("Subscription-Userinfo", out var values))
                    ParseSubscriptionUserinfo(string.Join(" ", values), item);
            }
            else
            {
                item.UpdatedAt = DateTimeOffset.Now;
            }
        }
        catch
        {
            item.UpdatedAt = DateTimeOffset.Now;
        }
        await SubscriptionService.Instance.SaveAsync();
    }

    private static void ParseSubscriptionUserinfo(string header, SubscriptionItem item)
    {
        long? upload = null, download = null, total = null;
        foreach (var part in header.Split(';', StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            if (!long.TryParse(kv[1], out var val)) continue;
            switch (kv[0].ToLowerInvariant())
            {
                case "upload": upload = val; break;
                case "download": download = val; break;
                case "total": total = val; break;
            }
        }
        if (upload.HasValue && download.HasValue) item.UsageBytes = upload.Value + download.Value;
        if (total.HasValue) item.TotalBytes = total.Value;
    }
}
