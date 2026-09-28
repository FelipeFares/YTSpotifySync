using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using YTSpotifySync.ViewModels;

namespace YTSpotifySync.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        SecretBox.Password = ViewModel.SpotifyClientSecret;
    }

    private void SecretBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            ViewModel.SpotifyClientSecret = pb.Password;
        }
    }
}
