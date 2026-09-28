using CommunityToolkit.Mvvm.ComponentModel;

namespace YTSpotifySync.Models;

public partial class SyncResult : ObservableObject
{
    [ObservableProperty]
    public partial VideoItem Video { get; set; }

    [ObservableProperty]
    public partial EpisodeItem? MatchedEpisode { get; set; }

    [ObservableProperty]
    public partial SyncStatus Status { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SyncResult(VideoItem video, EpisodeItem? matchedEpisode, SyncStatus status)
    {
        Video = video;
        MatchedEpisode = matchedEpisode;
        Status = status;
    }

    public bool IsSynced => Status == SyncStatus.Synced;
    public bool IsPending => Status == SyncStatus.Pending;

    public string StatusDisplayName => Status switch
    {
        SyncStatus.Synced => "Sincronizado",
        SyncStatus.Pending => "Pendente",
        SyncStatus.Downloading => "Baixando...",
        SyncStatus.Ready => "Pronto para Envio",
        _ => Status.ToString()
    };
}
