using System;
using System.Collections.Generic;
using System.Linq;
using YTSpotifySync.Models;

namespace YTSpotifySync.Services;

public class SyncEngine
{
    /// <summary>
    /// Holds the most recently calculated sync results for the application.
    /// </summary>
    public List<SyncResult> CurrentResults { get; private set; } = new();

    /// <summary>
    /// Event fired when sync results are updated.
    /// </summary>
    public event EventHandler? ResultsUpdated;

    /// <summary>
    /// Compares a list of YouTube videos with Spotify episodes based on normalized titles.
    /// Uses exact normalized matching with fallback to substring containment.
    /// </summary>
    public List<SyncResult> Compare(List<VideoItem> videos, List<EpisodeItem> episodes)
    {
        var results = new List<SyncResult>();

        // Build dictionary of Spotify episodes by normalized name for fast O(1) lookup
        var epDict = new Dictionary<string, EpisodeItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var ep in episodes)
        {
            if (!string.IsNullOrWhiteSpace(ep.NormalizedName) && !epDict.ContainsKey(ep.NormalizedName))
            {
                epDict[ep.NormalizedName] = ep;
            }
        }

        foreach (var video in videos)
        {
            EpisodeItem? matched = null;

            // 1. Direct match on normalized title
            if (!string.IsNullOrWhiteSpace(video.NormalizedTitle) && epDict.TryGetValue(video.NormalizedTitle, out var exactMatch))
            {
                matched = exactMatch;
            }
            else if (!string.IsNullOrWhiteSpace(video.NormalizedTitle) && video.NormalizedTitle.Length >= 5)
            {
                // 2. Fallback: Check if one contains the other (e.g. video title with "ao vivo" tag removed or added)
                matched = episodes.FirstOrDefault(e =>
                    !string.IsNullOrWhiteSpace(e.NormalizedName) &&
                    (e.NormalizedName.Contains(video.NormalizedTitle) || video.NormalizedTitle.Contains(e.NormalizedName)));
            }

            var status = matched != null ? SyncStatus.Synced : SyncStatus.Pending;
            results.Add(new SyncResult(video, matched, status));
        }

        CurrentResults = results;
        ResultsUpdated?.Invoke(this, EventArgs.Empty);

        return results;
    }

    /// <summary>
    /// Updates status of a video in the current results.
    /// </summary>
    public void UpdateStatus(string videoId, SyncStatus newStatus)
    {
        var item = CurrentResults.FirstOrDefault(r => r.Video.VideoId == videoId);
        if (item != null)
        {
            item.Status = newStatus;
            ResultsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
