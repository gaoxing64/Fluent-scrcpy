using System;
using System.IO;
using System.Threading.Tasks;

namespace FluentScrcpy.WinUI3.Services;

public static class LogService
{
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static LogService()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentScrcpy", "Logs");
        Directory.CreateDirectory(logDir);
        LogFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        
        WriteLog("LogService initialized");
    }

    public static void WriteLog(string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        
        lock (LockObj)
        {
            try
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }
        
        System.Diagnostics.Debug.WriteLine(logEntry);
    }

    public static void WriteLog(string context, string message)
    {
        WriteLog($"[{context}] {message}");
    }

    public static void WriteLog(string context, Exception ex)
    {
        WriteLog($"[{context}] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        WriteLog($"[{context}] STACK TRACE: {ex.StackTrace}");
    }

    public static string GetLogFilePath() => LogFilePath;
}
