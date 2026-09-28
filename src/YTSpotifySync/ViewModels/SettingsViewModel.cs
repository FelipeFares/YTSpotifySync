using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTSpotifySync.Services;

namespace YTSpotifySync.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly YouTubeService _youtubeService;
    private readonly SpotifyService _spotifyService;

    [ObservableProperty]
    public partial string YouTubeApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string YouTubeChannelId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SpotifyClientId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SpotifyClientSecret { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SpotifyShowId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string YtDlpPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DownloadDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSuccessInfoOpen { get; set; }

    [ObservableProperty]
    public partial bool IsTestingYouTube { get; set; }

    [ObservableProperty]
    public partial string YouTubeTestResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasYouTubeTestResult { get; set; }

    [ObservableProperty]
    public partial bool IsYouTubeTestSuccess { get; set; }

    [ObservableProperty]
    public partial bool IsTestingSpotify { get; set; }

    [ObservableProperty]
    public partial string SpotifyTestResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasSpotifyTestResult { get; set; }

    [ObservableProperty]
    public partial bool IsSpotifyTestSuccess { get; set; }

    public SettingsViewModel(
        SettingsService settingsService,
        YouTubeService youtubeService,
        SpotifyService spotifyService)
    {
        _settingsService = settingsService;
        _youtubeService = youtubeService;
        _spotifyService = spotifyService;
        LoadFromService();
    }

    private void LoadFromService()
    {
        YouTubeApiKey = _settingsService.YouTubeApiKey;
        YouTubeChannelId = _settingsService.YouTubeChannelId;
        SpotifyClientId = _settingsService.SpotifyClientId;
        SpotifyClientSecret = _settingsService.SpotifyClientSecret;
        SpotifyShowId = _settingsService.SpotifyShowId;
        YtDlpPath = _settingsService.YtDlpPath;
        DownloadDirectory = _settingsService.DownloadDirectory;
    }

    [RelayCommand]
    public void Save()
    {
        _settingsService.YouTubeApiKey = YouTubeApiKey.Trim();
        _settingsService.YouTubeChannelId = YouTubeChannelId.Trim();
        _settingsService.SpotifyClientId = SpotifyClientId.Trim();
        _settingsService.SpotifyClientSecret = SpotifyClientSecret.Trim();
        _settingsService.SpotifyShowId = SpotifyShowId.Trim();
        _settingsService.YtDlpPath = YtDlpPath.Trim();
        _settingsService.DownloadDirectory = DownloadDirectory.Trim();

        _settingsService.Save();

        StatusMessage = "Configurações salvas com sucesso!";
        IsSuccessInfoOpen = true;
    }

    [RelayCommand]
    public async Task TestYouTubeAsync()
    {
        Save();
        IsTestingYouTube = true;
        HasYouTubeTestResult = false;

        try
        {
            bool ok = await _youtubeService.TestConnectionAsync();
            IsYouTubeTestSuccess = ok;
            YouTubeTestResult = ok
                ? "Conexão com YouTube bem-sucedida! Canal localizado."
                : "Falha na conexão com YouTube. Verifique a API Key e o ID do Canal.";
        }
        catch (Exception ex)
        {
            IsYouTubeTestSuccess = false;
            YouTubeTestResult = $"Erro: {ex.Message}";
        }
        finally
        {
            IsTestingYouTube = false;
            HasYouTubeTestResult = true;
        }
    }

    [RelayCommand]
    public async Task TestSpotifyAsync()
    {
        Save();
        IsTestingSpotify = true;
        HasSpotifyTestResult = false;

        try
        {
            bool ok = await _spotifyService.TestConnectionAsync();
            IsSpotifyTestSuccess = ok;
            SpotifyTestResult = ok
                ? "Conexão com Spotify bem-sucedida! Podcast localizado."
                : "Falha na conexão com Spotify. Verifique Client ID, Client Secret e ID do Podcast.";
        }
        catch (Exception ex)
        {
            IsSpotifyTestSuccess = false;
            SpotifyTestResult = $"Erro: {ex.Message}";
        }
        finally
        {
            IsTestingSpotify = false;
            HasSpotifyTestResult = true;
        }
    }

    [RelayCommand]
    public async Task BrowseYtDlpAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            YtDlpPath = file.Path;
        }
    }

    [RelayCommand]
    public async Task BrowseDownloadDirAsync()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            DownloadDirectory = folder.Path;
        }
    }
}
