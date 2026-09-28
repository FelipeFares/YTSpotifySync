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

    private async void OpenSpotify_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DownloadTask task })
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText($"{task.Video.Title}\n\n{task.Video.Description}");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            await ViewModel.OpenSpotifyUploadAsync(task);
        }
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
