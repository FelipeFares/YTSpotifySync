# YTSpotifySync — Design Specification

## Purpose

A Windows desktop application that helps a YouTube creator identify which videos from their YouTube channel have not yet been published as episodes on their Spotify for Creators podcast, download the video files, prepare metadata, and open the Spotify upload page for final manual submission.

## Target User

A single content creator who publishes videos (uploads, live streams, and Shorts) to YouTube and wants to cross-publish them as Video Podcast episodes on Spotify for Creators. The user uploads directly through the Spotify for Creators dashboard (no external RSS feed).

## Success Criteria

1. The user opens the app and sees a dashboard showing YouTube video count, Spotify episode count, and how many videos are missing from Spotify.
2. The user can browse a comparison list showing matched and unmatched content.
3. The user can select unmatched videos, download the full video file, and have the app open the Spotify for Creators upload page so they can drag-and-drop the file.
4. The entire flow — from opening the app to having a video file ready for Spotify upload — takes under 2 minutes per video (excluding download time).

## Constraints

- **.NET 10** with **WinUI 3 (Windows App SDK)** — the app runs only on Windows 10/11.
- **No local database** — comparisons are computed at runtime by querying both APIs.
- **No automated Spotify upload** — the Spotify for Creators platform has no public upload API. The app prepares everything and opens the browser for the user to finish.
- **yt-dlp** is required for video downloads. The app should detect it on PATH or allow the user to configure its location.
- **ffmpeg** must be available for yt-dlp to function.

---

## Architecture

### Layers

```
┌─────────────────────────────────────────────────┐
│                   WinUI 3 UI                    │
│   (Dashboard, Comparison, Download, Settings)   │
├─────────────────────────────────────────────────┤
│                  ViewModels                     │
│   (MVVM pattern via CommunityToolkit.Mvvm)      │
├─────────────────────────────────────────────────┤
│                   Services                      │
│  YouTubeService │ SpotifyService │ SyncEngine   │
│  DownloadService│ SettingsService│ BrowserLauncher│
├─────────────────────────────────────────────────┤
│                    Models                       │
│   VideoItem │ EpisodeItem │ SyncResult          │
└─────────────────────────────────────────────────┘
```

### MVVM Pattern

- **CommunityToolkit.Mvvm** for `ObservableObject`, `RelayCommand`, and source generators.
- ViewModels are injected into Views via `Microsoft.Extensions.DependencyInjection`.

---

## Components

### 1. Models

#### `VideoItem`
Represents a YouTube video.
- `string VideoId` — YouTube video ID
- `string Title` — Video title
- `string Description` — Video description
- `string ThumbnailUrl` — URL to the default thumbnail
- `DateTime PublishedAt` — Publication date
- `TimeSpan Duration` — Video duration
- `VideoType Type` — enum: `Upload`, `LiveStream`, `Short`
- `string NormalizedTitle` — lowercase, stripped of special chars, used for matching

#### `EpisodeItem`
Represents a Spotify podcast episode.
- `string EpisodeId` — Spotify episode ID
- `string Name` — Episode name
- `string Description` — Episode description
- `string ImageUrl` — Episode cover art URL
- `DateTime ReleaseDate` — Release date
- `TimeSpan Duration` — Episode duration
- `string NormalizedName` — lowercase, stripped of special chars, used for matching

#### `SyncResult`
The outcome of comparing YouTube videos with Spotify episodes.
- `VideoItem Video` — The YouTube video
- `EpisodeItem? MatchedEpisode` — The matched Spotify episode, or null if not found
- `SyncStatus Status` — enum: `Synced`, `Pending`, `Downloading`, `Ready`

### 2. Services

#### `YouTubeService`
- `Task<List<VideoItem>> GetAllVideosAsync(string channelId)`
  - Uses YouTube Data API v3 `Channels.List` to get the uploads playlist ID.
  - Paginates through `PlaylistItems.List` (50 per page) to get all video IDs.
  - Calls `Videos.List` with `snippet,contentDetails,liveStreamingDetails` to classify each video and get duration.
  - Classifies: `liveStreamingDetails != null` → LiveStream; `duration < 60s && aspect vertical` → Short; else → Upload.

#### `SpotifyService`
- `Task<List<EpisodeItem>> GetAllEpisodesAsync(string showId)`
  - Uses SpotifyAPI.Web with Client Credentials flow.
  - Calls `Shows.GetEpisodes` with pagination via `spotify.Paginate()`.

#### `SyncEngine`
- `List<SyncResult> Compare(List<VideoItem> videos, List<EpisodeItem> episodes)`
  - Normalizes titles: lowercase, remove accents, remove punctuation, trim whitespace.
  - For each video, searches for an episode whose normalized name matches.
  - Returns a `SyncResult` per video with `Synced` or `Pending` status.

#### `DownloadService`
- `Task<string> DownloadVideoAsync(string videoId, string outputDir, IProgress<double> progress)`
  - Invokes `yt-dlp` via `System.Diagnostics.Process`.
  - Arguments: `-f "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]" --merge-output-format mp4 -o "{outputDir}/%(title)s.%(ext)s" "https://youtube.com/watch?v={videoId}"`.
  - Parses stdout for progress percentage and reports via `IProgress<double>`.
  - Returns the path to the downloaded file.
- `Task<string> DownloadThumbnailAsync(string thumbnailUrl, string outputDir)`
  - Downloads the thumbnail image via HttpClient.

#### `SettingsService`
- Stores user configuration in `%APPDATA%/YTSpotifySync/settings.json`.
- Settings:
  - `string YouTubeApiKey`
  - `string YouTubeChannelId` (default: `UCsF18bQeCiSw04faoERqGug`)
  - `string SpotifyClientId`
  - `string SpotifyClientSecret`
  - `string SpotifyShowId` (default: `3Tx5j7earRSezANSH46i68`)
  - `string YtDlpPath` (default: searches PATH)
  - `string DownloadDirectory` (default: `%USERPROFILE%/Downloads/YTSpotifySync`)

#### `BrowserLauncher`
- `void OpenSpotifyUploadPage(string showId)`
  - Opens `https://creators.spotify.com/dash/show/{showId}/episode/new` in the default browser.

### 3. ViewModels

#### `DashboardViewModel`
- Properties: `int YouTubeVideoCount`, `int SpotifyEpisodeCount`, `int PendingCount`, `bool IsLoading`
- Commands: `RefreshCommand` — calls both services and runs SyncEngine.

#### `ComparisonViewModel`
- Properties: `ObservableCollection<SyncResult> Results`, filter by `SyncStatus`, search text
- Commands: `RefreshCommand`, `SelectForDownloadCommand`

#### `DownloadViewModel`
- Properties: `ObservableCollection<DownloadTask> Queue`, per-item progress, overall progress
- Commands: `StartDownloadCommand`, `OpenInSpotifyCommand`, `OpenFolderCommand`

#### `SettingsViewModel`
- Properties: bound to `SettingsService` fields
- Commands: `SaveCommand`, `TestYouTubeConnectionCommand`, `TestSpotifyConnectionCommand`, `BrowseYtDlpCommand`

### 4. Views (WinUI 3 XAML)

#### `MainWindow`
- NavigationView with 4 pages: Dashboard, Comparison, Downloads, Settings.
- Fluent Design with Mica backdrop and rounded corners.

#### `DashboardPage`
- Three stat cards (YouTube count, Spotify count, Pending) with icons.
- Quick-action button: "Sincronizar Agora".
- Recent activity list (last 5 comparisons).

#### `ComparisonPage`
- DataGrid / ListView with columns: Thumbnail, Title, Type (icon), YouTube Date, Spotify Status, Actions.
- Filter bar: All / Pending / Synced / Type filter.
- Multi-select with "Download Selected" button.

#### `DownloadPage`
- Queue of selected videos with individual progress bars.
- Per-item actions: Cancel, Open File, Open in Spotify.
- Global "Open Spotify Upload" button.

#### `SettingsPage`
- Form with labeled fields for all settings.
- "Test Connection" buttons for both APIs.
- File picker for yt-dlp path.
- Folder picker for download directory.

---

## Data Flow

### Sync Flow
1. User clicks "Sincronizar" on Dashboard.
2. `DashboardViewModel` calls `YouTubeService.GetAllVideosAsync()` and `SpotifyService.GetAllEpisodesAsync()` in parallel.
3. Both results fed to `SyncEngine.Compare()`.
4. Results displayed in ComparisonPage.

### Download Flow
1. User selects pending videos in ComparisonPage and clicks "Download Selected".
2. Selected items added to `DownloadViewModel.Queue`.
3. Navigation switches to DownloadPage.
4. Downloads execute sequentially (one at a time to avoid bandwidth issues).
5. On completion, "Open in Spotify" button becomes enabled per item.
6. Clicking it opens `https://creators.spotify.com/dash/show/{showId}/episode/new` in the browser.

---

## Error Handling

| Error | Handling |
|---|---|
| YouTube API key invalid/expired | Show inline error in Settings with link to Google Cloud Console |
| Spotify credentials invalid | Show inline error in Settings with link to Spotify Developer Dashboard |
| yt-dlp not found | Show warning banner with download link |
| Network failure during download | Retry button per item; yt-dlp supports resume |
| YouTube quota exceeded | Show error with estimated reset time (midnight Pacific) |

---

## UI Language

The application UI will be in **Portuguese (pt-BR)** since the user is Brazilian.

---

## File Structure

```
YTSpotifySync/
├── YTSpotifySync.sln
├── src/
│   └── YTSpotifySync/
│       ├── YTSpotifySync.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── Models/
│       │   ├── VideoItem.cs
│       │   ├── EpisodeItem.cs
│       │   ├── SyncResult.cs
│       │   └── Enums.cs
│       ├── Services/
│       │   ├── YouTubeService.cs
│       │   ├── SpotifyService.cs
│       │   ├── SyncEngine.cs
│       │   ├── DownloadService.cs
│       │   ├── SettingsService.cs
│       │   └── BrowserLauncher.cs
│       ├── ViewModels/
│       │   ├── DashboardViewModel.cs
│       │   ├── ComparisonViewModel.cs
│       │   ├── DownloadViewModel.cs
│       │   └── SettingsViewModel.cs
│       ├── Views/
│       │   ├── DashboardPage.xaml / .cs
│       │   ├── ComparisonPage.xaml / .cs
│       │   ├── DownloadPage.xaml / .cs
│       │   └── SettingsPage.xaml / .cs
│       ├── Helpers/
│       │   └── TitleNormalizer.cs
│       └── Assets/
│           └── (app icons)
└── docs/
    └── superpowers/
        ├── specs/
        └── plans/
```

---

## NuGet Dependencies

| Package | Purpose |
|---|---|
| `Microsoft.WindowsAppSDK` | WinUI 3 runtime |
| `Microsoft.Windows.SDK.BuildTools` | Windows SDK build support |
| `CommunityToolkit.Mvvm` | MVVM source generators and base classes |
| `CommunityToolkit.WinUI.Controls.DataGrid` | DataGrid control for comparison view |
| `Google.Apis.YouTube.v3` | YouTube Data API v3 client |
| `SpotifyAPI.Web` | Spotify Web API client |
| `Microsoft.Extensions.DependencyInjection` | IoC container |
| `System.Text.Json` | Settings serialization (built-in) |
