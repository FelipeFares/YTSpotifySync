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

        // This JS simulates user typing by getting the input element, setting value, and triggering React synthetic events
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

            // Acha os campos - a estrutura do Spotify pode variar, geralmente é input para título e textarea ou div(draft-js) para descrição
            const inputs = document.querySelectorAll('input[type="text"], textarea');
            if (inputs.length >= 1) {
                const titleInput = inputs[0];
                setNativeValue(titleInput, {{title}});
                titleInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (inputs.length >= 2) {
                const descInput = inputs[1];
                setNativeValue(descInput, {{desc}});
                descInput.dispatchEvent(new Event('input', { bubbles: true }));
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
