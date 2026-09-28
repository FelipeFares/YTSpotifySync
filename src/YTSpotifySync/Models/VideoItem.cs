using System;
using YTSpotifySync.Helpers;

namespace YTSpotifySync.Models;

public record VideoItem(
    string VideoId,
    string Title,
    string Description,
    string ThumbnailUrl,
    DateTime PublishedAt,
    TimeSpan Duration,
    VideoType Type)
{
    public string NormalizedTitle => TitleNormalizer.Normalize(Title);

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";

    public string DurationFormatted => Duration.Hours > 0
        ? $"{Duration.Hours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}"
        : $"{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public string PublishedAtFormatted => PublishedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string TypeDisplayName => Type switch
    {
        VideoType.LiveStream => "Ao Vivo",
        VideoType.Short => "Short",
        _ => "Vídeo"
    };
}
