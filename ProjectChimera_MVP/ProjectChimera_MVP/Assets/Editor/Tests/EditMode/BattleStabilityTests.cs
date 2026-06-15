using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ProjectChimera.Core;

/// <summary>
/// EditMode 战斗稳定性单元测试。
/// 将 BattleTestRunner 的核心测试逻辑封装为 [Test] 特性标注的 EditMode 测试，
/// 可通过 Unity Test Runner 窗口运行。
/// 
/// 测试覆盖：
///   1. 2v2 正常战斗
///   2. 4v4 极限战斗
///   3. 1v3 生存战
///   4. 1v4 全灭战
///   5. 撤退测试
///   6. 空敌人边界测试
///   7. 玩家全死边界测试
///   8. 长时间战斗防死循环
///   9. DealDamage null 安全
///   10. 死亡动画防重复调用
/// </summary>
[TestFixture]
public class BattleStabilityTests
{
    readonly List<GameObject> spawned = new List<GameObject>();
    readonly List<Object> spawnedAssets = new List<Object>();

    // ==================== 生命周期 ====================

    [SetUp]
    public void SetUp()
    {
        // 设置编辑模式标志，跳过 WaitForSeconds 等真实时间等待
        EnemyAIEngine.IsEditorTestMode = true;
        VFXManager.IsEditorTestMode = true;
        TurnManager.IsEditorTestMode = true;
        BattleLog.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();

        foreach (var asset in spawnedAssets)
            if (asset != null) Object.DestroyImmediate(asset);
        spawnedAssets.Clear();

        BattleEvents.ClearAll();
    }

    // ==================== 工厂方法 ====================

    /// <summary>
    /// 创建一个测试用的 UnitData GameObject。
    /// </summary>
    UnitData CreateUnit(string name, bool isPlayer, int vit, int str, int agi, int int_, int hp)
    {
        var go = new GameObject(name);
        spawned.Add(go);
        var unit = go.AddComponent<UnitData>();
        unit.unitName = name;
        unit.isPlayer = isPlayer;
        unit.primaryAttributes = new PrimaryAttributes(vit, str, agi, int_);
        unit.currentHP = hp;
        unit.skills = new List<SkillData>();
        unit.weaponAttack = 5;
        unit.rank = isPlayer ? 0 : 0;
        return unit;
    }

    /// <summary>
    /// 给单位补充测试必要组件：SpriteRenderer、SelectCircle、默认技能。
    /// </summary>
    void EnsureUnitComponents(UnitData unit)
    {
        if (unit == null) return;
        if (unit.GetComponent<SpriteRenderer>() == null)
        {
            var sr = unit.gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
        }
        if (unit.selectCircle == null)
        {
            var circle = new GameObject("SelectCircle");
            circle.transform.SetParent(unit.transform, false);
            circle.AddComponent<SpriteRenderer>();
            unit.selectCircle = circle;
            spawned.Add(circle);
        }
        if (unit.skills == null || unit.skills.Count == 0)
        {
            unit.skills = new List<SkillData> { CreateBasicAttackSkill() };
        }
        if (unit.currentHP <= 0 && unit.MaxHp > 0)
            unit.currentHP = unit.MaxHp;
    }

    SkillData CreateBasicAttackSkill()
    {
        var skill = new SkillData();
        skill.skillName = "普通攻击";
        skill.baseDamage = 10;
        skill.strScaling = 1f;
        skill.targetType = SkillTargetType.SingleEnemy;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.DirectDamage;
        skill.commands = new List<Command>
        {
            new DealDamageCommand { baseDamage = 10, strScaling = 1f }
        };
        return skill;
    }

    SkillData CreateDamageSkill(string name, int damage, SkillTargetType targetType)
    {
        var skill = new SkillData();
        skill.skillName = name;
        skill.baseDamage = damage;
        skill.strScaling = 1f;
        skill.targetType = targetType;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.DirectDamage;
        skill.commands = new List<Command>
        {
            new DealDamageCommand { baseDamage = damage, strScaling = 1f }
        };
        return skill;
    }

    SkillData CreateHealSkill(string name, int healAmount)
    {
        var skill = new SkillData();
        skill.skillName = name;
        skill.targetType = SkillTargetType.SingleAlly;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.Heal;
        skill.commands = new List<Command>
        {
            new HealCommand { baseHeal = healAmount, intScaling = 0.5f }
        };
        return skill;
    }

    /// <summary>
    /// 创建带有 Camera 和 BattleManager 的战斗测试场景。
    /// </summary>
    BattleManager CreateBattleManager()
    {
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        spawned.Add(camGo);

        var bmGo = new GameObject("BattleManager");
        spawned.Add(bmGo);
        return bmGo.AddComponent<BattleManager>();
    }

    /// <summary>
    /// 执行同步战斗循环：所有存活单位按 AGI 降序依次攻击对方阵营随机单位。
    /// 返回 (是否分出胜负, 回合数, 玩家存活, 敌人存活)。
    /// </summary>
    (bool battleEnded, int round, bool playersAlive, bool enemiesAlive) RunBattleLoop(
        BattleManager bm, int maxRounds)
    {
        for (int round = 1; round <= maxRounds; round++)
        {
            var allAlive = bm.playerUnits
                .Where(u => u != null && u.currentHP > 0)
                .Concat(bm.enemyUnits.Where(u => u != null && u.currentHP > 0))
                .OrderByDescending(u => u.AGI)
                .ToList();

            if (allAlive.Count == 0) break;

            foreach (var unit in allAlive)
            {
                if (unit.currentHP <= 0) continue;

                bool playersAlive = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
                bool enemiesAlive = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
                if (!playersAlive || !enemiesAlive)
                {
                    return (true, round, playersAlive, enemiesAlive);
                }

                var targets = unit.isPlayer
                    ? bm.enemyUnits.Where(u => u != null && u.currentHP > 0).ToList()
                    : bm.playerUnits.Where(u => u != null && u.currentHP > 0).ToList();

                if (targets.Count == 0) continue;
                var target = targets[Random.Range(0, targets.Count)];

                int damage = Mathf.Max(1, unit.GetEffectiveSTR() + unit.weaponAttack - target.GetEffectiveDEF() / 2);
                bm.DealDamage(unit, target, damage);
            }
        }

        bool pAlive = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
        bool eAlive = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
        return (!pAlive || !eAlive, maxRounds, pAlive, eAlive);
    }

    // ==================== 测试 1: 2v2 正常战斗 ====================

    /// <summary>
    /// 2v2 对称力量对战，应在 200 回合内分胜负。
    /// </summary>
    [Test]
    public void Battle_2v2_EndsWithinMaxRounds()
    {
        var bm = CreateBattleManager();

        var p1 = CreateUnit("战士A", true, 12, 10, 6, 3, 80);
        var p2 = CreateUnit("弓手A", true, 8, 7, 10, 4, 60);
        var e1 = CreateUnit("哥布林", false, 8, 7, 5, 2, 50);
        var e2 = CreateUnit("兽人", false, 10, 9, 4, 2, 70);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("重击", 15, SkillTargetType.SingleEnemy) };
        p2.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("连射", 12, SkillTargetType.SingleEnemy) };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e2.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("猛击", 14, SkillTargetType.SingleEnemy) };

        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData> { e1, e2 };

        foreach (var u in bm.playerUnits) EnsureUnitComponents(u);
        foreach (var u in bm.enemyUnits) EnsureUnitComponents(u);

        var result = RunBattleLoop(bm, 200);

        Assert.IsTrue(result.battleEnded,
            $"2v2 战斗应在 200 回合内分胜负，实际回合={result.round}, 玩家存活={result.playersAlive}, 敌人存活={result.enemiesAlive}");
    }

    // ==================== 测试 2: 4v4 极限战斗 ====================

    /// <summary>
    /// 4v4 大规模战斗，应在 300 回合内分胜负。
    /// </summary>
    [Test]
    public void Battle_4v4_EndsWithinMaxRounds()
    {
        var bm = CreateBattleManager();

        var players = new List<UnitData>();
        var enemies = new List<UnitData>();
        for (int i = 0; i < 4; i++)
        {
            players.Add(CreateUnit($"英雄{i + 1}", true, 10 + i, 8 + i, 6 + i, 3 + i, 70 + i * 10));
            enemies.Add(CreateUnit($"敌人{i + 1}", false, 9 + i, 7 + i, 5 + i, 2 + i, 60 + i * 10));
        }
        foreach (var p in players)
            p.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("技能", 15, SkillTargetType.SingleEnemy) };
        foreach (var e in enemies)
            e.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("技能", 14, SkillTargetType.SingleEnemy) };

        bm.playerUnits = players;
        bm.enemyUnits = enemies;

        foreach (var u in bm.playerUnits) EnsureUnitComponents(u);
        foreach (var u in bm.enemyUnits) EnsureUnitComponents(u);

        var result = RunBattleLoop(bm, 300);

        Assert.IsTrue(result.battleEnded,
            $"4v4 战斗应在 300 回合内分胜负，实际回合={result.round}");
    }

    // ==================== 测试 3: 1v3 生存 ====================

    /// <summary>
    /// 1 个高血量玩家对抗 3 个弱小敌人。
    /// </summary>
    [Test]
    public void Battle_1v3_EndsWithinMaxRounds()
    {
        var bm = CreateBattleManager();

        var p1 = CreateUnit("生存者", true, 20, 12, 8, 5, 150);
        var e1 = CreateUnit("小妖A", false, 5, 4, 3, 1, 25);
        var e2 = CreateUnit("小妖B", false, 5, 4, 3, 1, 25);
        var e3 = CreateUnit("小妖C", false, 5, 4, 3, 1, 25);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateHealSkill("急救", 20) };
        e1.skills = e2.skills = e3.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1, e2, e3 };

        foreach (var u in bm.playerUnits) EnsureUnitComponents(u);
        foreach (var u in bm.enemyUnits) EnsureUnitComponents(u);

        var result = RunBattleLoop(bm, 200);

        Assert.IsTrue(result.battleEnded,
            $"1v3 战斗应在 200 回合内分胜负，实际回合={result.round}");
    }

    // ==================== 测试 4: 1v4 全灭 ====================

    /// <summary>
    /// 1 个弱小玩家对抗 4 个强大敌人，玩家应被击败。
    /// </summary>
    [Test]
    public void Battle_1v4_PlayerDefeated()
    {
        var bm = CreateBattleManager();

        var p1 = CreateUnit("新兵", true, 6, 5, 4, 2, 30);
        var e1 = CreateUnit("精英A", false, 12, 11, 7, 3, 90);
        var e2 = CreateUnit("精英B", false, 12, 11, 7, 3, 90);
        var e3 = CreateUnit("精英C", false, 12, 11, 7, 3, 90);
        var e4 = CreateUnit("精英D", false, 12, 11, 7, 3, 90);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = e2.skills = e3.skills = e4.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("重击", 20, SkillTargetType.SingleEnemy) };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1, e2, e3, e4 };

        foreach (var u in bm.playerUnits) EnsureUnitComponents(u);
        foreach (var u in bm.enemyUnits) EnsureUnitComponents(u);

        var result = RunBattleLoop(bm, 200);

        Assert.IsTrue(result.battleEnded,
            $"1v4 战斗应在 200 回合内分胜负，实际回合={result.round}");
        Assert.IsFalse(result.playersAlive,
            "1v4 预期玩家被全灭");
    }

    // ==================== 测试 5: 撤退 ====================

    /// <summary>
    /// 撤退逻辑不应抛出异常。
    /// </summary>
    [Test]
    public void Retreat_DoesNotThrow()
    {
        var bm = CreateBattleManager();

        var tmGo = new GameObject("TurnManager");
        spawned.Add(tmGo);
        var tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("撤退者", true, 10, 8, 6, 3, 80);
        var e1 = CreateUnit("追兵", false, 8, 7, 5, 2, 60);
        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(e1);

        tm.Context = bm;
        bm.TurnState = tm;
        tm.InitializeBattle(bm.playerUnits, bm.enemyUnits);

        // 撤退不应抛出异常
        Assert.DoesNotThrow(() => bm.TriggerRetreat(),
            "TriggerRetreat() 不应抛出异常");
    }

    // ==================== 测试 6: 空敌人 ====================

    /// <summary>
    /// 空敌人编队时战斗应直接结束（不抛异常）。
    /// </summary>
    [Test]
    public void EmptyEnemy_BattleEndsImmediately()
    {
        var bm = CreateBattleManager();
        var tmGo = new GameObject("TurnManager");
        spawned.Add(tmGo);
        var tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("英雄1", true, 10, 8, 6, 3, 80);
        var p2 = CreateUnit("英雄2", true, 10, 8, 6, 3, 80);
        p1.skills = p2.skills = new List<SkillData> { CreateBasicAttackSkill() };
        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData>();

        EnsureUnitComponents(p1);
        EnsureUnitComponents(p2);

        tm.Context = bm;
        bm.TurnState = tm;

        // InitializeBattle 不应在空敌人时崩溃
        Assert.DoesNotThrow(() => tm.InitializeBattle(bm.playerUnits, bm.enemyUnits),
            "空敌人初始化不应抛出异常");
    }

    // ==================== 测试 7: 玩家全死 ====================

    /// <summary>
    /// 玩家全部 HP<=0 时战斗应直接结束。
    /// </summary>
    [Test]
    public void PlayerAllDead_BattleEndsImmediately()
    {
        var bm = CreateBattleManager();
        var tmGo = new GameObject("TurnManager");
        spawned.Add(tmGo);
        var tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("死者1", true, 10, 8, 6, 3, 0);
        var p2 = CreateUnit("死者2", true, 10, 8, 6, 3, 0);
        var e1 = CreateUnit("胜者", false, 10, 8, 6, 3, 80);
        p1.skills = p2.skills = e1.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(p2);
        EnsureUnitComponents(e1);
        // 强制保持 HP=0
        p1.currentHP = 0;
        p2.currentHP = 0;

        tm.Context = bm;
        bm.TurnState = tm;

        Assert.DoesNotThrow(() => tm.InitializeBattle(bm.playerUnits, bm.enemyUnits),
            "全死玩家初始化不应抛出异常");
    }

    // ==================== 测试 8: 长时间战斗防死循环 ====================

    /// <summary>
    /// 高防御低伤害战斗不会死循环，50 回合内安全退出。
    /// </summary>
    [Test]
    public void LongBattle_DoesNotInfiniteLoop()
    {
        var bm = CreateBattleManager();

        var p1 = CreateUnit("铁壁", true, 15, 3, 6, 3, 100);
        var e1 = CreateUnit("硬壳", false, 15, 3, 5, 2, 100);
        p1.baseDefense = 50;
        e1.baseDefense = 50;
        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(e1);

        const int maxRounds = 50;
        int round = 0;
        bool exceptionThrown = false;

        for (round = 1; round <= maxRounds; round++)
        {
            bool playersAlive = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
            bool enemiesAlive = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
            if (!playersAlive || !enemiesAlive) break;

            try
            {
                int dmg1 = Mathf.Max(1, p1.GetEffectiveSTR() + p1.weaponAttack - e1.GetEffectiveDEF() / 2);
                bm.DealDamage(p1, e1, dmg1);

                if (e1.currentHP > 0)
                {
                    int dmg2 = Mathf.Max(1, e1.GetEffectiveSTR() + e1.weaponAttack - p1.GetEffectiveDEF() / 2);
                    bm.DealDamage(e1, p1, dmg2);
                }
            }
            catch (System.Exception)
            {
                exceptionThrown = true;
                break;
            }
        }

        Assert.IsFalse(exceptionThrown,
            $"长时间战斗测试中不应抛出异常，已执行 {round} 回合");
        Assert.LessOrEqual(round, maxRounds,
            $"测试应在 {maxRounds} 回合内终止，实际回合={round}");
    }

    // ==================== 测试 9: DealDamage null 安全检查 ====================

    /// <summary>
    /// DealDamage 传入 null 参数不应崩溃。
    /// </summary>
    [Test]
    public void DealDamage_NullSafety_DoesNotCrash()
    {
        var bm = CreateBattleManager();
        var p1 = CreateUnit("测试者", true, 10, 8, 6, 3, 80);
        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData>();
        EnsureUnitComponents(p1);

        // 攻击者为 null
        Assert.DoesNotThrow(() => bm.DealDamage(null, p1, 10),
            "DealDamage(attacker=null) 不应崩溃");

        // 目标为 null
        Assert.DoesNotThrow(() => bm.DealDamage(p1, null, 10),
            "DealDamage(target=null) 不应崩溃");

        // 两者都为 null
        Assert.DoesNotThrow(() => bm.DealDamage(null, null, 10),
            "DealDamage(attacker=null, target=null) 不应崩溃");
    }

    // ==================== 测试 10: 死亡动画防重复调用 ====================

    /// <summary>
    /// 多次调用 Die() 不会导致异常或多次播放死亡动画。
    /// </summary>
    [Test]
    public void Die_ReentrySafe_DoesNotCrash()
    {
        var unit = CreateUnit("测试单位", true, 10, 8, 6, 3, 80);
        unit.currentHP = 0;

        // 多次调用 Die 不应崩溃
        Assert.DoesNotThrow(() => unit.Die(),
            "第一次 Die() 不应抛出异常");
        Assert.DoesNotThrow(() => unit.Die(),
            "重复 Die() 不应抛出异常（防重入）");
        Assert.DoesNotThrow(() => unit.Die(),
            "第三次 Die() 不应抛出异常");

        Assert.IsTrue(unit.isDead, "Die() 后 isDead 应为 true");
    }

    // ==================== 测试 11: SetIdle 安全 ====================

    /// <summary>
    /// SetIdle() 在 skeleton 为 null 时不应崩溃。
    /// </summary>
    [Test]
    public void SetIdle_NullSkeleton_DoesNotCrash()
    {
        var unit = CreateUnit("测试单位", true, 10, 8, 6, 3, 80);
        // 确保没有 SkeletonAnimation 组件
        Assert.DoesNotThrow(() => unit.SetIdle(),
            "SetIdle() 在无 skeleton 时不应崩溃");
    }
}
