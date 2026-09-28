using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YTSpotifySync.Models;

public partial class DownloadTask : ObservableObject
{
    [ObservableProperty]
    public partial VideoItem Video { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Na Fila";

    [ObservableProperty]
    public partial string? OutputPath { get; set; }

    [ObservableProperty]
    public partial string? ThumbnailPath { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    public CancellationTokenSource? Cts { get; set; }

    public DownloadTask(VideoItem video)
    {
        Video = video;
    }
}
