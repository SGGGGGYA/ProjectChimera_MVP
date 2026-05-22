using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 战斗日志系统 — 记录战斗中的关键信息
///   - 显示在 UI 上方便玩家复盘
///   - 写入文件 battle_log.txt 方便 AI 读取分析
/// </summary>
public static class BattleLog
{
    private static List<string> entries = new List<string>();
    /// <summary>供 uGUI 读取的只读引用</summary>
    public static List<string> Entries => entries;
    /// <summary>新增日志时的回调（供 uGUI 刷新）</summary>
    public static event System.Action OnNewEntry;
    private const int MaxEntries = 20;

    // 日志文件路径（项目根目录下的 battle_log.txt）
    private static string _logPath;
    private static string LogPath
    {
        get
        {
            if (_logPath == null)
                _logPath = Application.dataPath + "/../battle_log.txt";
            return _logPath;
        }
    }

    /// <summary>清空日志并新建日志文件</summary>
    public static void Clear()
    {
        entries.Clear();

        try
        {
            string dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(LogPath, "=== Project Chimera Battle Log ===\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BattleLog] 无法写入日志文件: {e.Message}");
        }
    }

    /// <summary>写入一条日志（UI + 控制台 + 文件）</summary>
    public static void Add(string message)
    {
        string entry = $"[{Time.time:F1}s] {message}";
        entries.Add(entry);
        Debug.Log(entry);

        // 追加到文件
        try
        {
            File.AppendAllText(LogPath, entry + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BattleLog] 无法写入日志文件: {e.Message}");
        }

        if (entries.Count > MaxEntries)
            entries.RemoveRange(0, entries.Count - MaxEntries);

        // 通知 uGUI 刷新
        OnNewEntry?.Invoke();
    }


}
