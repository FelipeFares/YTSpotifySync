using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTSpotifySync.Services;
using YTSpotifySync.Views;

namespace YTSpotifySync.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly YouTubeService _youtubeService;
    private readonly SpotifyService _spotifyService;
    private readonly SyncEngine _syncEngine;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    public partial int YouTubeVideoCount { get; set; }

    [ObservableProperty]
    public partial int SpotifyEpisodeCount { get; set; }

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int SyncedCount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Pronto para sincronizar.";

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string LastSyncTimeFormatted { get; set; } = "Nenhuma sincronização realizada ainda.";

    [ObservableProperty]
    public partial bool HasSynced { get; set; }

    public DashboardViewModel(
        YouTubeService youtubeService,
        SpotifyService spotifyService,
        SyncEngine syncEngine)
    {
        _youtubeService = youtubeService;
        _spotifyService = spotifyService;
        _syncEngine = syncEngine;

        UpdateFromCurrentResults();
        _syncEngine.ResultsUpdated += (s, e) => UpdateFromCurrentResults();
    }

    private void UpdateFromCurrentResults()
    {
        if (_syncEngine.CurrentResults.Count > 0)
        {
            YouTubeVideoCount = _syncEngine.CurrentResults.Count;
            SyncedCount = _syncEngine.CurrentResults.Count(r => r.IsSynced);
            PendingCount = _syncEngine.CurrentResults.Count(r => r.IsPending);
            HasSynced = true;
        }
    }

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusText = "Iniciando consulta às APIs...";

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            var progress = new Progress<string>(msg => StatusText = msg);

            // Fetch YouTube videos and Spotify episodes in parallel
            var ytTask = _youtubeService.GetAllVideosAsync(progress, ct);
            var spTask = _spotifyService.GetAllEpisodesAsync(progress, ct);

            await Task.WhenAll(ytTask, spTask);

            var videos = await ytTask;
            var episodes = await spTask;

            YouTubeVideoCount = videos.Count;
            SpotifyEpisodeCount = episodes.Count;

            StatusText = "Cruzando títulos e identificando vídeos pendentes...";
            var results = _syncEngine.Compare(videos, episodes);

            SyncedCount = results.Count(r => r.IsSynced);
            PendingCount = results.Count(r => r.IsPending);
            HasSynced = true;

            LastSyncTimeFormatted = $"Última sincronização: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            StatusText = $"Sincronização concluída! {PendingCount} vídeos pendentes encontrados.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Sincronização cancelada.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Erro durante a sincronização: {ex.Message}";
            StatusText = "Falha na sincronização.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NavigateToComparison()
    {
        App.MainWindowInstance?.NavigateTo(typeof(ComparisonPage));
    }

    [RelayCommand]
    public void NavigateToDownloads()
    {
        App.MainWindowInstance?.NavigateTo(typeof(DownloadPage));
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        App.MainWindowInstance?.NavigateTo(typeof(SettingsPage));
    }
}
