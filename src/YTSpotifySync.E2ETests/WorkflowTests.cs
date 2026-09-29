using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace YTSpotifySync.E2ETests
{
    public class WorkflowTests : IDisposable
    {
        private readonly FlaUI.Core.Application _app;
        private readonly UIA3Automation _automation;
        private readonly Window _mainWindow;
        private readonly string _artifactsDir;
        private readonly ITestOutputHelper _output;

        public WorkflowTests(ITestOutputHelper output)
        {
            _output = output;
            _automation = new UIA3Automation();
            
            // Build the path to the unpackaged exe
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string config = baseDir.Contains("\\Release\\") ? "Release" : "Debug";
            string exePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "YTSpotifySync", "bin", config, "net10.0-windows10.0.26100.0", "win-x64", "YTSpotifySync.exe"));
            
            if (!File.Exists(exePath))
            {
                // Fallback for different build output structures
                exePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "YTSpotifySync", "bin", config, "net10.0-windows10.0.26100.0", "win-x64", "YTSpotifySync.exe"));
            }

            _artifactsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "TestArtifacts"));
            if (!Directory.Exists(_artifactsDir))
            {
                Directory.CreateDirectory(_artifactsDir);
            }

            // Kill any existing instances
            foreach (var p in Process.GetProcessesByName("YTSpotifySync"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }

            // Clean appsettings.json in the output to force Wizard
            string settingsPath = Path.Combine(Path.GetDirectoryName(exePath) ?? "", "appsettings.json");
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
            
            // Clean AppData settings to force Wizard
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YTSpotifySync", "settings.json");
            if (File.Exists(appData))
            {
                File.Delete(appData);
            }

            // Launch via Process.Start to let the WinUI bootstrapper run
            var psi = new ProcessStartInfo(exePath)
            {
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? ""
            };
            Process.Start(psi);
            
            // Wait for app to render
            Thread.Sleep(5000);

            // Attach to the actual running process
            var processes = Process.GetProcessesByName("YTSpotifySync");
            if (processes.Length == 0)
                throw new Exception("YTSpotifySync process not found.");

            _app = FlaUI.Core.Application.Attach(processes[0]);
            
            // Fallback to top level windows
            var windows = _app.GetAllTopLevelWindows(_automation);
            _mainWindow = windows.Length > 0 ? windows[0] : throw new Exception("Window not found");
        }

        [Fact]
        public void Test_1_WizardAppearsAndCanBeClosed()
        {
            _output.WriteLine("Testing Wizard presence...");
            
            // The wizard should be visible on first launch
            var wizard = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("WizardDialogRoot"));
            Assert.NotNull(wizard);

            CaptureScreenshot("1_WizardAppears");

            // Click Configurar Agora (Primary Button in ContentDialog)
            // WinUI 3 ContentDialog primary button usually has AutomationId "PrimaryButton"
            var primaryBtn = wizard.FindFirstDescendant(cf => cf.ByAutomationId("PrimaryButton"))?.AsButton();
            if (primaryBtn != null)
            {
                primaryBtn.Invoke();
            }
            else
            {
                // Fallback to finding by name if AutomationId is missing
                var configureBtn = wizard.FindFirstDescendant(cf => cf.ByName("Configurar Agora"))?.AsButton();
                Assert.NotNull(configureBtn);
                configureBtn.Invoke();
            }
            
            Thread.Sleep(1000);
            CaptureScreenshot("2_WizardClosed_SettingsPage");
        }

        [Fact]
        public void Test_2_SettingsCanBeSaved()
        {
            // Close wizard if it's there
            var wizardBtn = _mainWindow.FindFirstDescendant(cf => cf.ByName("Configurar Agora"))?.AsButton();
            wizardBtn?.Invoke();
            Thread.Sleep(1000);

            // We should be on Settings Page now because wizard redirects there
            var ytKey = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("YoutubeApiKeyTextBox"))?.AsTextBox();
            Assert.NotNull(ytKey);
            
            ytKey.Text = "TEST_YOUTUBE_KEY";

            var spotifyId = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SpotifyClientIdTextBox"))?.AsTextBox();
            Assert.NotNull(spotifyId);
            spotifyId.Text = "TEST_SPOTIFY_ID";

            var saveBtn = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SaveSettingsButton"))?.AsButton();
            Assert.NotNull(saveBtn);
            
            CaptureScreenshot("3_SettingsFilled");
            saveBtn.Invoke();
            Thread.Sleep(500);
            CaptureScreenshot("4_SettingsSaved");
        }

        [Fact]
        public void Test_3_NavigationWorks()
        {
            // Close wizard
            var wizardBtn = _mainWindow.FindFirstDescendant(cf => cf.ByName("Configurar Agora"))?.AsButton();
            wizardBtn?.Invoke();
            Thread.Sleep(1000);

            // Click Dashboard
            var dashboardNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavDashboard"));
            dashboardNav?.Patterns.SelectionItem.Pattern.Select();
            // Fallback select
            if (dashboardNav == null) {
                var dItem = _mainWindow.FindFirstDescendant(cf => cf.ByName("Dashboard"));
                dItem?.Patterns.SelectionItem.Pattern.Select();
            }
            
            Thread.Sleep(1000);
            
            // Verify Sync Button is visible
            var syncBtn = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SyncNowButton"));
            Assert.NotNull(syncBtn);

            CaptureScreenshot("5_DashboardView");
        }

        private void CaptureScreenshot(string name)
        {
            if (_mainWindow != null)
            {
                try { _mainWindow.Focus(); } catch { }
                Thread.Sleep(500); // Give it time to paint
                var image = Capture.Element(_mainWindow);
                string path = Path.Combine(_artifactsDir, $"{name}.png");
                image.ToFile(path);
                _output.WriteLine($"Screenshot saved to {path}");
            }
        }

        public void Dispose()
        {
            _app?.Close();
            _app?.Dispose();
            _automation?.Dispose();
        }
    }
}
