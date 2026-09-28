using System;
using System.Diagnostics;
using System.Threading.Tasks;
using YTSpotifySync.Views;

namespace YTSpotifySync.Services;

public class SpotifyAutomationService
{
    /// <summary>
    /// Opens the Spotify for Creators episode upload page using the embedded WebView2.
    /// This persists the session natively inside the app without needing to re-login every time.
    /// </summary>
    public async Task OpenSpotifyUploadPageAsync(string showId, Models.DownloadTask? task = null)
    {
        string creatorId = showId == "0zU6hZslboizglsSm3VHnm" ? "3Tx5j7earRSezANSH46i68" : showId;
        string url = string.IsNullOrWhiteSpace(creatorId)
            ? "https://creators.spotify.com/dash"
            : $"https://creators.spotify.com/dash/show/{creatorId}/episode/new";

        try
        {
            // O WebView2 precisa ser inicializado na thread de UI principal,
            // e como OpenSpotifyUploadPageAsync é chamado a partir de um Command
            // de View, nós já estamos nela!
            var window = new SpotifyWebWindow(url);
            window.Activate();
            
            await Task.CompletedTask; // just to keep signature async for now
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open WebView2 window: {ex.Message}");
            BrowserLauncher.OpenSpotifyUploadPage(showId);
        }
    }
}
