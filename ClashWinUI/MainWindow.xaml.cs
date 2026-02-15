using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        RootGrid.ActualThemeChanged += (_, _) =>
            TitleBarHelper.ApplySystemThemeToCaptionButtons(this, RootGrid.ActualTheme);
    }

    private void SetWindowProperties()
    {
        Title = "ClashWinUI";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        if (AppWindow != null)
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
    }

    public NavigationView NavigationView => NavigationViewControl;

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        WindowHelper.TrackWindow(this);
        WindowHelper.SetWindowMinSize(this, 640, 500);
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, RootGrid.ActualTheme);
    }

    private void OnNavigationViewLoaded(object sender, RoutedEventArgs e)
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
        RulesItem.Content = Strings.Nav_Rules;
        LogsItem.Content = Strings.Nav_Logs;
        TestItem.Content = Strings.Nav_Test;
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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
            "Rules" => typeof(RulesPage),
            "Logs" => typeof(LogsPage),
            "Test" => typeof(TestPage),
            _ => null
        };
        if (pageType != null && RootFrame.CurrentSourcePageType != pageType)
            Navigate(pageType);
    }

    private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (RootFrame.CanGoBack)
            RootFrame.GoBack();
    }

    private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
    }

    private void OnPaneDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        titleBar.IsPaneToggleButtonVisible = sender.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
    }
}
