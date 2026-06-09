using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖英雄厅（UIHeroHallController）相关逻辑。
///
/// 测试范围：
///   - UnitBattleData 命名系统持久化字段默认值
///   - EquipExclusiveSkill 静态行为：写入 / 拒绝未命名 / 替换已选 / 幂等
///   - Save → Load round-trip：exclusiveSkillId 持久化
///   - 持久化字段在不同实例间相互独立
///
/// 注意：UI 面板构建（EnsurePanel/Refresh）依赖 Canvas/TMP，EditMode 下不直接构造；
/// 我们只覆盖纯逻辑部分，UI 集成测试留待 PlayMode。
/// </summary>
[TestFixture]
public class HeroHallTests
{
    string backupPath;

    [SetUp]
    public void SetUp()
    {
        if (File.Exists(SaveManager.SavePath))
        {
            backupPath = SaveManager.SavePath + ".bak";
            File.Copy(SaveManager.SavePath, backupPath, overwrite: true);
            File.Delete(SaveManager.SavePath);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(SaveManager.SavePath))
            File.Delete(SaveManager.SavePath);
        if (backupPath != null && File.Exists(backupPath))
        {
            File.Move(backupPath, SaveManager.SavePath);
            backupPath = null;
        }
    }

    // ==================== 1. UnitBattleData 字段默认值 ====================

    [Test]
    public void UnitBattleData_DefaultNamingFields_AreUnset()
    {
        var u = new UnitBattleData { unitName = "测试英雄" };
        Assert.IsFalse(u.isNamed, "默认未命名");
        Assert.AreEqual(0, u.stressCapBonus, "默认压力上限奖励 0");
        Assert.IsTrue(string.IsNullOrEmpty(u.exclusiveSkillId), "默认专属技能 ID 为空");
    }

    [Test]
    public void UnitBattleData_NamingFields_ArePubliclyMutable()
    {
        var u = new UnitBattleData();
        u.isNamed = true;
        u.stressCapBonus = 20;
        u.exclusiveSkillId = "传奇之刃";
        Assert.IsTrue(u.isNamed);
        Assert.AreEqual(20, u.stressCapBonus);
        Assert.AreEqual("传奇之刃", u.exclusiveSkillId);
    }

    // ==================== 2. EquipExclusiveSkill 静态行为 ====================

    [Test]
    public void EquipExclusiveSkill_NamedHero_WritesId()
    {
        var hero = new UnitBattleData { unitName = "战士", isNamed = true };
        var skill = new SkillData { skillName = "盾牌猛击", description = "伤害+眩晕" };

        UIHeroHallController.EquipExclusiveSkill(hero, skill);

        Assert.AreEqual("盾牌猛击", hero.exclusiveSkillId,
            "已命名英雄装备技能后，exclusiveSkillId 应被写入 skillName");
    }

    [Test]
    public void EquipExclusiveSkill_UnnamedHero_NoOp()
    {
        var hero = new UnitBattleData { unitName = "战士", isNamed = false };
        var skill = new SkillData { skillName = "盾牌猛击" };

        UIHeroHallController.EquipExclusiveSkill(hero, skill);

        Assert.IsTrue(string.IsNullOrEmpty(hero.exclusiveSkillId),
            "未命名英雄调用装备技能应被拒绝，exclusiveSkillId 保持空");
    }

    [Test]
    public void EquipExclusiveSkill_NullHero_NoThrow()
    {
        var skill = new SkillData { skillName = "X" };
        Assert.DoesNotThrow(() => UIHeroHallController.EquipExclusiveSkill(null, skill));
    }

    [Test]
    public void EquipExclusiveSkill_NullSkill_NoThrow()
    {
        var hero = new UnitBattleData { unitName = "X", isNamed = true };
        Assert.DoesNotThrow(() => UIHeroHallController.EquipExclusiveSkill(hero, null));
        Assert.IsTrue(string.IsNullOrEmpty(hero.exclusiveSkillId),
            "hero 仍应有空 ID（未写入）");
    }

    [Test]
    public void EquipExclusiveSkill_ReplacesPreviousSelection()
    {
        var hero = new UnitBattleData { unitName = "战士", isNamed = true };
        var skillA = new SkillData { skillName = "盾牌猛击" };
        var skillB = new SkillData { skillName = "嘲讽" };

        UIHeroHallController.EquipExclusiveSkill(hero, skillA);
        Assert.AreEqual("盾牌猛击", hero.exclusiveSkillId);

        UIHeroHallController.EquipExclusiveSkill(hero, skillB);
        Assert.AreEqual("嘲讽", hero.exclusiveSkillId,
            "再次装备应覆盖之前的技能 ID");
    }

    [Test]
    public void EquipExclusiveSkill_SameSkillTwice_Idempotent()
    {
        var hero = new UnitBattleData { unitName = "战士", isNamed = true };
        var skill = new SkillData { skillName = "盾牌猛击" };

        UIHeroHallController.EquipExclusiveSkill(hero, skill);
        UIHeroHallController.EquipExclusiveSkill(hero, skill);

        Assert.AreEqual("盾牌猛击", hero.exclusiveSkillId,
            "重复装备同一技能应保持一致结果");
    }

    [Test]
    public void EquipExclusiveSkill_KeepsStressCapBonusUntouched()
    {
        // EquipExclusiveSkill 只改 exclusiveSkillId，不该动 stressCapBonus
        var hero = new UnitBattleData { unitName = "战士", isNamed = true, stressCapBonus = 20 };
        var skill = new SkillData { skillName = "X" };

        UIHeroHallController.EquipExclusiveSkill(hero, skill);

        Assert.AreEqual(20, hero.stressCapBonus, "装备技能不应修改 stressCapBonus");
    }

    // ==================== 3. 持久化 ====================

    [Test]
    public void SaveLoad_PreservesExclusiveSkillId()
    {
        var team = new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "战士", isNamed = true, stressCapBonus = 20,
                exclusiveSkillId = "传奇之刃"
            },
            new UnitBattleData
            {
                unitName = "游侠", isNamed = false, stressCapBonus = 0,
                exclusiveSkillId = null
            }
        };

        var data = new SaveData { gold = 100, playerTeam = team };
        SaveManager.Save(data);

        var loaded = SaveManager.Load();
        Assert.IsNotNull(loaded);
        Assert.IsNotNull(loaded.playerTeam);
        Assert.AreEqual(2, loaded.playerTeam.Count);

        var warrior = loaded.playerTeam[0];
        Assert.IsTrue(warrior.isNamed);
        Assert.AreEqual(20, warrior.stressCapBonus);
        Assert.AreEqual("传奇之刃", warrior.exclusiveSkillId,
            "exclusiveSkillId 必须在存档中持久化");

        var ranger = loaded.playerTeam[1];
        Assert.IsFalse(ranger.isNamed);
        Assert.AreEqual(0, ranger.stressCapBonus);
        Assert.IsTrue(string.IsNullOrEmpty(ranger.exclusiveSkillId));
    }

    [Test]
    public void SaveLoad_PreservesEmptyExclusiveSkillId()
    {
        var team = new List<UnitBattleData>
        {
            new UnitBattleData { unitName = "X", isNamed = true, stressCapBonus = 20, exclusiveSkillId = "" }
        };
        var data = new SaveData { playerTeam = team };
        SaveManager.Save(data);

        var loaded = SaveManager.Load();
        Assert.AreEqual(1, loaded.playerTeam.Count);
        Assert.IsTrue(string.IsNullOrEmpty(loaded.playerTeam[0].exclusiveSkillId),
            "空字符串的 exclusiveSkillId 在 round-trip 后应仍为空");
    }

    // ==================== 4. 独立性 ====================

    [Test]
    public void MultipleHeroes_IndependentExclusiveSkillId()
    {
        var warrior = new UnitBattleData { unitName = "战士", isNamed = true };
        var mage    = new UnitBattleData { unitName = "学者", isNamed = true };
        var sWarrior = new SkillData { skillName = "盾牌猛击" };
        var sMage    = new SkillData { skillName = "闪电链" };

        UIHeroHallController.EquipExclusiveSkill(warrior, sWarrior);
        UIHeroHallController.EquipExclusiveSkill(mage, sMage);

        Assert.AreEqual("盾牌猛击", warrior.exclusiveSkillId);
        Assert.AreEqual("闪电链", mage.exclusiveSkillId);
        Assert.AreNotEqual(warrior.exclusiveSkillId, mage.exclusiveSkillId);
    }

    [Test]
    public void NamedHero_CanReequipAfterClear()
    {
        var hero = new UnitBattleData { unitName = "战士", isNamed = true, exclusiveSkillId = "旧技能" };
        var newSkill = new SkillData { skillName = "新技能" };

        UIHeroHallController.EquipExclusiveSkill(hero, newSkill);

        Assert.AreEqual("新技能", hero.exclusiveSkillId, "已命名英雄应能正常替换技能");

        hero.exclusiveSkillId = ""; // 模拟玩家主动卸下
        UIHeroHallController.EquipExclusiveSkill(hero, newSkill);
        Assert.AreEqual("新技能", hero.exclusiveSkillId, "清空后再次装备应仍生效");
    }

    // ==================== 5. SkillData 引用语义 ====================

    [Test]
    public void EquipExclusiveSkill_StoresSkillName_NotObjectReference()
    {
        // 重要：持久化只能存字符串/数值，所以存的是 skillName 字符串
        var hero = new UnitBattleData { unitName = "X", isNamed = true };
        var skill = new SkillData { skillName = "abc" };

        UIHeroHallController.EquipExclusiveSkill(hero, skill);

        Assert.AreEqual("abc", hero.exclusiveSkillId, "应存 skillName 字符串");
    }

    // ==================== 6. UnitData.ApplyExclusiveSkillFromId (进战同步) ====================

    GameObject go;
    UnitData unit;

    void MakeUnitWithClass(string unitName, params string[] skillNames)
    {
        if (go != null) Object.DestroyImmediate(go);
        go = new GameObject("TestUnit_HeroHall");
        unit = go.AddComponent<UnitData>();
        unit.unitName = unitName;
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();
        unit.skills = new List<SkillData>();
        unit.breakdownState = BreakdownState.None;
        unit.stress = 0;
        unit.stressResistRate = 0f;
        unit.level = 5;
        unit.isPlayer = true;
        unit.isNamed = true;
        unit.exclusiveSkill = null;

        var classDef = ScriptableObject.CreateInstance<ClassDefinition>();
        classDef.unitName = unitName;
        classDef.skillPool = new List<SkillData>();
        foreach (var s in skillNames)
            classDef.skillPool.Add(new SkillData { skillName = s });
        unit.classData = classDef;
    }

    [TearDown]
    public void ExtraTearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
        go = null;
        unit = null;
    }

    [Test]
    public void ApplyExclusiveSkillFromId_MatchInPool_AssignsSkill()
    {
        MakeUnitWithClass("战士", "盾牌猛击", "嘲讽");
        var matched = unit.ApplyExclusiveSkillFromId("嘲讽");
        Assert.IsNotNull(matched, "匹配成功应返回 SkillData");
        Assert.AreEqual("嘲讽", matched.skillName);
        Assert.AreEqual(matched, unit.exclusiveSkill, "unit.exclusiveSkill 应被赋值");
    }

    [Test]
    public void ApplyExclusiveSkillFromId_NotInPool_ReturnsNull_NoAssign()
    {
        MakeUnitWithClass("战士", "盾牌猛击", "嘲讽");
        var matched = unit.ApplyExclusiveSkillFromId("不存在");
        Assert.IsNull(matched, "找不到应返回 null");
        Assert.IsNull(unit.exclusiveSkill, "找不到时不应修改 exclusiveSkill");
    }

    [Test]
    public void ApplyExclusiveSkillFromId_EmptyId_ReturnsNull()
    {
        MakeUnitWithClass("战士", "盾牌猛击");
        var matched = unit.ApplyExclusiveSkillFromId("");
        Assert.IsNull(matched);
        Assert.IsNull(unit.exclusiveSkill);
    }

    [Test]
    public void ApplyExclusiveSkillFromId_NullId_ReturnsNull()
    {
        MakeUnitWithClass("战士", "盾牌猛击");
        var matched = unit.ApplyExclusiveSkillFromId(null);
        Assert.IsNull(matched);
        Assert.IsNull(unit.exclusiveSkill);
    }

    [Test]
    public void ApplyExclusiveSkillFromId_NullClassData_ReturnsNull()
    {
        MakeUnitWithClass("X", "A");
        unit.classData = null;
        var matched = unit.ApplyExclusiveSkillFromId("A");
        Assert.IsNull(matched, "classData 为空时返回 null（不抛）");
    }

    [Test]
    public void ApplyExclusiveSkillFromId_ExclusiveSkillAppearsInSkillsList()
    {
        // 验证：选中的专属技能在 unit.skills 中也存在（BattleSetup 会把整个 skillPool 复制进 skills）
        MakeUnitWithClass("战士", "盾牌猛击", "嘲讽", "援护");
        var matched = unit.ApplyExclusiveSkillFromId("援护");

        // 模拟 BattleSetup 的 skills 复制逻辑
        unit.skills = new List<SkillData>(unit.classData.skillPool);

        Assert.IsNotNull(matched);
        Assert.IsTrue(unit.skills.Exists(s => s.skillName == "援护"),
            "选中的专属技能应在 unit.skills 中可被找到（用于技能栏显示 ★ 标记）");
    }

    [Test]
    public void BattleSetupSync_MimicFlow_NamedWithId_ResolvesAndAssigns()
    {
        // 模拟 BattleSetup.CreateUnit 里的同步流程：UnitBattleData 持久化字段 → UnitData.exclusiveSkill
        MakeUnitWithClass("战士", "盾牌猛击", "嘲讽");
        var battleData = new UnitBattleData
        {
            unitName = "战士",
            isNamed = true,
            stressCapBonus = 20,
            exclusiveSkillId = "嘲讽"
        };

        // 同步
        unit.isNamed = battleData.isNamed;
        unit.stressCapBonus = battleData.stressCapBonus;
        var matched = unit.ApplyExclusiveSkillFromId(battleData.exclusiveSkillId);

        Assert.IsTrue(unit.isNamed);
        Assert.AreEqual(20, unit.stressCapBonus);
        Assert.IsNotNull(matched);
        Assert.AreEqual("嘲讽", unit.exclusiveSkill.skillName);
    }

    [Test]
    public void BattleSetupSync_MimicFlow_UnnamedHero_NoAssign()
    {
        MakeUnitWithClass("X", "A", "B");
        var battleData = new UnitBattleData
        {
            unitName = "X",
            isNamed = false,
            exclusiveSkillId = "A"
        };

        // 未命名时 BattleSetup 会跳过同步
        if (battleData.isNamed && !string.IsNullOrEmpty(battleData.exclusiveSkillId))
        {
            unit.ApplyExclusiveSkillFromId(battleData.exclusiveSkillId);
        }

        Assert.IsNull(unit.exclusiveSkill, "未命名英雄不应被同步专属技能");
    }

    // ==================== 8. V0.5 分配属性点 (AllocatePoint) ====================

    [Test]
    public void AllocatePoint_VIT_IncrementsVIT()
    {
        var u = new UnitBattleData { unitName = "测试", VIT = 10, unassignedPoints = 3 };
        bool ok = u.AllocatePoint(StatType.VIT);
        Assert.IsTrue(ok);
        Assert.AreEqual(11, u.VIT);
        Assert.AreEqual(2, u.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_STR_IncrementsSTR()
    {
        var u = new UnitBattleData { unitName = "测试", STR = 10, unassignedPoints = 1 };
        bool ok = u.AllocatePoint(StatType.STR);
        Assert.IsTrue(ok);
        Assert.AreEqual(11, u.STR);
        Assert.AreEqual(0, u.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_AGI_IncrementsAGI()
    {
        var u = new UnitBattleData { unitName = "测试", AGI = 5, unassignedPoints = 1 };
        u.AllocatePoint(StatType.AGI);
        Assert.AreEqual(6, u.AGI);
        Assert.AreEqual(0, u.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_INT_IncrementsINT()
    {
        var u = new UnitBattleData { unitName = "测试", INT = 3, unassignedPoints = 2 };
        u.AllocatePoint(StatType.INT);
        Assert.AreEqual(4, u.INT);
        Assert.AreEqual(1, u.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_NoPoints_ReturnsFalse()
    {
        var u = new UnitBattleData { unitName = "测试", VIT = 10, unassignedPoints = 0 };
        bool ok = u.AllocatePoint(StatType.VIT);
        Assert.IsFalse(ok, "unassignedPoints=0 时应返回 false");
        Assert.AreEqual(10, u.VIT);
    }

    [Test]
    public void AllocatePoint_InvalidStat_ReturnsFalse()
    {
        var u = new UnitBattleData { unitName = "测试", unassignedPoints = 5 };
        Assert.IsFalse(u.AllocatePoint(StatType.SPD), "SPD 是派生属性，不可分配");
        Assert.IsFalse(u.AllocatePoint(StatType.DEF), "DEF 是派生属性，不可分配");
        Assert.IsFalse(u.AllocatePoint(StatType.MaxHP), "MaxHP 是派生属性，不可分配");
        Assert.AreEqual(5, u.unassignedPoints, "失败时点数不变");
    }

    [Test]
    public void AllocatePoint_MultipleAllocations_DecrementsCounter()
    {
        var u = new UnitBattleData
        {
            unitName = "测试",
            VIT = 10, STR = 10, AGI = 5, INT = 3,
            unassignedPoints = 9
        };
        for (int i = 0; i < 3; i++) u.AllocatePoint(StatType.VIT);
        for (int i = 0; i < 3; i++) u.AllocatePoint(StatType.STR);
        for (int i = 0; i < 3; i++) u.AllocatePoint(StatType.AGI);

        Assert.AreEqual(13, u.VIT);
        Assert.AreEqual(13, u.STR);
        Assert.AreEqual(8, u.AGI);
        Assert.AreEqual(3, u.INT);
        Assert.AreEqual(0, u.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_DoesNotChangeUnrelatedStats()
    {
        var u = new UnitBattleData
        {
            unitName = "测试",
            VIT = 10, STR = 10, AGI = 5, INT = 3,
            unassignedPoints = 1
        };
        u.AllocatePoint(StatType.STR);
        Assert.AreEqual(10, u.VIT, "分配 STR 不应影响 VIT");
        Assert.AreEqual(5, u.AGI, "分配 STR 不应影响 AGI");
        Assert.AreEqual(3, u.INT, "分配 STR 不应影响 INT");
    }

    [Test]
    public void UnitBattleData_DefaultUnassignedPoints_IsZero()
    {
        var u = new UnitBattleData { unitName = "测试" };
        Assert.AreEqual(0, u.unassignedPoints, "默认未分配点应为 0");
    }

    // ==================== 9. 持久化 round-trip (unassignedPoints) ====================

    [Test]
    public void Persistence_UnassignedPoints_RoundTrip()
    {
        // 模拟战斗结束写回 + 下次战斗读出
        var data = new UnitBattleData { unitName = "战士", unassignedPoints = 6 };
        // 写：随 SaveData 保存
        var saved = JsonUtility.ToJson(data);
        // 读：下次启动
        var loaded = JsonUtility.FromJson<UnitBattleData>(saved);
        Assert.AreEqual(6, loaded.unassignedPoints, "unassignedPoints 应能跨会话保留");
    }

    [Test]
    public void BattleSetupSync_MimicFlow_AllocatedPointsFlowToUnit()
    {
        // 玩家在英雄厅分配：UnitBattleData.VIT+1, unassignedPoints-1
        // 下次 BattleSetup.CreateUnit 同步到 UnitData
        MakeUnitWithClass("战士", "盾牌猛击");
        var data = new UnitBattleData
        {
            unitName = "战士",
            VIT = 11,        // 已分配 1 点
            unassignedPoints = 2  // 还剩 2 点
        };
        // 模拟 BattleSetup.CreateUnit 的同步
        unit.VIT = data.VIT;
        unit.unassignedPoints = data.unassignedPoints;
        Assert.AreEqual(11, unit.VIT);
        Assert.AreEqual(2, unit.unassignedPoints);
    }

    [Test]
    public void BattleSetupSync_MimicFlow_BattleEndWritesBackUnassignedPoints()
    {
        // 模拟 BattleManager.ReturnToMap 写回
        MakeUnitWithClass("战士", "盾牌猛击");
        unit.unassignedPoints = 5;  // 战斗升级累加
        unit.VIT = 12;                // 之前英雄厅已分配过
        var data = new UnitBattleData { unitName = "战士" };
        // 写回
        data.VIT = unit.VIT;
        data.unassignedPoints = unit.unassignedPoints;
        Assert.AreEqual(12, data.VIT);
        Assert.AreEqual(5, data.unassignedPoints);
    }
}
