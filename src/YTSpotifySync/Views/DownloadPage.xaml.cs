using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YTSpotifySync.Models;
using YTSpotifySync.ViewModels;

namespace YTSpotifySync.Views;

public sealed partial class DownloadPage : Page
{
    public DownloadViewModel ViewModel { get; }

    public DownloadPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DownloadViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IEnumerable<SyncResult> items)
        {
            ViewModel.EnqueueItems(items);
        }
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DownloadTask task })
        {
            ViewModel.OpenFile(task);
        }
    }

    private void OpenSpotify_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSpotifyUpload();
    }

    private void RemoveTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DownloadTask task })
        {
            ViewModel.RemoveTask(task);
        }
    }

    private void GoToComparison_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance?.NavigateTo(typeof(ComparisonPage));
    }
}
