using System;
using System.IO;
using System.Threading;

namespace YTSpotifySync;

public static class Logger
{
    private static readonly string LogFilePath;
    private static readonly object _lock = new object();

    static Logger()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        LogFilePath = Path.Combine(baseDir, "app.log");
        
        try
        {
            if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > 10 * 1024 * 1024)
            {
                File.Delete(LogFilePath); // Keep log file under 10MB
            }
            Log("================ APP STARTED ================");
        }
        catch { }
    }

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [T{Thread.CurrentThread.ManagedThreadId:D2}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logLine);
            }
        }
        catch
        {
            // Ignore logging errors to prevent app crashes
        }
    }
}
