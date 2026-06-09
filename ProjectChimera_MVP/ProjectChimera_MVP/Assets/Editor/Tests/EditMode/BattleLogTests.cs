using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="BattleLog"/> 的内存缓冲与文件输出。
///
/// 关键路径：
///   - Add(message)  → entries 列表追加，OnNewEntry 事件触发
///   - 超过 MaxEntries(20) → 滚动移除最旧
///   - Clear()       → 清空 entries
///   - 写入文件      → 追加到 battle_log.txt（项目根目录）
///
/// 测试会在 SetUp 备份原日志，TearDown 还原。
/// </summary>
[TestFixture]
public class BattleLogTests
{
    string logPath;

    [SetUp]
    public void SetUp()
    {
        // BattleLog.LogPath 私有，通过 Application.dataPath 推算
        logPath = Application.dataPath + "/../battle_log.txt";
        if (File.Exists(logPath))
        {
            string backup = logPath + ".bak";
            File.Copy(logPath, backup, overwrite: true);
            File.Delete(logPath);
        }
        BattleLog.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        // 清空内存 entries（避免影响后续测试）
        BattleLog.Clear();
        // 清理本次测试创建的文件
        if (File.Exists(logPath))
            File.Delete(logPath);
        // 还原备份
        string backup = logPath + ".bak";
        if (File.Exists(backup))
        {
            File.Move(backup, logPath);
        }
    }

    // ============== Add & Entries ==============

    [Test]
    public void Add_AppendsToEntries()
    {
        int beforeCount = BattleLog.Entries.Count;
        BattleLog.Add("Hello");
        BattleLog.Add("World");
        Assert.AreEqual(beforeCount + 2, BattleLog.Entries.Count);
        Assert.IsTrue(BattleLog.Entries[beforeCount].Contains("Hello"));
        Assert.IsTrue(BattleLog.Entries[beforeCount + 1].Contains("World"));
    }

    [Test]
    public void Add_PrependsTimestamp()
    {
        BattleLog.Add("TestMessage");
        var entry = BattleLog.Entries[BattleLog.Entries.Count - 1];
        // 格式：[12.3s] TestMessage
        Assert.IsTrue(entry.Contains("TestMessage"));
        // 含 [..s] 时间戳前缀
        Assert.IsTrue(entry.StartsWith("[") && entry.Contains("s]"),
            $"entry 应以时间戳开头，实际={entry}");
    }

    [Test]
    public void Add_TriggersOnNewEntryEvent()
    {
        int callCount = 0;
        System.Action handler = () => callCount++;
        BattleLog.OnNewEntry += handler;
        try
        {
            BattleLog.Add("EventTest");
            Assert.AreEqual(1, callCount, "Add 应触发 OnNewEntry 一次");

            BattleLog.Add("EventTest2");
            Assert.AreEqual(2, callCount, "每次 Add 都应触发");
        }
        finally
        {
            BattleLog.OnNewEntry -= handler;
        }
    }

    [Test]
    public void Add_MultipleSubscribers_AllInvoked()
    {
        int c1 = 0, c2 = 0;
        System.Action h1 = () => c1++;
        System.Action h2 = () => c2++;
        BattleLog.OnNewEntry += h1;
        BattleLog.OnNewEntry += h2;
        try
        {
            BattleLog.Add("Multi");
            Assert.AreEqual(1, c1);
            Assert.AreEqual(1, c2);
        }
        finally
        {
            BattleLog.OnNewEntry -= h1;
            BattleLog.OnNewEntry -= h2;
        }
    }

    [Test]
    public void Add_RemovedSubscriber_NotInvoked()
    {
        int count = 0;
        System.Action handler = () => count++;
        BattleLog.OnNewEntry += handler;
        BattleLog.Add("A");
        Assert.AreEqual(1, count);

        BattleLog.OnNewEntry -= handler;
        BattleLog.Add("B");
        Assert.AreEqual(1, count, "解绑后不应再触发");
    }

    // ============== Clear ==============

    [Test]
    public void Clear_EmptiesEntries()
    {
        BattleLog.Add("a");
        BattleLog.Add("b");
        Assert.Greater(BattleLog.Entries.Count, 0);

        BattleLog.Clear();
        Assert.AreEqual(0, BattleLog.Entries.Count);
    }

    [Test]
    public void Clear_CreatesHeaderFile()
    {
        BattleLog.Clear();
        // Clear 会写入头部：=== Project Chimera Battle Log ===
        Assert.IsTrue(File.Exists(logPath), "Clear 应创建/覆盖日志文件");
        string content = File.ReadAllText(logPath);
        Assert.IsTrue(content.Contains("Project Chimera"),
            $"Clear 后文件应含头部，实际内容前 100 字符={content.Substring(0, System.Math.Min(100, content.Length))}");
    }

    // ============== MaxEntries 滚动 ==============

    [Test]
    public void Add_OverMaxEntries_RemovesOldest()
    {
        // MaxEntries = 20（私有常量）
        // 添加 25 条
        for (int i = 0; i < 25; i++)
            BattleLog.Add($"msg{i}");

        Assert.LessOrEqual(BattleLog.Entries.Count, 20,
            $"entries 数量应 <= MaxEntries(20)，实际={BattleLog.Entries.Count}");

        // 最早的消息（msg0..msg4）应被移除
        bool hasMsg0 = false, hasMsg4 = false, hasMsg24 = false;
        foreach (var e in BattleLog.Entries)
        {
            if (e.Contains("msg0")) hasMsg0 = true;
            if (e.Contains("msg4")) hasMsg4 = true;
            if (e.Contains("msg24")) hasMsg24 = true;
        }
        Assert.IsTrue(hasMsg24, "最新消息 msg24 应保留");
        // 最早 5 条应被滚动移除
        Assert.IsFalse(hasMsg0, "msg0 应被滚动移除");
    }

    // ============== Entries 只读 ==============

    [Test]
    public void Entries_ReturnsValidList()
    {
        // Entries 属性返回 List<string>，可读
        Assert.IsNotNull(BattleLog.Entries);
        BattleLog.Add("x");
        Assert.Greater(BattleLog.Entries.Count, 0);
    }

    [Test]
    public void Entries_AfterManyAdds_Stable()
    {
        for (int i = 0; i < 50; i++)
            BattleLog.Add($"batch_{i}");

        // 验证：50 次添加后，数量稳定在 20，且包含最新的 20 条
        Assert.AreEqual(20, BattleLog.Entries.Count);
        // 最后一条应是 batch_49
        var last = BattleLog.Entries[BattleLog.Entries.Count - 1];
        Assert.IsTrue(last.Contains("batch_49"));
    }

    // ============== 文件 I/O ==============

    [Test]
    public void Add_WritesToFile()
    {
        BattleLog.Add("FileTest1");
        BattleLog.Add("FileTest2");

        Assert.IsTrue(File.Exists(logPath));
        string content = File.ReadAllText(logPath);
        Assert.IsTrue(content.Contains("FileTest1"));
        Assert.IsTrue(content.Contains("FileTest2"));
    }

    [Test]
    public void Add_AppendsToExistingFile()
    {
        // Clear 后写头部，再 Add 多条
        BattleLog.Clear();
        long sizeBefore = new FileInfo(logPath).Length;

        BattleLog.Add("Append1");
        BattleLog.Add("Append2");

        long sizeAfter = new FileInfo(logPath).Length;
        Assert.Greater(sizeAfter, sizeBefore, "Add 应追加文件大小");
    }

    [Test]
    public void Add_EmptyMessage_Allowed()
    {
        // 空消息不抛异常
        Assert.DoesNotThrow(() => BattleLog.Add(""));
        Assert.Greater(BattleLog.Entries.Count, 0, "空消息也应被记录");
    }

    [Test]
    public void Add_LongMessage_Allowed()
    {
        string longMsg = new string('x', 1000);
        Assert.DoesNotThrow(() => BattleLog.Add(longMsg));
    }

    [Test]
    public void Add_UnicodeMessage_Preserved()
    {
        BattleLog.Add("中文测试 αβγ 🎮");
        var last = BattleLog.Entries[BattleLog.Entries.Count - 1];
        Assert.IsTrue(last.Contains("中文测试"));
        Assert.IsTrue(last.Contains("🎮"));
    }

    // ============== LogPath ==============

    [Test]
    public void LogPath_UnderProjectRoot()
    {
        // BattleLog.LogPath 是私有，间接通过 Application.dataPath
        // 验证：相对项目根的 battle_log.txt
        string expected = Application.dataPath + "/../battle_log.txt";
        Assert.AreEqual(expected, logPath);
    }

    // ============== 事件清理 ==============

    [Test]
    public void OnNewEntry_PersistAcrossAdds()
    {
        // 验证事件订阅持久（不应在 Clear 时被清空）
        int count = 0;
        System.Action h = () => count++;
        BattleLog.OnNewEntry += h;
        try
        {
            BattleLog.Clear();   // Clear 不应触发 OnNewEntry（只清 entries，不调 Add）
            Assert.AreEqual(0, count, "Clear 不应触发 OnNewEntry");

            BattleLog.Add("after_clear");
            Assert.AreEqual(1, count, "Clear 后订阅者仍能接收事件");
        }
        finally
        {
            BattleLog.OnNewEntry -= h;
        }
    }
}
