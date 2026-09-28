namespace YTSpotifySync.Models;

public enum VideoType
{
    Upload,
    LiveStream,
    Short
}

public enum SyncStatus
{
    Synced,
    Pending,
    Downloading,
    Ready
}
