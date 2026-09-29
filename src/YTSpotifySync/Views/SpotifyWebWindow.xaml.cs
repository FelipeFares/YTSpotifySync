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
            try {
                // Title Input
                const titleInput = document.getElementById('title-input') || document.querySelector('input[name="title"]');
                if (titleInput) {
                    const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                    nativeInputValueSetter.call(titleInput, {{title}});
                    titleInput.dispatchEvent(new Event('input', { bubbles: true }));
                }
            } catch (e) { console.error('Error setting title:', e); }

            try {
                // Description Input (Slate JS contenteditable div)
                const descInput = document.querySelector('div[role="textbox"][name="description"]');
                if (descInput) {
                    descInput.focus();
                    
                    // Clear the content first
                    document.execCommand('selectAll', false, null);
                    document.execCommand('delete', false, null);

                    // Insert text using paste event which is natively handled by Slate.js
                    const dataTransfer = new DataTransfer();
                    dataTransfer.setData('text/plain', {{desc}});
                    const pasteEvent = new ClipboardEvent('paste', {
                        clipboardData: dataTransfer,
                        bubbles: true,
                        cancelable: true
                    });
                    descInput.dispatchEvent(pasteEvent);
                }
            } catch (e) { console.error('Error setting description:', e); }

            try {
                // Content Checks (e.g. 18+ and Explicit Content)
                const eighteenPlus = document.querySelector('input[name="isVideoEighteenPlus"]');
                if (eighteenPlus && eighteenPlus.checked) {
                    eighteenPlus.click(); // Ensure it defaults to "No"
                }

                const explicitContent = document.querySelector('input[name="isExplicit"]');
                if (explicitContent && explicitContent.checked) {
                    explicitContent.click(); // Ensure it defaults to "No"
                }
            } catch (e) { console.error('Error setting checks:', e); }
        })();
        """;

        await MyWebView.CoreWebView2.ExecuteScriptAsync(js);

        // Thumbnail Automation using Chrome DevTools Protocol
        if (!string.IsNullOrEmpty(_task.ThumbnailPath))
        {
            try
            {
                // Retrieve the backend objectId of the file input
                string getFileInputJs = "document.querySelector('input[type=\"file\"]')";
                var evaluationResult = await MyWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.evaluate", 
                    $"{{\"expression\":\"{getFileInputJs}\"}}");

                using var doc = System.Text.Json.JsonDocument.Parse(evaluationResult);
                if (doc.RootElement.TryGetProperty("result", out var resultObj) && 
                    resultObj.TryGetProperty("objectId", out var objectIdElement))
                {
                    string objectId = objectIdElement.GetString()!;
                    
                    // Convert local path to an array format for the CDP method
                    string filesJson = System.Text.Json.JsonSerializer.Serialize(new[] { _task.ThumbnailPath });
                    string setFilesArgs = $"{{\"objectId\":\"{objectId}\",\"files\":{filesJson}}}";
                    
                    // Invoke DOM.setFileInputFiles to attach the file
                    await MyWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("DOM.setFileInputFiles", setFilesArgs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao setar a thumbnail via CDP: {ex.Message}");
            }
        }
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
