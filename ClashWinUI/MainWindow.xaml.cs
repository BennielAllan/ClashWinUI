using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Navigation;
using ClashWinUI.Helpers;
using ClashWinUI.Pages;

namespace ClashWinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetWindowProperties();
        AppSettings.LanguageChanged += OnLanguageChanged;
        RootGrid.ActualThemeChanged += (_, __) =>
            TitleBarHelper.ApplySystemThemeToCaptionButtons(this, RootGrid.ActualTheme);
    }

    private void ApplyLocalizedStrings()
    {
        Title = Strings.App_Title;
        titleBar.Title = Strings.App_Title;
        UpdateNavigationItemLabels();
    }

    private void OnLanguageChanged()
    {
        ApplyLocalizedStrings();
        var currentType = RootFrame.CurrentSourcePageType;
        if (currentType != null)
            RootFrame.Navigate(currentType);
    }

    private void SetWindowProperties()
    {
        ApplyLocalizedStrings();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        if (AppWindow != null)
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
    }

    public NavigationView NavigationView => NavigationViewControl;

    private void RootGrid_Loaded(object _, RoutedEventArgs __)
    {
        WindowHelper.TrackWindow(this);
        WindowHelper.SetWindowMinSize(this, 640, 500);
        var scale = RootGrid.XamlRoot.RasterizationScale;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(900 * scale), (int)(620 * scale)));
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, RootGrid.ActualTheme);
    }

    private void OnNavigationViewLoaded(object _, RoutedEventArgs __)
    {
        RootFrame.NavigationFailed += OnNavigationFailed;
        UpdateNavigationItemLabels();
        if (RootFrame.Content == null)
        {
            Navigate(typeof(HomePage));
            NavigationViewControl.SelectedItem = HomeItem;
        }
    }

    private void UpdateNavigationItemLabels()
    {
        HomeItem.Content = Strings.Nav_Home;
        ProxyItem.Content = Strings.Nav_Proxy;
        SubscriptionItem.Content = Strings.Nav_Subscription;
        ConnectionsItem.Content = Strings.Nav_Connections;
        LogsItem.Content = Strings.Nav_Logs;
        if (NavigationViewControl.SettingsItem is NavigationViewItem settingsItem)
            settingsItem.Content = Strings.Nav_Settings;
    }

    private void OnNavigationFailed(object _, NavigationFailedEventArgs e)
    {
        throw new System.Exception("Failed to load Page " + e.SourcePageType?.FullName);
    }

    public void Navigate(Type pageType, object? parameter = null)
    {
        RootFrame.Navigate(pageType, parameter);
    }

    private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            if (RootFrame.CurrentSourcePageType != typeof(SettingsPage))
                Navigate(typeof(SettingsPage));
            return;
        }
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
            return;
        var pageType = tag switch
        {
            "Home" => typeof(HomePage),
            "Proxy" => typeof(ProxyPage),
            "Subscription" => typeof(SubscriptionPage),
            "Connections" => typeof(ConnectionsPage),
            "Logs" => typeof(LogsPage),
            _ => null
        };
        if (pageType != null && RootFrame.CurrentSourcePageType != pageType)
            Navigate(pageType);
    }

    private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar _, object __)
    {
        if (RootFrame.CanGoBack)
            RootFrame.GoBack();
    }

    private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar _, object __)
    {
        NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
    }

    private void OnPaneDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs _)
    {
        titleBar.IsPaneToggleButtonVisible = sender.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
    }
}
