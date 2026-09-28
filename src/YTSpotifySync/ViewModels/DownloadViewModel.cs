using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTSpotifySync.Models;
using YTSpotifySync.Services;

namespace YTSpotifySync.ViewModels;

public partial class DownloadViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly DownloadService _downloadService;
    private readonly SyncEngine _syncEngine;

    public ObservableCollection<DownloadTask> Queue { get; } = new();

    [ObservableProperty]
    public partial bool IsDownloadingAny { get; set; }

    [ObservableProperty]
    public partial int TotalInQueue { get; set; }

    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    public partial string GlobalStatusMessage { get; set; } = "Fila pronta.";

    [ObservableProperty]
    public partial bool HasItems { get; set; }

    public DownloadViewModel(
        SettingsService settingsService,
        DownloadService downloadService,
        SyncEngine syncEngine)
    {
        _settingsService = settingsService;
        _downloadService = downloadService;
        _syncEngine = syncEngine;
    }

    public void EnqueueItems(IEnumerable<SyncResult> items)
    {
        foreach (var syncResult in items)
        {
            // Avoid adding duplicates that are already queued
            if (!Queue.Any(q => q.Video.VideoId == syncResult.Video.VideoId))
            {
                var task = new DownloadTask(syncResult.Video);
                Queue.Add(task);
            }
        }

        UpdateCounters();
    }

    private void UpdateCounters()
    {
        TotalInQueue = Queue.Count;
        CompletedCount = Queue.Count(t => t.IsCompleted);
        HasItems = Queue.Count > 0;
    }

    [RelayCommand]
    public async Task StartAllAsync()
    {
        if (IsDownloadingAny) return;

        IsDownloadingAny = true;
        GlobalStatusMessage = "Processando fila de downloads...";

        string outputDir = _settingsService.DownloadDirectory;

        try
        {
            foreach (var task in Queue.Where(t => !t.IsCompleted).ToList())
            {
                task.IsActive = true;
                task.Status = "Baixando...";
                task.ErrorMessage = null;
                task.Cts = new CancellationTokenSource();

                // Download thumbnail in parallel or first
                try
                {
                    task.ThumbnailPath = await _downloadService.DownloadThumbnailAsync(
                        task.Video.ThumbnailUrl, outputDir, task.Video.VideoId, task.Cts.Token);
                }
                catch { }

                var progress = new Progress<double>(pct =>
                {
                    task.Progress = pct;
                    task.Status = $"Baixando: {pct:F1}%";
                });

                var statusProgress = new Progress<string>(s =>
                {
                    // keep status updated with current detail
                });

                try
                {
                    string finalPath = await _downloadService.DownloadVideoAsync(
                        task.Video, outputDir, progress, statusProgress, task.Cts.Token);

                    task.OutputPath = finalPath;
                    task.IsCompleted = true;
                    task.IsActive = false;
                    task.Status = "Concluído";
                    task.Progress = 100.0;

                    // Update SyncEngine status so comparison reflects download ready
                    _syncEngine.UpdateStatus(task.Video.VideoId, SyncStatus.Ready);
                }
                catch (OperationCanceledException)
                {
                    task.Status = "Cancelado";
                    task.IsActive = false;
                }
                catch (Exception ex)
                {
                    task.IsFailed = true;
                    task.IsActive = false;
                    task.Status = "Erro no download";
                    task.ErrorMessage = ex.Message;
                }

                UpdateCounters();
            }

            GlobalStatusMessage = $"Downloads finalizados. {CompletedCount} de {TotalInQueue} vídeos concluídos.";
        }
        finally
        {
            IsDownloadingAny = false;
        }
    }

    [RelayCommand]
    public void CancelTask(DownloadTask task)
    {
        task.Cts?.Cancel();
        task.Status = "Cancelado";
        task.IsActive = false;
    }

    [RelayCommand]
    public void OpenFile(DownloadTask task)
    {
        if (!string.IsNullOrEmpty(task.OutputPath))
        {
            BrowserLauncher.OpenFileInFolder(task.OutputPath);
        }
        else
        {
            BrowserLauncher.OpenFolder(_settingsService.DownloadDirectory);
        }
    }

    [RelayCommand]
    public void OpenSpotifyUpload()
    {
        BrowserLauncher.OpenSpotifyUploadPage(_settingsService.SpotifyShowId);
    }

    [RelayCommand]
    public void OpenDownloadFolder()
    {
        BrowserLauncher.OpenFolder(_settingsService.DownloadDirectory);
    }

    [RelayCommand]
    public void RemoveTask(DownloadTask task)
    {
        CancelTask(task);
        Queue.Remove(task);
        UpdateCounters();
    }

    [RelayCommand]
    public void ClearCompleted()
    {
        var completed = Queue.Where(t => t.IsCompleted).ToList();
        foreach (var task in completed)
        {
            Queue.Remove(task);
        }
        UpdateCounters();
    }
}
