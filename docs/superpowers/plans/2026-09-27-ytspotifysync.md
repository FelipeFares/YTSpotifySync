# YTSpotifySync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Build a WinUI 3 Windows application that syncs YouTube channel videos to a Spotify for Creators podcast by identifying unmatched content, downloading video files, and directing the user to the Spotify upload page.

**Architecture:** MVVM app with CommunityToolkit.Mvvm, three service layers (YouTube API, Spotify API, yt-dlp download), a sync engine for title-based matching, and four WinUI 3 pages (Dashboard, Comparison, Downloads, Settings). No local database — all comparisons are computed at runtime.

**Tech Stack:** .NET 10, WinUI 3 (Windows App SDK), CommunityToolkit.Mvvm, Google.Apis.YouTube.v3, SpotifyAPI.Web, yt-dlp, System.Text.Json

**Spec:** `docs/superpowers/specs/2026-09-27-ytspotifysync-design.md`

## Global Constraints

- .NET 10 (SDK 10.0.401) — `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`
- WinUI 3 via Windows App SDK — packaged app model
- UI language: Portuguese (pt-BR) — all user-facing strings in Portuguese
- YouTube channel ID: `UCsF18bQeCiSw04faoERqGug`
- Spotify show ID: `3Tx5j7earRSezANSH46i68`
- No local database; no automated Spotify upload
- yt-dlp + ffmpeg required at runtime for downloads
- All async operations must use CancellationToken

## Review Focus

1. **YouTube API quota exhaustion** — Enriching each video with `Videos.List` costs 1 unit per call; batching by 50 IDs per call avoids hitting the 10,000-unit daily quota for channels with hundreds of videos.
2. **Title matching false negatives** — Titles edited on one platform after upload (e.g., adding "[LIVE]" prefix only on YouTube) will cause the sync engine to report them as pending; the comparison view should let the user manually mark items as synced.
3. **yt-dlp process hang** — A download that stalls (no stdout for >60s) should be killed and reported as failed, not left running indefinitely.
4. **Spotify Client Credentials scope** — Client Credentials flow only accesses public data; if the show is not public, the user will see zero episodes with no clear error.
5. **Large video download disk space** — Downloading a 3-hour live stream at best quality can exceed 10GB; the app should show estimated file size before starting download.

---

### Task 1: Scaffold WinUI 3 project + solution structure
**GitHub Issue:** #1

**Files:**
- Create: `YTSpotifySync.sln`
- Create: `src/YTSpotifySync/YTSpotifySync.csproj`
- Create: `src/YTSpotifySync/App.xaml`, `src/YTSpotifySync/App.xaml.cs`
- Create: `src/YTSpotifySync/MainWindow.xaml`, `src/YTSpotifySync/MainWindow.xaml.cs`
- Create: placeholder pages for Dashboard, Comparison, Downloads, Settings

**Interfaces:**
- Produces: Solution that builds and runs, `MainWindow` with `NavigationView` hosting 4 page stubs, DI container in `App.xaml.cs` with `IServiceProvider`

- [x] **Step 1: Create the WinUI 3 solution**

Run: `dotnet new winui3 -n YTSpotifySync -o src/YTSpotifySync --framework net10.0-windows10.0.19041.0`
Then: `dotnet new sln -n YTSpotifySync` at repo root, `dotnet sln add src/YTSpotifySync/YTSpotifySync.csproj`

- [x] **Step 2: Add NuGet packages**

```bash
cd src/YTSpotifySync
dotnet add package CommunityToolkit.Mvvm
dotnet add package CommunityToolkit.WinUI.Controls.DataGrid
dotnet add package Google.Apis.YouTube.v3
dotnet add package SpotifyAPI.Web
dotnet add package Microsoft.Extensions.DependencyInjection
```

- [x] **Step 3: Create folder structure**

Create empty directories: `Models/`, `Services/`, `ViewModels/`, `Views/`, `Helpers/`, `Assets/`

- [x] **Step 4: Create placeholder pages**

Create four XAML pages in `Views/`: `DashboardPage.xaml`, `ComparisonPage.xaml`, `DownloadPage.xaml`, `SettingsPage.xaml` — each with a `Page` containing a `TextBlock` placeholder.

- [x] **Step 5: Implement MainWindow with NavigationView**

`MainWindow.xaml`: `NavigationView` with 4 `NavigationViewItem`s (Dashboard, Comparação, Downloads, Configurações). Frame as content. Code-behind handles `SelectionChanged` to navigate the Frame.

- [x] **Step 6: Configure DI container in App.xaml.cs**

Register all services and view models in `IServiceCollection`. Store the `IServiceProvider` as a static property `App.Services`. Views resolve their ViewModels from DI.

- [x] **Step 7: Build and run**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: Build succeeds, app launches showing NavigationView with 4 tabs.

- [x] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: scaffold WinUI 3 project with NavigationView and DI"
```

---

### Task 2: Models + Enums + TitleNormalizer
**GitHub Issue:** #2

**Files:**
- Create: `src/YTSpotifySync/Models/VideoItem.cs`
- Create: `src/YTSpotifySync/Models/EpisodeItem.cs`
- Create: `src/YTSpotifySync/Models/SyncResult.cs`
- Create: `src/YTSpotifySync/Models/Enums.cs`
- Create: `src/YTSpotifySync/Helpers/TitleNormalizer.cs`

**Interfaces:**
- Produces: `VideoItem`, `EpisodeItem`, `SyncResult` records; `VideoType` and `SyncStatus` enums; `TitleNormalizer.Normalize(string) -> string`

- [x] **Step 1: Create Enums.cs**

```csharp
namespace YTSpotifySync.Models;
public enum VideoType { Upload, LiveStream, Short }
public enum SyncStatus { Synced, Pending, Downloading, Ready }
```

- [x] **Step 2: Create VideoItem.cs**

Record with properties: `string VideoId`, `string Title`, `string Description`, `string ThumbnailUrl`, `DateTime PublishedAt`, `TimeSpan Duration`, `VideoType Type`. Computed property `NormalizedTitle => TitleNormalizer.Normalize(Title)`.

- [x] **Step 3: Create EpisodeItem.cs**

Record with properties: `string EpisodeId`, `string Name`, `string Description`, `string ImageUrl`, `DateTime ReleaseDate`, `TimeSpan Duration`. Computed property `NormalizedName => TitleNormalizer.Normalize(Name)`.

- [x] **Step 4: Create SyncResult.cs**

Record: `VideoItem Video`, `EpisodeItem? MatchedEpisode`, `SyncStatus Status`.

- [x] **Step 5: Implement TitleNormalizer.Normalize(string input) -> string in Helpers/TitleNormalizer.cs**

Normalize: lowercase, `string.Normalize(NormalizationForm.FormD)` to decompose accents, strip non-letter/non-digit/non-space via regex, collapse whitespace, trim.

- [x] **Step 6: Build**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: PASS

- [x] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add data models, enums, and TitleNormalizer"
```

---

### Task 3: SettingsService + SettingsPage
**GitHub Issue:** #3

**Files:**
- Create: `src/YTSpotifySync/Services/SettingsService.cs`
- Create: `src/YTSpotifySync/ViewModels/SettingsViewModel.cs`
- Modify: `src/YTSpotifySync/Views/SettingsPage.xaml` (replace placeholder)
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Produces: `SettingsService` with `Load()`, `Save()`, properties for all config fields; `SettingsViewModel` with two-way bindings and `SaveCommand`
- Consumed by: Task 4 (YouTubeService reads API key), Task 5 (SpotifyService reads credentials), Task 8 (DownloadService reads paths)

- [x] **Step 1: Implement SettingsService.cs**

Class with properties: `YouTubeApiKey`, `YouTubeChannelId` (default `UCsF18bQeCiSw04faoERqGug`), `SpotifyClientId`, `SpotifyClientSecret`, `SpotifyShowId` (default `3Tx5j7earRSezANSH46i68`), `YtDlpPath`, `DownloadDirectory` (default `%USERPROFILE%/Downloads/YTSpotifySync`). `Load()` reads from `%APPDATA%/YTSpotifySync/settings.json` via `System.Text.Json`. `Save()` writes back. Auto-creates directory/file if missing.

- [x] **Step 2: Implement SettingsViewModel.cs**

`ObservableObject` with `[ObservableProperty]` for each setting. `[RelayCommand] Save()` calls `SettingsService.Save()`. `[RelayCommand] BrowseYtDlp()` opens file picker. `[RelayCommand] BrowseDownloadDir()` opens folder picker.

- [x] **Step 3: Build SettingsPage.xaml**

Fluent Design form: `StackPanel` with labeled `TextBox`es for each field, `PasswordBox` for Spotify secret, `Button`s for browse and save. Group into sections: YouTube, Spotify, Downloads.

- [x] **Step 4: Register in DI and wire up**

Add `SettingsService` as singleton in `App.xaml.cs`. Add `SettingsViewModel` as transient. `SettingsPage.xaml.cs` resolves `SettingsViewModel` from `App.Services`.

- [x] **Step 5: Build and test manually**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: Settings page renders, values save/load from JSON file.

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add SettingsService and SettingsPage with JSON persistence"
```

---

### Task 4: YouTubeService
**GitHub Issue:** #4

**Files:**
- Create: `src/YTSpotifySync/Services/YouTubeService.cs`
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Consumes: `SettingsService.YouTubeApiKey`, `SettingsService.YouTubeChannelId`, `VideoItem`, `VideoType`
- Produces: `YouTubeService.GetAllVideosAsync(CancellationToken) -> Task<List<VideoItem>>`, `YouTubeService.TestConnectionAsync() -> Task<bool>`

- [x] **Step 1: Implement YouTubeService.GetAllVideosAsync**

1. Create `YouTubeService` with injected `SettingsService`.
2. `GetAllVideosAsync`: use `Google.Apis.YouTube.v3` — `Channels.List("contentDetails")` with `Id = channelId` to get `UploadsPlaylistId`.
3. Paginate `PlaylistItems.List("snippet")` with `MaxResults=50` and `NextPageToken` to collect all video IDs.
4. Batch video IDs in groups of 50, call `Videos.List("snippet,contentDetails,liveStreamingDetails")` for each batch.
5. Map to `VideoItem`: parse `ContentDetails.Duration` (ISO 8601) to `TimeSpan`, check `LiveStreamingDetails != null` for `LiveStream`, `Duration < 60s` for `Short`, else `Upload`.

- [x] **Step 2: Implement TestConnectionAsync**

Call `Channels.List("snippet")` with the configured channel ID. Return true if response has items, false otherwise. Catch `GoogleApiException` and return false.

- [x] **Step 3: Register in DI**

Add `YouTubeService` as singleton in `App.xaml.cs`.

- [x] **Step 4: Build**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add YouTubeService with full video listing and classification"
```

---

### Task 5: SpotifyService
**GitHub Issue:** #5

**Files:**
- Create: `src/YTSpotifySync/Services/SpotifyService.cs`
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Consumes: `SettingsService.SpotifyClientId`, `SettingsService.SpotifyClientSecret`, `SettingsService.SpotifyShowId`, `EpisodeItem`
- Produces: `SpotifyService.GetAllEpisodesAsync(CancellationToken) -> Task<List<EpisodeItem>>`, `SpotifyService.TestConnectionAsync() -> Task<bool>`

- [x] **Step 1: Implement SpotifyService.GetAllEpisodesAsync**

1. Create `SpotifyService` with injected `SettingsService`.
2. Authenticate via `ClientCredentialsRequest` using `SpotifyClientConfig.CreateDefault()`.
3. Call `spotify.Shows.GetEpisodes(showId, new ShowEpisodesRequest { Limit = 50 })`.
4. Paginate with `await spotify.Paginate(...).ToListAsync()`.
5. Map each `SimpleEpisode` to `EpisodeItem`: `Id`, `Name`, `Description`, `Images[0].Url`, `ReleaseDate` parsed to DateTime, `DurationMs` to TimeSpan.

- [x] **Step 2: Implement TestConnectionAsync**

Call `spotify.Shows.Get(showId)`. Return true if successful, false on `APIException`.

- [x] **Step 3: Register in DI, connect Test Connection buttons in SettingsViewModel**

Wire `SettingsViewModel.TestYouTubeCommand` and `TestSpotifyCommand` to call the respective service's `TestConnectionAsync()` and show success/failure.

- [x] **Step 4: Build**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add SpotifyService with episode listing and test connection"
```

---

### Task 6: SyncEngine + DashboardPage
**GitHub Issue:** #6

**Files:**
- Create: `src/YTSpotifySync/Services/SyncEngine.cs`
- Create: `src/YTSpotifySync/ViewModels/DashboardViewModel.cs`
- Modify: `src/YTSpotifySync/Views/DashboardPage.xaml` (replace placeholder)
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Consumes: `YouTubeService.GetAllVideosAsync()`, `SpotifyService.GetAllEpisodesAsync()`, `VideoItem`, `EpisodeItem`, `SyncResult`, `TitleNormalizer`
- Produces: `SyncEngine.Compare(List<VideoItem>, List<EpisodeItem>) -> List<SyncResult>`; `DashboardViewModel` with counters and `RefreshCommand`

- [x] **Step 1: Implement SyncEngine.Compare**

For each `VideoItem`, search `episodes` for one whose `NormalizedName` equals `video.NormalizedTitle`. If found: `SyncResult(video, episode, Synced)`. Else: `SyncResult(video, null, Pending)`. Use a `Dictionary<string, EpisodeItem>` keyed by normalized name for O(1) lookup.

- [x] **Step 2: Implement DashboardViewModel**

`ObservableObject` with `[ObservableProperty]` for `YouTubeVideoCount`, `SpotifyEpisodeCount`, `PendingCount`, `IsLoading`, `ErrorMessage`, `LastSyncTime`. `[RelayCommand] async Refresh()`: call both services in parallel via `Task.WhenAll`, feed results to `SyncEngine.Compare`, update counters. Store the full `List<SyncResult>` for navigation to ComparisonPage.

- [x] **Step 3: Build DashboardPage.xaml**

Three stat cards in a horizontal `StackPanel`: YouTube icon + count (red), Spotify icon + count (green), Pending icon + count (orange). Below: "Sincronizar Agora" button. Below that: last sync time. Loading overlay with `ProgressRing`.

- [x] **Step 4: Register in DI and wire up**

- [x] **Step 5: Build and run**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: Dashboard shows, clicking Sync calls APIs and displays counts.

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add SyncEngine and DashboardPage with live counters"
```

---

### Task 7: ComparisonPage
**GitHub Issue:** #7

**Files:**
- Create: `src/YTSpotifySync/ViewModels/ComparisonViewModel.cs`
- Modify: `src/YTSpotifySync/Views/ComparisonPage.xaml` (replace placeholder)
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Consumes: `SyncEngine.Compare()`, `YouTubeService`, `SpotifyService`, `SyncResult`
- Produces: `ComparisonViewModel` with filtered `ObservableCollection<SyncResult>`, `SelectedItems`, and `DownloadSelectedCommand`

- [x] **Step 1: Implement ComparisonViewModel**

`ObservableObject` with `ObservableCollection<SyncResult> AllResults` and `ObservableCollection<SyncResult> FilteredResults`. Properties: `SelectedFilter` (All/Pending/Synced), `SelectedTypeFilter` (All/Upload/LiveStream/Short), `SearchText`. On any filter change, re-compute `FilteredResults`. Multi-select support via `ObservableCollection<SyncResult> SelectedItems`. `[RelayCommand] DownloadSelected()`: navigates to DownloadPage passing selected items.

- [x] **Step 2: Build ComparisonPage.xaml**

Filter bar: three toggle buttons (Todos/Pendentes/Sincronizados) + ComboBox for type filter + SearchBox. ListView with `DataTemplate`: thumbnail (48x48), title, type icon badge, YouTube date, Spotify status badge (green check or orange clock), checkbox for selection. Bottom bar: "Download Selecionados ({count})" button.

- [x] **Step 3: Register in DI and wire navigation from Dashboard**

When user clicks "Sincronizar" on Dashboard, after sync completes, auto-navigate to ComparisonPage with results.

- [x] **Step 4: Build and run**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: Comparison page shows filtered list of videos with status.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add ComparisonPage with filters and multi-select"
```

---

### Task 8: DownloadService + DownloadPage
**GitHub Issue:** #8

**Files:**
- Create: `src/YTSpotifySync/Services/DownloadService.cs`
- Create: `src/YTSpotifySync/Services/BrowserLauncher.cs`
- Create: `src/YTSpotifySync/ViewModels/DownloadViewModel.cs`
- Create: `src/YTSpotifySync/Models/DownloadTask.cs`
- Modify: `src/YTSpotifySync/Views/DownloadPage.xaml` (replace placeholder)
- Modify: `src/YTSpotifySync/App.xaml.cs` (register in DI)

**Interfaces:**
- Consumes: `SettingsService.YtDlpPath`, `SettingsService.DownloadDirectory`, `SettingsService.SpotifyShowId`, `VideoItem`
- Produces: `DownloadService.DownloadVideoAsync(string videoId, string outputDir, IProgress<double>, CancellationToken) -> Task<string>`, `DownloadService.DownloadThumbnailAsync(string url, string outputDir) -> Task<string>`, `BrowserLauncher.OpenSpotifyUploadPage(string showId)`

- [x] **Step 1: Create DownloadTask model**

Record/class: `VideoItem Video`, `double Progress` (0-100), `string Status` (Queued/Downloading/Completed/Failed/Cancelled), `string? OutputPath`, `string? ErrorMessage`. Implements `INotifyPropertyChanged`.

- [x] **Step 2: Implement DownloadService**

`DownloadVideoAsync`: build `ProcessStartInfo` for yt-dlp with args `-f "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]" --merge-output-format mp4 --newline -o "{outputDir}/%(title)s.%(ext)s" "https://youtube.com/watch?v={videoId}"`. Read stdout line-by-line, parse `[download]  XX.X%` pattern, report to `IProgress<double>`. Handle `CancellationToken` by killing the process. Timeout: kill if no output for 120s. Return output file path.

`DownloadThumbnailAsync`: use `HttpClient` to download thumbnail to `{outputDir}/{videoId}_thumb.jpg`.

- [x] **Step 3: Implement BrowserLauncher**

`OpenSpotifyUploadPage(showId)`: call `Process.Start(new ProcessStartInfo("https://creators.spotify.com/dash/show/{showId}/episode/new") { UseShellExecute = true })`.

- [x] **Step 4: Implement DownloadViewModel**

`ObservableCollection<DownloadTask> Queue`. `[RelayCommand] async StartDownloads()`: iterate queue sequentially, update each task's progress. `[RelayCommand] OpenInSpotify()`: calls `BrowserLauncher`. `[RelayCommand] OpenFolder(DownloadTask)`: opens containing folder in Explorer. `[RelayCommand] Cancel(DownloadTask)`: triggers cancellation.

- [x] **Step 5: Build DownloadPage.xaml**

ListView of `DownloadTask` items with `DataTemplate`: thumbnail, title, progress bar, percentage text, status badge, action buttons (Cancel/Open Folder/Open Spotify). Top: "Iniciar Downloads" and "Abrir Spotify Upload" global buttons. Summary text: "X de Y concluídos".

- [x] **Step 6: Register in DI, wire navigation from ComparisonPage**

"Download Selecionados" button creates `DownloadTask` instances and navigates to DownloadPage.

- [x] **Step 7: Build and run**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj`
Expected: Download page shows queue, downloads work with progress, Spotify opens in browser.

- [x] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add DownloadService, BrowserLauncher, and DownloadPage"
```

---

### Task 9: Polish — UI/UX, themes, icons, finalization
**GitHub Issue:** #9

**Files:**
- Modify: `src/YTSpotifySync/MainWindow.xaml` (Mica backdrop, title bar)
- Modify: `src/YTSpotifySync/App.xaml` (theme resources, global styles)
- Modify: all Views (animations, empty states, error states)
- Create: `README.md`
- Modify: `src/YTSpotifySync/Assets/` (app icon)

**Interfaces:**
- Consumes: all existing Views and ViewModels
- Produces: polished, production-ready UI

- [x] **Step 1: Apply Mica backdrop**

In `MainWindow.xaml.cs`, set `SystemBackdrop = new MicaBackdrop()`. Extend title bar into the window chrome.

- [x] **Step 2: Add navigation icons**

Use Segoe Fluent Icons for NavigationViewItems: Dashboard (Home), Comparação (ArrowRepeatAll), Downloads (Download), Configurações (Settings).

- [x] **Step 3: Add page transition animations**

Set `Frame.ContentTransitions` with `NavigationThemeTransition` for smooth page transitions.

- [x] **Step 4: Implement empty states**

For Dashboard (no data yet), ComparisonPage (no results), DownloadPage (empty queue): show friendly illustrations and "Comece por..." guidance messages.

- [x] **Step 5: Improve error messages**

All error messages in Portuguese. InfoBar control for non-blocking errors. ContentDialog for blocking errors (e.g., missing yt-dlp).

- [x] **Step 6: Write README.md**

Sections: Sobre, Requisitos (yt-dlp, ffmpeg, YouTube API key, Spotify credentials), Instalação, Uso, Screenshots.

- [x] **Step 7: Push everything and update GitHub issues**

```bash
git push origin main
```
Close all GitHub issues in the milestone.

- [x] **Step 8: Final build and smoke test**

Run: `dotnet build src/YTSpotifySync/YTSpotifySync.csproj -c Release`
Expected: Clean build, app runs, all pages navigate, settings save/load.
