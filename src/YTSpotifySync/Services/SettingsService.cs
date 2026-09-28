using System;
using System.IO;
using System.Text.Json;

namespace YTSpotifySync.Services;

public class SettingsModel
{
    public string YouTubeApiKey { get; set; } = "";
    public string YouTubeChannelId { get; set; } = "UCsF18bQeCiSw04faoERqGug";
    public string SpotifyClientId { get; set; } = "";
    public string SpotifyClientSecret { get; set; } = "";
    public string SpotifyShowId { get; set; } = "3Tx5j7earRSezANSH46i68";
    public string YtDlpPath { get; set; } = "yt-dlp";
    public string DownloadDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads",
        "YTSpotifySync");
}

public class SettingsService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YTSpotifySync");

    private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");

    private SettingsModel _settings = new();

    public string YouTubeApiKey
    {
        get => _settings.YouTubeApiKey;
        set => _settings.YouTubeApiKey = value;
    }

    public string YouTubeChannelId
    {
        get => _settings.YouTubeChannelId;
        set => _settings.YouTubeChannelId = value;
    }

    public string SpotifyClientId
    {
        get => _settings.SpotifyClientId;
        set => _settings.SpotifyClientId = value;
    }

    public string SpotifyClientSecret
    {
        get => _settings.SpotifyClientSecret;
        set => _settings.SpotifyClientSecret = value;
    }

    public string SpotifyShowId
    {
        get => _settings.SpotifyShowId;
        set => _settings.SpotifyShowId = value;
    }

    public string YtDlpPath
    {
        get => _settings.YtDlpPath;
        set => _settings.YtDlpPath = value;
    }

    public string DownloadDirectory
    {
        get => _settings.DownloadDirectory;
        set => _settings.DownloadDirectory = value;
    }

    public SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<SettingsModel>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
            else
            {
                Save(); // Save default values
            }
        }
        catch (Exception)
        {
            _settings = new SettingsModel();
        }
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception)
        {
            // Log or ignore during save
        }
    }
}
