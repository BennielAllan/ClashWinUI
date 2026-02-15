using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using ClashWinUI.Helpers;

namespace ClashWinUI;

public partial class App : Application
{
    internal static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ThemeHelper.Initialize();
        MainWindow = new MainWindow();
        Helpers.WindowHelper.TrackWindow(MainWindow);
        MainWindow.Activate();
    }
}
