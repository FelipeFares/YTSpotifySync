using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YTSpotifySync.Views;

namespace YTSpotifySync;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        if (AppWindow.TitleBar != null)
        {
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        // Navigate to default page
        NavFrame.Navigate(typeof(DashboardPage));
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            Type targetPage = item.Tag switch
            {
                "dashboard" => typeof(DashboardPage),
                "comparison" => typeof(ComparisonPage),
                "downloads" => typeof(DownloadPage),
                _ => typeof(DashboardPage)
            };

            NavFrame.Navigate(targetPage);
        }
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        NavFrame.Navigate(pageType, parameter);
        
        // Update selected navigation item
        if (pageType == typeof(DashboardPage))
            NavView.SelectedItem = NavView.MenuItems[0];
        else if (pageType == typeof(ComparisonPage))
            NavView.SelectedItem = NavView.MenuItems[1];
        else if (pageType == typeof(DownloadPage))
            NavView.SelectedItem = NavView.MenuItems[2];
        else if (pageType == typeof(SettingsPage))
            NavView.SelectedItem = NavView.SettingsItem;
    }
}
