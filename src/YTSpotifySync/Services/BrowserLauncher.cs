using System;
using System.Diagnostics;
using System.IO;

namespace YTSpotifySync.Services;

public static class BrowserLauncher
{
    /// <summary>
    /// Opens the Spotify for Creators episode upload page for the given show ID in the default browser.
    /// </summary>
    public static void OpenSpotifyUploadPage(string showId)
    {
        string url = string.IsNullOrWhiteSpace(showId)
            ? "https://creators.spotify.com/dash"
            : $"https://creators.spotify.com/dash/show/{showId}/episode/new";

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open browser URL {url}: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the given folder in Windows File Explorer.
    /// </summary>
    public static void OpenFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open folder {folderPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects and highlights the specified file in Windows File Explorer.
    /// </summary>
    public static void OpenFileInFolder(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            }
            else
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    OpenFolder(dir);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to select file {filePath}: {ex.Message}");
        }
    }
}
