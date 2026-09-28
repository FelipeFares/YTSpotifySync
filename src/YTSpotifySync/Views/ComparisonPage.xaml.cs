using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YTSpotifySync.ViewModels;

namespace YTSpotifySync.Views;

public sealed partial class ComparisonPage : Page
{
    public ComparisonViewModel ViewModel { get; }

    public ComparisonPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ComparisonViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshData();
    }
}
