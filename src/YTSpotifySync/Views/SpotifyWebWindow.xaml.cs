using System;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace YTSpotifySync.Views;

public sealed partial class SpotifyWebWindow : Window
{
    private readonly string _uploadUrl;

    public SpotifyWebWindow(string url)
    {
        _uploadUrl = url;
        this.InitializeComponent();
        
        AppWindow.Resize(new SizeInt32(1100, 800));
        
        InitializeWebView(url);
    }

    private void GoHome_Click(object sender, RoutedEventArgs e)
    {
        MyWebView.Source = new Uri("https://creators.spotify.com/");
    }

    private void GoUpload_Click(object sender, RoutedEventArgs e)
    {
        MyWebView.Source = new Uri(_uploadUrl);
    }

    private async void InitializeWebView(string url)
    {
        try
        {
            await MyWebView.EnsureCoreWebView2Async();
            
            MyWebView.Source = new Uri(url);
        }
        catch (Exception)
        {
            // Em caso de erro, apenas avança.
        }
    }
}
