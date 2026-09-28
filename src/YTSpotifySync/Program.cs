using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace YTSpotifySync;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Application startup exception: {ex}");
            throw;
        }
    }
}
