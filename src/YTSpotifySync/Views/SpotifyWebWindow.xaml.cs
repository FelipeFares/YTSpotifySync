using System;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace YTSpotifySync.Views;

public sealed partial class SpotifyWebWindow : Window
{
    private readonly string _uploadUrl;
    private readonly YTSpotifySync.Models.DownloadTask? _task;

    public SpotifyWebWindow(string url, YTSpotifySync.Models.DownloadTask? task = null)
    {
        _uploadUrl = url;
        _task = task;
        this.InitializeComponent();
        
        AppWindow.Resize(new SizeInt32(1100, 800));

        if (_task != null)
        {
            FillDataBtn.Visibility = Visibility.Visible;
        }
        
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

    private async void FillData_Click(object sender, RoutedEventArgs e)
    {
        if (_task == null || MyWebView.CoreWebView2 == null) return;

        // Escape JSON safely
        var title = System.Text.Json.JsonSerializer.Serialize(_task.Video.Title);
        var desc = System.Text.Json.JsonSerializer.Serialize(_task.Video.Description);

        // This JS simulates user typing by getting the exact input elements from Spotify's HTML
        string js = $$"""
        (function() {
            function setNativeValue(element, value) {
                const valueSetter = Object.getOwnPropertyDescriptor(element, 'value').set;
                const prototype = Object.getPrototypeOf(element);
                const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value').set;
                
                if (valueSetter && valueSetter !== prototypeValueSetter) {
                    prototypeValueSetter.call(element, value);
                } else {
                    valueSetter.call(element, value);
                }
            }

            // Title Input
            const titleInput = document.getElementById('title-input') || document.querySelector('input[name="title"]');
            if (titleInput) {
                setNativeValue(titleInput, {{title}});
                titleInput.dispatchEvent(new Event('input', { bubbles: true }));
            }

            // Description Input (Slate JS contenteditable div)
            const descInput = document.querySelector('div[role="textbox"][name="description"]');
            if (descInput) {
                descInput.focus();
                document.execCommand('selectAll', false, null);
                document.execCommand('insertText', false, {{desc}});
            }
        })();
        """;

        await MyWebView.CoreWebView2.ExecuteScriptAsync(js);
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
