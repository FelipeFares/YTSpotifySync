using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace YTSpotifySync;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public static MainWindow? MainWindowInstance => ((App)Current)._window as MainWindow;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<YTSpotifySync.Services.SettingsService>();
        services.AddSingleton<YTSpotifySync.Services.YouTubeService>();
        services.AddSingleton<YTSpotifySync.Services.SpotifyService>();
        services.AddSingleton<YTSpotifySync.Services.SyncEngine>();
        services.AddSingleton<YTSpotifySync.Services.DownloadService>();
        services.AddSingleton<YTSpotifySync.Services.SpotifyAutomationService>();

        // ViewModels
        services.AddTransient<YTSpotifySync.ViewModels.SettingsViewModel>();
        services.AddSingleton<YTSpotifySync.ViewModels.DashboardViewModel>();
        services.AddSingleton<YTSpotifySync.ViewModels.ComparisonViewModel>();
        services.AddSingleton<YTSpotifySync.ViewModels.DownloadViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
