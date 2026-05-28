using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Log
{
    public enum Level { Verbose, Info, Warn, Error }

    public struct Entry
    {
        public Level level;
        public string tag;
        public string message;
        public string timestamp;
        public string stackTrace;
        public string sourceFile;
        public int sourceLine;
    }

    public static Level MinLevel { get; set; } = Level.Verbose;
    public static bool EnableFileOutput { get; set; } = true;

    private static List<Entry> _entries = new List<Entry>();
    private static StreamWriter _fileWriter;
    private static readonly string LogFilePath;
    private static readonly object _lock = new object();

    public static IReadOnlyList<Entry> Entries => _entries;
    public static event Action<Entry> OnEntry;

    /// <summary>当前日志文件完整路径</summary>
    public static string FilePath => LogFilePath;

    public const int MaxEntries = 2000;

    static Log()
    {
        string dir = Path.Combine(Application.persistentDataPath, "logs");
        try
        {
            Directory.CreateDirectory(dir);
            CleanupOldLogs(dir, 10);
        }
        catch { }

        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        LogFilePath = Path.Combine(dir, $"game_{ts}.log");

        try
        {
            _fileWriter = new StreamWriter(LogFilePath, append: false) { AutoFlush = true };
            _fileWriter.WriteLine($"[Log] Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _fileWriter.WriteLine($"[Log] Unity {Application.unityVersion} | Platform {Application.platform}");
            _fileWriter.WriteLine($"[Log] LogLevel={MinLevel} FileOutput={EnableFileOutput}");
            _fileWriter.WriteLine("---");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Log] Cannot create log file: {e.Message}");
            EnableFileOutput = false;
        }

        if (EnableFileOutput)
            Debug.Log($"[Log] Log file: {LogFilePath}");
    }

    public static void Verbose(string message,
        [CallerFilePath] string src = "",
        [CallerLineNumber] int line = 0)
    {
        Write(Level.Verbose, ExtractTag(src), message, src, line);
    }

    public static void Info(string message,
        [CallerFilePath] string src = "",
        [CallerLineNumber] int line = 0)
    {
        Write(Level.Info, ExtractTag(src), message, src, line);
    }

    public static void Warn(string message,
        [CallerFilePath] string src = "",
        [CallerLineNumber] int line = 0)
    {
        Write(Level.Warn, ExtractTag(src), message, src, line);
    }

    public static void Error(string message,
        [CallerFilePath] string src = "",
        [CallerLineNumber] int line = 0)
    {
        Write(Level.Error, ExtractTag(src), message, src, line);
    }

    private static void Write(Level level, string tag, string message,
        string sourceFile, int sourceLine)
    {
        if (level < MinLevel) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string stackTrace = null;

        if (level == Level.Error)
            stackTrace = Environment.StackTrace;

        var entry = new Entry
        {
            level = level,
            tag = tag,
            message = message,
            timestamp = timestamp,
            stackTrace = stackTrace,
            sourceFile = sourceFile,
            sourceLine = sourceLine
        };

        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }

        string formatted = $"[{timestamp}][{tag}] {message}";
        switch (level)
        {
            case Level.Verbose:
            case Level.Info:
                Debug.Log(formatted);
                break;
            case Level.Warn:
                Debug.LogWarning(formatted);
                break;
            case Level.Error:
                Debug.LogError(formatted);
                break;
        }

        if (EnableFileOutput && _fileWriter != null)
        {
            try
            {
                _fileWriter.WriteLine($"[{timestamp}][{level}][{tag}:{sourceLine}] {message}");
                if (level == Level.Error && !string.IsNullOrEmpty(stackTrace))
                {
                    using (var reader = new StringReader(stackTrace))
                    {
                        string stLine;
                        while ((stLine = reader.ReadLine()) != null)
                            _fileWriter.WriteLine($"  |{stLine}");
                    }
                }
            }
            catch { }
        }

        OnEntry?.Invoke(entry);
    }

    private static string ExtractTag(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath)) return "?";
        try { return Path.GetFileNameWithoutExtension(sourcePath); }
        catch { return "?"; }
    }

    private static void CleanupOldLogs(string dir, int keep)
    {
        var files = new List<string>(Directory.GetFiles(dir, "game_*.log"));
        files.Sort();
        while (files.Count > keep)
        {
            File.Delete(files[0]);
            files.RemoveAt(0);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}
