using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace YTSpotifySync.Views;

public sealed partial class WizardDialog : ContentDialog
{
    public WizardDialog()
    {
        InitializeComponent();
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, "WizardDialogRoot");
        this.Loaded += WizardDialog_Loaded;
    }

    private async void WizardDialog_Loaded(object sender, RoutedEventArgs e)
    {
        bool hasYtdlp = await CheckCommand("yt-dlp --version");
        bool hasFfmpeg = await CheckCommand("ffmpeg -version");

        DependencyProgress.IsActive = false;
        DependencyProgress.Visibility = Visibility.Collapsed;
        DependencyIcon.Visibility = Visibility.Visible;

        if (hasYtdlp && hasFfmpeg)
        {
            DependencyIcon.Glyph = "\uE73E"; // CheckMark
            DependencyIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            DependencyStatusText.Text = "yt-dlp e ffmpeg detectados no sistema!";
        }
        else
        {
            DependencyIcon.Glyph = "\uEA39"; // Warning
            DependencyIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
            DependencyStatusText.Text = "yt-dlp ou ffmpeg não encontrados. O aplicativo tentará baixa-los automaticamente ao iniciar um download.";
        }
    }

    private Task<bool> CheckCommand(string args)
    {
        return Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {args}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null) return false;
                process.WaitForExit(3000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Will close automatically, caller handles navigation
    }
}
