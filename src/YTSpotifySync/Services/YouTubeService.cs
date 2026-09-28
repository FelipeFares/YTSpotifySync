using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YTSpotifySync.Models;

using GoogleYouTubeService = Google.Apis.YouTube.v3.YouTubeService;

namespace YTSpotifySync.Services;

public class YouTubeService
{
    private readonly SettingsService _settingsService;

    public YouTubeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private GoogleYouTubeService CreateClient()
    {
        return new GoogleYouTubeService(new BaseClientService.Initializer
        {
            ApiKey = _settingsService.YouTubeApiKey,
            ApplicationName = "YTSpotifySync"
        });
    }

    /// <summary>
    /// Tests connection and validates API key + Channel ID.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settingsService.YouTubeApiKey) ||
                string.IsNullOrWhiteSpace(_settingsService.YouTubeChannelId))
            {
                return false;
            }

            using var youtube = CreateClient();
            var request = youtube.Channels.List("snippet");
            request.Id = _settingsService.YouTubeChannelId;
            var response = await request.ExecuteAsync(cancellationToken);

            return response.Items != null && response.Items.Count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Fetches all videos, livestreams, and shorts from the configured YouTube channel.
    /// Uses batching to minimize API quota consumption.
    /// </summary>
    public async Task<List<VideoItem>> GetAllVideosAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<VideoItem>();

        if (string.IsNullOrWhiteSpace(_settingsService.YouTubeApiKey) ||
            string.IsNullOrWhiteSpace(_settingsService.YouTubeChannelId))
        {
            return result;
        }

        using var youtube = CreateClient();

        // 1. Get uploads playlist ID for channel
        progress?.Report("Obtendo dados do canal...");
        var channelRequest = youtube.Channels.List("contentDetails");
        channelRequest.Id = _settingsService.YouTubeChannelId;
        var channelResponse = await channelRequest.ExecuteAsync(cancellationToken);

        string? uploadsListId = channelResponse.Items?.FirstOrDefault()?.ContentDetails?.RelatedPlaylists?.Uploads;
        if (string.IsNullOrWhiteSpace(uploadsListId))
        {
            // Fallback for standard channel ID to uploads playlist (UC... -> UU...)
            if (_settingsService.YouTubeChannelId.StartsWith("UC") && _settingsService.YouTubeChannelId.Length > 2)
            {
                uploadsListId = "UU" + _settingsService.YouTubeChannelId[2..];
            }
            else
            {
                return result;
            }
        }

        // 2. Fetch all video IDs from playlist items
        var rawVideoIds = new List<string>();
        string? nextPageToken = null;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var playlistRequest = youtube.PlaylistItems.List("snippet,contentDetails");
            playlistRequest.PlaylistId = uploadsListId;
            playlistRequest.MaxResults = 50;
            playlistRequest.PageToken = nextPageToken;

            var playlistResponse = await playlistRequest.ExecuteAsync(cancellationToken);
            if (playlistResponse.Items != null)
            {
                foreach (var item in playlistResponse.Items)
                {
                    string? videoId = item.ContentDetails?.VideoId ?? item.Snippet?.ResourceId?.VideoId;
                    if (!string.IsNullOrWhiteSpace(videoId) && !rawVideoIds.Contains(videoId))
                    {
                        rawVideoIds.Add(videoId);
                    }
                }
            }

            progress?.Report($"Coletando lista de vídeos... ({rawVideoIds.Count} encontrados)");
            nextPageToken = playlistResponse.NextPageToken;
        }
        while (!string.IsNullOrEmpty(nextPageToken));

        // 3. Batch video details by chunks of 50 to conserve API quota and enrich with duration/livestream details
        const int batchSize = 50;
        int processedCount = 0;

        for (int i = 0; i < rawVideoIds.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = rawVideoIds.Skip(i).Take(batchSize).ToList();
            var videoListRequest = youtube.Videos.List("snippet,contentDetails,liveStreamingDetails");
            videoListRequest.Id = string.Join(",", batch);

            var videoListResponse = await videoListRequest.ExecuteAsync(cancellationToken);

            if (videoListResponse.Items != null)
            {
                foreach (var v in videoListResponse.Items)
                {
                    var videoItem = MapToVideoItem(v);
                    result.Add(videoItem);
                }
            }

            processedCount += batch.Count;
            progress?.Report($"Detalhando vídeos ({processedCount}/{rawVideoIds.Count})...");
        }

        // Order by published date descending (newest first)
        return result.OrderByDescending(v => v.PublishedAt).ToList();
    }

    private static VideoItem MapToVideoItem(Video video)
    {
        string id = video.Id ?? string.Empty;
        string title = video.Snippet?.Title ?? "Sem título";
        string description = video.Snippet?.Description ?? string.Empty;
        DateTime publishedAt = video.Snippet?.PublishedAtDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow;

        string thumbUrl = video.Snippet?.Thumbnails?.Maxres?.Url
            ?? video.Snippet?.Thumbnails?.High?.Url
            ?? video.Snippet?.Thumbnails?.Medium?.Url
            ?? video.Snippet?.Thumbnails?.Default__?.Url
            ?? string.Empty;

        TimeSpan duration = TimeSpan.Zero;
        if (!string.IsNullOrEmpty(video.ContentDetails?.Duration))
        {
            try
            {
                duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
            }
            catch
            {
                duration = TimeSpan.Zero;
            }
        }

        VideoType type = VideoType.Upload;
        if (video.LiveStreamingDetails != null ||
            video.Snippet?.LiveBroadcastContent == "live" ||
            video.Snippet?.LiveBroadcastContent == "upcoming" ||
            video.Snippet?.LiveBroadcastContent == "completed")
        {
            type = VideoType.LiveStream;
        }
        else if (duration > TimeSpan.Zero && duration <= TimeSpan.FromSeconds(60))
        {
            type = VideoType.Short;
        }

        return new VideoItem(
            VideoId: id,
            Title: title,
            Description: description,
            ThumbnailUrl: thumbUrl,
            PublishedAt: publishedAt,
            Duration: duration,
            Type: type);
    }
}
