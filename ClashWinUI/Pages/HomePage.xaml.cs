using Microsoft.UI.Xaml.Controls;

namespace ClashWinUI.Pages;

public sealed partial class HomePage : Page
{
    public string PageTitle => Strings.Nav_Home;
    public HomePage() => InitializeComponent();
}
