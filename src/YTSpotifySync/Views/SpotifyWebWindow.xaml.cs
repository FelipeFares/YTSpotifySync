using System;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace YTSpotifySync.Views;

public sealed partial class SpotifyWebWindow : Window
{
    public SpotifyWebWindow(string url)
    {
        this.InitializeComponent();
        
        AppWindow.Resize(new SizeInt32(1100, 800));
        
        InitializeWebView(url);
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
