using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ClashWinUI.Helpers;

namespace ClashWinUI.Pages;

public sealed partial class SettingsPage : Page
{
    public string VersionDisplay => "v1.0.0";

    public string Settings_Title => Strings.Settings_Title;
    public string Settings_Appearance => Strings.Settings_Appearance;
    public string Settings_AppTheme => Strings.Settings_AppTheme;
    public string Settings_AppTheme_Description => Strings.Settings_AppTheme_Description;
    public string Theme_Light => Strings.Theme_Light;
    public string Theme_Dark => Strings.Theme_Dark;
    public string Theme_System => Strings.Theme_System;
    public string Settings_Language => Strings.Settings_Language;
    public string Settings_Language_Description => Strings.Settings_Language_Description;
    public string Settings_About => Strings.Settings_About;
    public string Settings_About_Description => Strings.Settings_About_Description;
    public string Language_English => Strings.Language_English;
    public string Language_Chinese => Strings.Language_Chinese;
    public string About_AppName => Strings.About_AppName;
    public string About_GitHub => Strings.About_GitHub;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var theme = ThemeHelper.RootTheme;
        ThemeComboBox.SelectedIndex = theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };
        var lang = AppSettings.Language;
        LanguageComboBox.SelectedIndex = lang == "zh" ? 1 : 0;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;
        if (WindowHelper.GetWindowForElement(this) is not Window window)
            return;
        var theme = EnumHelper.GetEnum<ElementTheme>(tag);
        ThemeHelper.RootTheme = theme;
        var resolved = theme == ElementTheme.Default ? ThemeHelper.ActualTheme : theme;
        TitleBarHelper.ApplySystemThemeToCaptionButtons(window, resolved);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;
        AppSettings.Language = tag;
        // Optionally restart or notify that language will apply on next launch
    }
}
