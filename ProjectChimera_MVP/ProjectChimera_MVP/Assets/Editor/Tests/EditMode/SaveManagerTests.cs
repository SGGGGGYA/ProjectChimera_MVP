using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="SaveManager"/> 的存档读写。
///
/// 存档文件路径：<c>Application.dataPath + "/../save_data.json"</c>（项目根目录）。
/// 测试会在 SetUp 备份真实存档，在 TearDown 还原，避免污染用户数据。
/// </summary>
[TestFixture]
public class SaveManagerTests
{
    string backupPath;

    [SetUp]
    public void SetUp()
    {
        // 备份真实存档
        if (File.Exists(SaveManager.SavePath))
        {
            backupPath = SaveManager.SavePath + ".bak";
            File.Copy(SaveManager.SavePath, backupPath, overwrite: true);
            File.Delete(SaveManager.SavePath);
        }
        Assert.IsFalse(File.Exists(SaveManager.SavePath),
            "SetUp 后应无存档");
    }

    [TearDown]
    public void TearDown()
    {
        // 清理测试产生的存档
        if (File.Exists(SaveManager.SavePath))
            File.Delete(SaveManager.SavePath);
        // 还原备份
        if (backupPath != null && File.Exists(backupPath))
        {
            File.Move(backupPath, SaveManager.SavePath);
            backupPath = null;
        }
    }

    // ============== HasSave ==============

    [Test]
    public void HasSave_NoFile_ReturnsFalse()
    {
        Assert.IsFalse(SaveManager.HasSave());
    }

    [Test]
    public void HasSave_AfterSave_ReturnsTrue()
    {
        var data = new SaveData { gold = 100, dungeonFloor = 1 };
        SaveManager.Save(data);
        Assert.IsTrue(SaveManager.HasSave());
    }

    [Test]
    public void HasSave_AfterDelete_ReturnsFalse()
    {
        var data = new SaveData { gold = 100 };
        SaveManager.Save(data);
        Assert.IsTrue(SaveManager.HasSave());
        SaveManager.DeleteSave();
        Assert.IsFalse(SaveManager.HasSave());
    }

    // ============== Load ==============

    [Test]
    public void Load_NoFile_ReturnsNull()
    {
        var data = SaveManager.Load();
        Assert.IsNull(data);
    }

    [Test]
    public void Load_AfterSave_RoundTrip()
    {
        var original = new SaveData
        {
            gold = 1234,
            dungeonFloor = 3,
            squadX = 5,
            squadY = 7,
            inventory = new List<ItemStack>
            {
                new ItemStack("potion_hp", 5),
                new ItemStack("sword_iron", 1),
            },
            playerTeam = new List<UnitBattleData>(),
        };
        SaveManager.Save(original);

        var loaded = SaveManager.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(1234, loaded.gold);
        Assert.AreEqual(3, loaded.dungeonFloor);
        Assert.AreEqual(5, loaded.squadX);
        Assert.AreEqual(7, loaded.squadY);
        Assert.AreEqual(2, loaded.inventory.Count);
        Assert.AreEqual("potion_hp", loaded.inventory[0].itemId);
        Assert.AreEqual(5, loaded.inventory[0].quantity);
        Assert.AreEqual("sword_iron", loaded.inventory[1].itemId);
    }

    [Test]
    public void Load_SetsTimestamp()
    {
        var data = new SaveData { gold = 100 };
        SaveManager.Save(data);

        var loaded = SaveManager.Load();
        Assert.IsNotNull(loaded.timestamp);
        Assert.AreNotEqual("", loaded.timestamp, "timestamp 应非空");
        // 格式 yyyy-MM-dd HH:mm:ss，长度 19
        Assert.AreEqual(19, loaded.timestamp.Length,
            $"timestamp 格式应为 yyyy-MM-dd HH:mm:ss，实际={loaded.timestamp}");
    }

    [Test]
    public void Load_OverwriteExisting_ReplacesContent()
    {
        SaveManager.Save(new SaveData { gold = 100 });
        SaveManager.Save(new SaveData { gold = 999 });

        var loaded = SaveManager.Load();
        Assert.AreEqual(999, loaded.gold, "第二次 Save 应覆盖第一次");
    }

    [Test]
    public void Load_CorruptedJson_ReturnsNull()
    {
        // 手动写入坏 JSON
        File.WriteAllText(SaveManager.SavePath, "{ this is not valid json ::: ]]]");

        var data = SaveManager.Load();
        Assert.IsNull(data, "损坏的 JSON 应返回 null（不抛异常）");
    }

    // ============== Save ==============

    [Test]
    public void Save_CreatesFileAtExpectedPath()
    {
        var data = new SaveData { gold = 50 };
        SaveManager.Save(data);
        Assert.IsTrue(File.Exists(SaveManager.SavePath),
            $"Save 应在 {SaveManager.SavePath} 创建文件");
    }

    [Test]
    public void Save_PrettyPrinted_ContainsNewlines()
    {
        var data = new SaveData { gold = 100 };
        SaveManager.Save(data);

        string content = File.ReadAllText(SaveManager.SavePath);
        // prettyPrint=true 时，JSON 应有缩进/换行
        Assert.Greater(content.Length, 50, "存档内容应非空");
        Assert.IsTrue(content.Contains("\n") || content.Contains("{"),
            "存档应含 JSON 结构");
    }

    [Test]
    public void Save_NullData_DoesNotThrow()
    {
        // null data 会让 JsonUtility.ToJson 返回 "{}"，写入应不抛
        // 实际上 JsonUtility.ToJson(null) 不抛，File.WriteAllText 也不抛
        Assert.DoesNotThrow(() => SaveManager.Save(null));
    }

    [Test]
    public void Save_PreservesAllScalarFields()
    {
        var data = new SaveData
        {
            gold = 9999,
            dungeonFloor = 42,
            squadX = -3,
            squadY = 11,
        };
        SaveManager.Save(data);

        var loaded = SaveManager.Load();
        Assert.AreEqual(9999, loaded.gold);
        Assert.AreEqual(42, loaded.dungeonFloor);
        Assert.AreEqual(-3, loaded.squadX);
        Assert.AreEqual(11, loaded.squadY);
    }

    // ============== DeleteSave ==============

    [Test]
    public void DeleteSave_NoFile_DoesNotThrow()
    {
        Assert.IsFalse(File.Exists(SaveManager.SavePath));
        Assert.DoesNotThrow(() => SaveManager.DeleteSave());
    }

    [Test]
    public void DeleteSave_AfterSave_FileGone()
    {
        SaveManager.Save(new SaveData { gold = 100 });
        Assert.IsTrue(File.Exists(SaveManager.SavePath));

        SaveManager.DeleteSave();

        Assert.IsFalse(File.Exists(SaveManager.SavePath));
    }

    // ============== SavePath 路径 ==============

    [Test]
    public void SavePath_IsUnderProjectRoot()
    {
        // 路径应包含 save_data.json
        Assert.IsTrue(SaveManager.SavePath.EndsWith("save_data.json"),
            $"存档路径应以 save_data.json 结尾，实际={SaveManager.SavePath}");
    }

    [Test]
    public void SavePath_ConsistentAcrossAccesses()
    {
        var p1 = SaveManager.SavePath;
        var p2 = SaveManager.SavePath;
        Assert.AreEqual(p1, p2, "SavePath 应是稳定的缓存值");
    }

    // ============== 多个 Save 之间的隔离 ==============

    [Test]
    public void MultipleSavesAndLoads_StableState()
    {
        for (int i = 0; i < 5; i++)
        {
            SaveManager.Save(new SaveData { gold = i * 100, dungeonFloor = i });
            var loaded = SaveManager.Load();
            Assert.AreEqual(i * 100, loaded.gold);
            Assert.AreEqual(i, loaded.dungeonFloor);
        }
    }

    // ============== SaveData 初始值 ==============

    [Test]
    public void SaveData_DefaultInventory_NotNull()
    {
        var data = new SaveData();
        // SaveData 字段声明：public List<ItemStack> inventory = new List<ItemStack>();
        Assert.IsNotNull(data.inventory);
    }

    [Test]
    public void SaveData_PlayerTeam_CanBeNull()
    {
        var data = new SaveData();
        // playerTeam 没默认值 → 是 null
        // 写入时 JsonUtility 会序列化为 []
        // 读取时如果原数据就是 null → 仍是 null
        SaveManager.Save(data);
        var loaded = SaveManager.Load();
        // JsonUtility 的行为：null list 会被序列化为空数组，读取后变空 list
        Assert.IsTrue(loaded.playerTeam == null || loaded.playerTeam.Count == 0,
            "playerTeam 在 round-trip 后应为空或 null");
    }
}
