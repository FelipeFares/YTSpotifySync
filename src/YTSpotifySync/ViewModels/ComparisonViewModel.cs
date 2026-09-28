using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTSpotifySync.Models;
using YTSpotifySync.Services;
using YTSpotifySync.Views;

namespace YTSpotifySync.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    private readonly SyncEngine _syncEngine;

    public ObservableCollection<SyncResult> FilteredResults { get; } = new();

    [ObservableProperty]
    public partial int StatusFilterIndex { get; set; } = 1; // Default to "Pendentes" (most important view)

    [ObservableProperty]
    public partial int TypeFilterIndex { get; set; } = 0; // Default to "Todos os Tipos"

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int SyncedCount { get; set; }

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial bool HasItems { get; set; }

    public ComparisonViewModel(SyncEngine syncEngine)
    {
        _syncEngine = syncEngine;
        _syncEngine.ResultsUpdated += (s, e) => RefreshData();
        RefreshData();
    }

    public void RefreshData()
    {
        TotalCount = _syncEngine.CurrentResults.Count;
        PendingCount = _syncEngine.CurrentResults.Count(r => r.IsPending);
        SyncedCount = _syncEngine.CurrentResults.Count(r => r.IsSynced);

        ApplyFilter();
    }

    partial void OnStatusFilterIndexChanged(int value) => ApplyFilter();
    partial void OnTypeFilterIndexChanged(int value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public void ApplyFilter()
    {
        var query = _syncEngine.CurrentResults.AsEnumerable();

        // 1. Status Filter: 0 = Todos, 1 = Pendentes, 2 = Sincronizados
        if (StatusFilterIndex == 1)
        {
            query = query.Where(r => r.IsPending);
        }
        else if (StatusFilterIndex == 2)
        {
            query = query.Where(r => r.IsSynced);
        }

        // 2. Type Filter: 0 = Todos, 1 = Upload, 2 = LiveStream, 3 = Short
        if (TypeFilterIndex == 1)
        {
            query = query.Where(r => r.Video.Type == VideoType.Upload);
        }
        else if (TypeFilterIndex == 2)
        {
            query = query.Where(r => r.Video.Type == VideoType.LiveStream);
        }
        else if (TypeFilterIndex == 3)
        {
            query = query.Where(r => r.Video.Type == VideoType.Short);
        }

        // 3. Search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Video.Title.ToLowerInvariant().Contains(search) ||
                (r.MatchedEpisode != null && r.MatchedEpisode.Name.ToLowerInvariant().Contains(search)));
        }

        FilteredResults.Clear();
        foreach (var item in query)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
            FilteredResults.Add(item);
        }

        HasItems = FilteredResults.Count > 0;
        UpdateSelectedCount();
    }

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncResult.IsSelected))
        {
            UpdateSelectedCount();
        }
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = FilteredResults.Count(r => r.IsSelected);
    }

    [RelayCommand]
    public void SelectAll()
    {
        bool allSelected = FilteredResults.Count > 0 && FilteredResults.All(r => r.IsSelected);
        bool targetState = !allSelected;

        foreach (var item in FilteredResults)
        {
            item.IsSelected = targetState;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void SelectOnlyPending()
    {
        foreach (var item in FilteredResults)
        {
            item.IsSelected = item.IsPending;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void ToggleSyncStatus(SyncResult item)
    {
        if (item.Status == SyncStatus.Pending)
        {
            item.Status = SyncStatus.Synced;
        }
        else
        {
            item.Status = SyncStatus.Pending;
        }

        TotalCount = _syncEngine.CurrentResults.Count;
        PendingCount = _syncEngine.CurrentResults.Count(r => r.IsPending);
        SyncedCount = _syncEngine.CurrentResults.Count(r => r.IsSynced);

        ApplyFilter();
    }

    [RelayCommand]
    public void OpenInYouTube(SyncResult item)
    {
        try
        {
            Process.Start(new ProcessStartInfo(item.Video.VideoUrl) { UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    public void SendSelectedToDownloads()
    {
        var selected = FilteredResults.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) return;

        App.MainWindowInstance?.NavigateTo(typeof(DownloadPage), selected);
    }

    [RelayCommand]
    public void NavigateToDashboard()
    {
        App.MainWindowInstance?.NavigateTo(typeof(DashboardPage));
    }
}
