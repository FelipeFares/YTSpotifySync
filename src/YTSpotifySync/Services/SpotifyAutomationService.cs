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
        // Utilizando o link exato fornecido pelo usuário.
        string url = "https://creators.spotify.com/pod/show/0zU6hZslboizglsSm3VHnm/episode/wizard";

        try
        {
            var window = new SpotifyWebWindow(url, task);
            window.Activate();
            
            await Task.CompletedTask; 
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open WebView2 window: {ex.Message}");
            BrowserLauncher.OpenSpotifyUploadPage(showId);
        }
    }
}
