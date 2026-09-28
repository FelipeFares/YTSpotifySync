using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YTSpotifySync.Models;

namespace YTSpotifySync.Services;

public partial class DownloadService
{
    private readonly SettingsService _settingsService;
    private readonly HttpClient _httpClient = new();

    [GeneratedRegex(@"\[download\]\s+(\d+(?:\.\d+)?)%")]
    private static partial Regex DownloadPercentRegex();

    [GeneratedRegex(@"\[(?:download|Merger|ffmpeg)\]\s+(?:Destination:\s+|Merging formats into\s+"")?([^""\r\n]+\.mp4)")]
    private static partial Regex OutputPathRegex();

    public DownloadService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Locates the yt-dlp executable from settings or system PATH.
    /// </summary>
    public string ResolveYtDlpPath()
    {
        string configured = _settingsService.YtDlpPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (File.Exists(configured))
                return configured;

            if (configured.Equals("yt-dlp", StringComparison.OrdinalIgnoreCase) ||
                configured.Equals("yt-dlp.exe", StringComparison.OrdinalIgnoreCase))
            {
                // Check if in PATH
                string? inPath = FindInPath("yt-dlp.exe");
                if (inPath != null) return inPath;
            }
        }

        // Check common locations
        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "yt-dlp.exe"),
            Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "yt-dlp", "yt-dlp.exe")
        ];

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        string? fallback = FindInPath("yt-dlp.exe");
        if (fallback != null) return fallback;

        return "yt-dlp"; // Fallback to PATH invocation
    }

    private static string? FindInPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var p in paths)
        {
            try
            {
                var full = Path.Combine(p.Trim(), fileName);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Downloads the thumbnail image of a video to the target output directory.
    /// </summary>
    public async Task<string?> DownloadThumbnailAsync(string thumbnailUrl, string outputDir, string videoId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(thumbnailUrl)) return null;

        try
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string targetFile = Path.Combine(outputDir, $"{videoId}_thumb.jpg");
            var bytes = await _httpClient.GetByteArrayAsync(thumbnailUrl, ct);
            await File.WriteAllBytesAsync(targetFile, bytes, ct);
            return targetFile;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads a YouTube video using yt-dlp with real-time percentage progress and cancellation support.
    /// </summary>
    public async Task<string> DownloadVideoAsync(
        VideoItem video,
        string outputDir,
        IProgress<double>? progress = null,
        IProgress<string>? statusProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string ytDlpPath = ResolveYtDlpPath();

        // Template ensures file has title and video ID: OutputDir/%(title)s [%(id)s].%(ext)s
        string outputTemplate = Path.Combine(outputDir, "%(title).100s [%(id)s].%(ext)s");
        string args = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" --merge-output-format mp4 --newline --no-playlist -o \"{outputTemplate}\" \"https://www.youtube.com/watch?v={video.VideoId}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        string detectedOutputPath = string.Empty;
        var tcs = new TaskCompletionSource<int>();

        process.OutputDataReceived += (s, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            string line = e.Data;
            statusProgress?.Report(line);

            // Match percentage
            var matchPercent = DownloadPercentRegex().Match(line);
            if (matchPercent.Success && double.TryParse(matchPercent.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
            {
                progress?.Report(pct);
            }

            // Detect target filename
            var matchFile = OutputPathRegex().Match(line);
            if (matchFile.Success)
            {
                detectedOutputPath = matchFile.Groups[1].Value.Trim().Trim('"');
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                statusProgress?.Report(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Não foi possível iniciar o processo yt-dlp.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch { }
            });

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
            {
                throw new Exception($"yt-dlp finalizou com código de erro {process.ExitCode}");
            }

            // Find downloaded file if not detected directly from stdout
            if (string.IsNullOrEmpty(detectedOutputPath) || !File.Exists(detectedOutputPath))
            {
                var files = Directory.GetFiles(outputDir, $"*[{video.VideoId}]*.mp4");
                if (files.Length > 0)
                {
                    detectedOutputPath = files[0];
                }
            }

            progress?.Report(100.0);
            return detectedOutputPath;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
            throw;
        }
    }
}
