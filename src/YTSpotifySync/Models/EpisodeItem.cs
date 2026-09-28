using System;
using YTSpotifySync.Helpers;

namespace YTSpotifySync.Models;

public record EpisodeItem(
    string EpisodeId,
    string Name,
    string Description,
    string ImageUrl,
    DateTime ReleaseDate,
    TimeSpan Duration)
{
    public string NormalizedName => TitleNormalizer.Normalize(Name);

    public string EpisodeUrl => $"https://open.spotify.com/episode/{EpisodeId}";

    public string DurationFormatted => Duration.Hours > 0
        ? $"{Duration.Hours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}"
        : $"{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public string ReleaseDateFormatted => ReleaseDate.ToString("dd/MM/yyyy");
}
