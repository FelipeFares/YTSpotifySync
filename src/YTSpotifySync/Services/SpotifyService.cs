using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using YTSpotifySync.Models;

namespace YTSpotifySync.Services;

public class SpotifyService
{
    private readonly SettingsService _settingsService;

    public SpotifyService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private async Task<SpotifyClient> CreateClientAsync()
    {
        var config = SpotifyClientConfig.CreateDefault();
        var oauth = new OAuthClient(config);
        var tokenResponse = await oauth.RequestToken(
            new ClientCredentialsRequest(_settingsService.SpotifyClientId, _settingsService.SpotifyClientSecret));

        return new SpotifyClient(config.WithToken(tokenResponse.AccessToken));
    }

    /// <summary>
    /// Tests connection and validates Client ID, Client Secret, and Show ID.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settingsService.SpotifyClientId) ||
                string.IsNullOrWhiteSpace(_settingsService.SpotifyClientSecret) ||
                string.IsNullOrWhiteSpace(_settingsService.SpotifyShowId))
            {
                return false;
            }

            var spotify = await CreateClientAsync();
            var show = await spotify.Shows.Get(_settingsService.SpotifyShowId, cancellationToken);

            return show != null && !string.IsNullOrEmpty(show.Id);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Fetches all episodes published on the configured Spotify podcast show.
    /// Handles pagination across the entire episode catalog.
    /// </summary>
    public async Task<List<EpisodeItem>> GetAllEpisodesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<EpisodeItem>();

        if (string.IsNullOrWhiteSpace(_settingsService.SpotifyClientId) ||
            string.IsNullOrWhiteSpace(_settingsService.SpotifyClientSecret) ||
            string.IsNullOrWhiteSpace(_settingsService.SpotifyShowId))
        {
            return result;
        }

        progress?.Report("Conectando ao Spotify...");
        var spotify = await CreateClientAsync();

        progress?.Report("Obtendo episódios do podcast...");
        var request = new ShowEpisodesRequest
        {
            Limit = 50,
            Market = "BR"
        };

        var firstPage = await spotify.Shows.GetEpisodes(_settingsService.SpotifyShowId, request, cancellationToken);
        var allEpisodes = await spotify.PaginateAll(firstPage, cancellationToken: cancellationToken);

        foreach (var ep in allEpisodes)
        {
            if (ep == null) continue;

            DateTime releaseDate = DateTime.MinValue;
            if (!string.IsNullOrEmpty(ep.ReleaseDate))
            {
                if (DateTime.TryParse(ep.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    releaseDate = parsed;
                }
            }

            string imageUrl = ep.Images?.FirstOrDefault()?.Url ?? string.Empty;
            var duration = TimeSpan.FromMilliseconds(ep.DurationMs);

            result.Add(new EpisodeItem(
                EpisodeId: ep.Id ?? string.Empty,
                Name: ep.Name ?? "Sem título",
                Description: ep.Description ?? string.Empty,
                ImageUrl: imageUrl,
                ReleaseDate: releaseDate,
                Duration: duration));
        }

        progress?.Report($"{result.Count} episódios carregados do Spotify.");
        return result.OrderByDescending(e => e.ReleaseDate).ToList();
    }
}
