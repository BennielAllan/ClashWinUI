using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using ClashWinUI.Helpers;
using ClashWinUI.Models;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class SubscriptionPage : Page
{
    public string PageTitle => Strings.Nav_Subscription;
    public string Subscription_Import => Strings.Subscription_Import;
    public string Subscription_New => Strings.Subscription_New;
    public string Subscription_Open => Strings.Subscription_Open;
    public string Subscription_NoItems => Strings.Subscription_NoItems;
    public string Subscription_ImportFromUrl => Strings.Subscription_ImportFromUrl;
    public string Subscription_ImportFromFile => Strings.Subscription_ImportFromFile;
    public string Subscription_EditInfo => Strings.Subscription_EditInfo;
    public string Subscription_More => Strings.Subscription_More;
    public string Subscription_Delete => Strings.Subscription_Delete;
    public string Subscription_UpdateIntervalMinutes => Strings.Subscription_UpdateIntervalMinutes;

    public ObservableCollection<SubscriptionItem> SubscriptionItems => SubscriptionService.Instance.Items;
    private SubscriptionItem? _selectedItem;

    public SubscriptionPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SubscriptionService.Instance.Items.CollectionChanged += (_, _) => UpdateEmptyState();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await SubscriptionService.Instance.LoadAsync();
        UpdateEmptyState();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Subscription_Import,
            PrimaryButtonText = Strings.Subscription_ImportFromUrl,
            SecondaryButtonText = Strings.Subscription_ImportFromFile,
            CloseButtonText = Strings.Common_Cancel
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            await ImportFromUrlAsync();
        else if (result == ContentDialogResult.Secondary)
            await ImportFromFileAsync();
    }

    private void UpdateEmptyState()
    {
        EmptyStatePanel.Visibility = SubscriptionItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SubscriptionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedItem = SubscriptionListView.SelectedItem as SubscriptionItem;
    }

    private void SubscriptionMore_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not SubscriptionItem item) return;
        var button = (FrameworkElement)sender;
        var flyout = new MenuFlyout();
        var editItem = new MenuFlyoutItem { Text = Strings.Subscription_EditInfo };
        editItem.Click += async (_, _) => { await ShowEditSubscriptionDialogAsync(item); };
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
        flyout.Items.Add(editItem);
        flyout.Items.Add(deleteItem);
        flyout.ShowAt(button);
    }

    private async System.Threading.Tasks.Task ShowEditSubscriptionDialogAsync(SubscriptionItem item)
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
            Width = 120,
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

    private async void SubscriptionRefresh_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not SubscriptionItem item) return;
        item.IsRefreshing = true;
        try
        {
            await RefreshSubscriptionAsync(item);
        }
        finally
        {
            item.IsRefreshing = false;
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
                item.UpdatedAt = DateTimeOffset.Now;
                if (response.Headers.TryGetValues("Subscription-Userinfo", out var values))
                {
                    var info = string.Join(" ", values);
                    ParseSubscriptionUserinfo(info, item);
                }
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

    /// <summary>Parse Subscription-Userinfo header (e.g. upload=0; download=0; total=5368709120).</summary>
    private static void ParseSubscriptionUserinfo(string header, SubscriptionItem item)
    {
        long? upload = null, download = null, total = null;
        foreach (var part in header.Split(';', StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            var key = kv[0].ToLowerInvariant();
            if (!long.TryParse(kv[1], out var val)) continue;
            if (key == "upload") upload = val;
            else if (key == "download") download = val;
            else if (key == "total") total = val;
        }
        if (upload.HasValue && download.HasValue)
            item.UsageBytes = upload.Value + download.Value;
        if (total.HasValue)
            item.TotalBytes = total.Value;
    }

    private async void SubscriptionListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (_selectedItem != null)
            await OpenSelectedAsync();
    }

    private async void ImportFromUrl_Click(object sender, RoutedEventArgs e) => await ImportFromUrlAsync();
    private async void ImportFromFile_Click(object sender, RoutedEventArgs e) => await ImportFromFileAsync();

    private async System.Threading.Tasks.Task ImportFromUrlAsync()
    {
        var urlBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_UrlPlaceholder,
            Width = 400,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };
        var nameBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_Name,
            Width = 400,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_Name });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "URL", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) });
        panel.Children.Add(urlBox);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Subscription_ImportFromUrl,
            Content = panel,
            PrimaryButtonText = Strings.Subscription_Import,
            CloseButtonText = Strings.Common_Cancel
        };
        dialog.PrimaryButtonClick += (_, _) =>
        {
            var url = urlBox.Text?.Trim();
            var name = nameBox.Text?.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                var item = new SubscriptionItem
                {
                    Name = string.IsNullOrEmpty(name) ? url : name,
                    UrlOrPath = url,
                    IsRemote = true,
                    UpdatedAt = DateTimeOffset.Now
                };
                SubscriptionService.Instance.Add(item);
                UpdateEmptyState();
            }
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ImportFromFileAsync()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker
        {
            ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".yaml");
        picker.FileTypeFilter.Add(".yml");
        picker.FileTypeFilter.Add(".json");
        if (WindowHelper.GetWindowForElement(this) is { } window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;
        var item = new SubscriptionItem
        {
            Name = file.Name,
            UrlOrPath = file.Path,
            IsRemote = false,
            UpdatedAt = DateTimeOffset.Now
        };
        SubscriptionService.Instance.Add(item);
        UpdateEmptyState();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_Name,
            Width = 400,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };
        var pathBox = new TextBox
        {
            PlaceholderText = Strings.Subscription_UrlOrPath,
            Width = 400,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_Name });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.Subscription_UrlOrPath + " (optional)", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) });
        panel.Children.Add(pathBox);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Subscription_NewProfile,
            Content = panel,
            PrimaryButtonText = Strings.Subscription_New,
            CloseButtonText = Strings.Common_Cancel
        };
        dialog.PrimaryButtonClick += (_, _) =>
        {
            var name = nameBox.Text?.Trim();
            var path = pathBox.Text?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                var item = new SubscriptionItem
                {
                    Name = name,
                    UrlOrPath = path ?? string.Empty,
                    IsRemote = false,
                    UpdatedAt = DateTimeOffset.Now
                };
                SubscriptionService.Instance.Add(item);
                UpdateEmptyState();
            }
        };
        await dialog.ShowAsync();
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenSelectedAsync();
    }

    private async System.Threading.Tasks.Task OpenSelectedAsync()
    {
        if (_selectedItem == null)
        {
            var d = new ContentDialog { XamlRoot = XamlRoot, Title = Strings.Subscription_Open, Content = Strings.Subscription_SelectFirst, CloseButtonText = Strings.Common_Ok };
            await d.ShowAsync();
            return;
        }
        if (!_selectedItem.IsRemote && !string.IsNullOrEmpty(_selectedItem.UrlOrPath))
        {
            var path = _selectedItem.UrlOrPath;
            if (System.IO.File.Exists(path))
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                var options = new LauncherOptions { DisplayApplicationPicker = true };
                await Launcher.LaunchFileAsync(file, options);
            }
        }
        else if (_selectedItem.IsRemote && !string.IsNullOrEmpty(_selectedItem.UrlOrPath))
        {
            var uri = new Uri(_selectedItem.UrlOrPath);
            await Launcher.LaunchUriAsync(uri);
        }
    }
}
