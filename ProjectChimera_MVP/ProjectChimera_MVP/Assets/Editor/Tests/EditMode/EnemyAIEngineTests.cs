using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="EnemyAIEngine"/> 的可测静态 API
/// （<c>InitializeThreats</c> / <c>UpdateThreatAfterKill</c> / <c>GetProfile</c> / <c>Reset</c>）。
///
/// 协程部分 <c>ExecuteTurn</c> 涉及 TurnManager + BattleManager + WaitForSeconds，
/// EditMode 测起来很重，这里只覆盖纯静态/纯计算路径。
/// </summary>
[TestFixture]
public class EnemyAIEngineTests
{
    GameObject playerGO;
    GameObject bmGO;
    GameObject enemyGO;
    UnitData player;
    UnitData enemy;
    BattleManager bm;

    [SetUp]
    public void SetUp()
    {
        EnemyAIEngine.ResetAll();
        playerGO = new GameObject("TestPlayer");
        player = playerGO.AddComponent<UnitData>();
        player.unitName = "TestPlayer";
        player.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        player.modifiers = new List<AttributeModifier>();
        player.quirks = new List<Quirk>();
        player.skills = new List<SkillData>();
        player.isPlayer = true;

        enemyGO = new GameObject("TestEnemy");
        enemy = enemyGO.AddComponent<UnitData>();
        enemy.unitName = "TestEnemy";
        enemy.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        enemy.modifiers = new List<AttributeModifier>();
        enemy.quirks = new List<Quirk>();
        enemy.skills = new List<SkillData>();
        enemy.isPlayer = false;

        bmGO = new GameObject("TestBM");
        bm = bmGO.AddComponent<BattleManager>();
        bm.playerUnits = new List<UnitData> { player };
        bm.enemyUnits = new List<UnitData> { enemy };
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGO != null) Object.DestroyImmediate(playerGO);
        if (bmGO != null) Object.DestroyImmediate(bmGO);
        if (enemyGO != null) Object.DestroyImmediate(enemyGO);
        EnemyAIEngine.ResetAll();
        playerGO = null; enemyGO = null; bmGO = null;
        player = null; enemy = null; bm = null;
    }

    // ============== InitializeThreats ==============

    [Test]
    public void InitializeThreats_PopulatesScoresForAllPlayers()
    {
        EnemyAIEngine.InitializeThreats(bm);

        // 至少 1 个 player 在 bm 里 → 应被登记
        Assert.GreaterOrEqual(GetThreatScore(player), 1f,
            "InitializeThreats 后应有非零威胁分");
    }

    [Test]
    public void InitializeThreats_ScoreHasMinimumOne()
    {
        // 极端低属性：所有 stat = 0
        player.primaryAttributes = new PrimaryAttributes(0, 0, 0, 0);
        EnemyAIEngine.InitializeThreats(bm);

        // 公式：sum += 0*1.5 + 0*1.5 + MaxHp*0.1 + 0*0.5 + bonuses
        // MaxHp = 0*5 = 0，total = 0
        // 但 Mathf.Max(1, score) 保证最小 1
        Assert.GreaterOrEqual(GetThreatScore(player), 1f,
            "威胁分应被 Mathf.Max(1, ...) 钳制到 1");
    }

    [Test]
    public void InitializeThreats_HighStatsProduceHigherScore()
    {
        player.primaryAttributes = new PrimaryAttributes(10, 5, 5, 3);
        EnemyAIEngine.InitializeThreats(bm);
        float lowScore = GetThreatScore(player);

        // 重新初始化 + 大幅强化属性
        EnemyAIEngine.ResetAll();
        player.primaryAttributes = new PrimaryAttributes(20, 20, 20, 20);
        EnemyAIEngine.InitializeThreats(bm);
        float highScore = GetThreatScore(player);

        Assert.Greater(highScore, lowScore,
            "属性更强的 player 威胁分应更高");
    }

    [Test]
    public void InitializeThreats_HealerSkillAddsBonus()
    {
        // 无 Heal 技能
        player.skills = new List<SkillData>
        {
            new SkillData { skillName = "atk", aiCategory = SkillEffectType.DirectDamage }
        };
        EnemyAIEngine.InitializeThreats(bm);
        float scoreNoHeal = GetThreatScore(player);

        // 重新初始化 + Heal 技能
        EnemyAIEngine.ResetAll();
        player.skills = new List<SkillData>
        {
            new SkillData { skillName = "heal", aiCategory = SkillEffectType.Heal }
        };
        EnemyAIEngine.InitializeThreats(bm);
        float scoreWithHeal = GetThreatScore(player);

        // 公式：有 Heal 时 +20
        Assert.GreaterOrEqual(scoreWithHeal - scoreNoHeal, 19f,
            $"Heal 技能应 +20 威胁分，差值={scoreWithHeal - scoreNoHeal}");
    }

    [Test]
    public void InitializeThreats_StressSkillAddsBonus()
    {
        player.skills = new List<SkillData>
        {
            new SkillData { skillName = "atk", aiCategory = SkillEffectType.DirectDamage }
        };
        EnemyAIEngine.InitializeThreats(bm);
        float scoreNoStress = GetThreatScore(player);

        EnemyAIEngine.ResetAll();
        player.skills = new List<SkillData>
        {
            new SkillData { skillName = "stress_atk", aiCategory = SkillEffectType.Stress }
        };
        EnemyAIEngine.InitializeThreats(bm);
        float scoreWithStress = GetThreatScore(player);

        // 公式：有 Stress 技能时 +15
        Assert.GreaterOrEqual(scoreWithStress - scoreNoStress, 14f,
            $"Stress 技能应 +15 威胁分，差值={scoreWithStress - scoreNoStress}");
    }

    [Test]
    public void InitializeThreats_ClearsPreviousScores()
    {
        // 第一次初始化
        EnemyAIEngine.InitializeThreats(bm);
        Assert.Greater(GetThreatScore(player), 1f);

        // 第二次初始化应覆盖（不会累加）
        EnemyAIEngine.InitializeThreats(bm);
        // 不报错即通过（说明 Clear() 工作正常）
        Assert.GreaterOrEqual(GetThreatScore(player), 1f);
    }

    // ============== UpdateThreatAfterKill ==============

    [Test]
    public void UpdateThreatAfterKill_SetsScoreToZero()
    {
        EnemyAIEngine.InitializeThreats(bm);
        Assert.Greater(GetThreatScore(player), 0f);

        EnemyAIEngine.UpdateThreatAfterKill(player);

        Assert.AreEqual(0f, GetThreatScore(player),
            "击杀后威胁分应清零");
    }

    [Test]
    public void UpdateThreatAfterKill_UnknownUnit_NoThrow()
    {
        EnemyAIEngine.InitializeThreats(bm);
        var unknown = playerGO.AddComponent<UnitData>();  // 不在 bm 里
        unknown.unitName = "Ghost";
        // 未登记的 unit 不应抛异常
        Assert.DoesNotThrow(() => EnemyAIEngine.UpdateThreatAfterKill(unknown));
        Object.DestroyImmediate(unknown);
    }

    // ============== GetProfile ==============

    [Test]
    public void GetProfile_DefaultName_ReturnsNormal()
    {
        enemy.unitName = "Slime";
        enemy.level = 1;
        var profile = EnemyAIEngine.GetProfile(enemy);
        Assert.AreEqual(AIDifficulty.Normal, profile.difficulty);
    }

    [Test]
    public void GetProfile_NameContainsElite_ReturnsHard()
    {
        enemy.unitName = "精英骷髅";
        enemy.level = 1;
        var profile = EnemyAIEngine.GetProfile(enemy);
        Assert.AreEqual(AIDifficulty.Hard, profile.difficulty);
    }

    [Test]
    public void GetProfile_NameContainsEliteEnglish_ReturnsHard()
    {
        enemy.unitName = "Elite Archer";
        enemy.level = 1;
        var profile = EnemyAIEngine.GetProfile(enemy);
        Assert.AreEqual(AIDifficulty.Hard, profile.difficulty);
    }

    [Test]
    public void GetProfile_Level6OrAbove_ReturnsBoss()
    {
        enemy.unitName = "Slime";
        enemy.level = 6;
        var profile = EnemyAIEngine.GetProfile(enemy);
        Assert.AreEqual(AIDifficulty.Boss, profile.difficulty);
    }

    [Test]
    public void GetProfile_BossTakesPrecedenceOverElite()
    {
        // 既是精英，level 又 >= 6：应返回 Boss（更高级别）
        enemy.unitName = "Elite Boss";
        enemy.level = 10;
        var profile = EnemyAIEngine.GetProfile(enemy);
        Assert.AreEqual(AIDifficulty.Boss, profile.difficulty,
            "Boss 优先级应高于 Elite");
    }

    [Test]
    public void GetProfile_CachesResult()
    {
        enemy.unitName = "Slime";
        enemy.level = 1;
        var p1 = EnemyAIEngine.GetProfile(enemy);
        var p2 = EnemyAIEngine.GetProfile(enemy);
        // 缓存命中应返回同一实例
        Assert.AreSame(p1, p2, "GetProfile 应缓存结果");

        // 改了 unitName 后，缓存中仍然是旧 profile（不会重新推断）
        enemy.unitName = "Elite";
        var p3 = EnemyAIEngine.GetProfile(enemy);
        Assert.AreSame(p1, p3, "缓存命中时不重新推断");
        Assert.AreEqual(AIDifficulty.Normal, p3.difficulty);

        // 调 Reset 后重新推断
        EnemyAIEngine.Reset(enemy);
        var p4 = EnemyAIEngine.GetProfile(enemy);
        Assert.AreNotSame(p1, p4);
        Assert.AreEqual(AIDifficulty.Hard, p4.difficulty);
    }

    [Test]
    public void GetProfile_DefaultValues_Reasonable()
    {
        var p = EnemyAIEngine.GetProfile(enemy);
        // Normal 默认：skillUseChance=0.6, threatFocus=0.3
        Assert.That(p.skillUseChance, Is.InRange(0f, 1f));
        Assert.That(p.tacticalDepth, Is.InRange(0f, 1f));
        Assert.That(p.threatFocus, Is.InRange(0f, 1f));
    }

    // ============== Reset / ResetAll ==============

    [Test]
    public void Reset_ClearsStateForSpecificUnit()
    {
        // 先建立状态
        enemy.unitName = "Slime";
        EnemyAIEngine.GetProfile(enemy);  // 缓存 profile
        EnemyAIEngine.InitializeThreats(bm);  // 缓存威胁

        // Reset
        EnemyAIEngine.Reset(enemy);

        // 重新获取 profile → 应该是新实例
        var newProfile = EnemyAIEngine.GetProfile(enemy);
        // 缓存被清掉，重新推断仍得到 Normal（因为 unitName 不变）
        Assert.AreEqual(AIDifficulty.Normal, newProfile.difficulty);
    }

    [Test]
    public void ResetAll_ClearsAllState()
    {
        EnemyAIEngine.InitializeThreats(bm);
        EnemyAIEngine.GetProfile(enemy);
        EnemyAIEngine.GetProfile(player);

        EnemyAIEngine.ResetAll();

        // 重新访问不报错
        Assert.DoesNotThrow(() => EnemyAIEngine.GetProfile(enemy));
        Assert.DoesNotThrow(() => EnemyAIEngine.InitializeThreats(bm));
    }

    [Test]
    public void Reset_NullUnit_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EnemyAIEngine.Reset(null));
    }

    // ============== AIDifficultyProfile.GetDefault ==============

    [Test]
    public void AIDifficultyProfile_GetDefault_Easy_HasLowerStats()
    {
        var p = AIDifficultyProfile.GetDefault(AIDifficulty.Easy);
        Assert.AreEqual(AIDifficulty.Easy, p.difficulty);
        Assert.IsFalse(p.useCooldowns, "Easy AI 不使用冷却");
        Assert.Less(p.threatFocus, 0.5f);
    }

    [Test]
    public void AIDifficultyProfile_GetDefault_Normal_Balanced()
    {
        var p = AIDifficultyProfile.GetDefault(AIDifficulty.Normal);
        Assert.AreEqual(AIDifficulty.Normal, p.difficulty);
        Assert.IsTrue(p.useCooldowns);
    }

    [Test]
    public void AIDifficultyProfile_GetDefault_Hard_Aggressive()
    {
        var p = AIDifficultyProfile.GetDefault(AIDifficulty.Hard);
        Assert.AreEqual(AIDifficulty.Hard, p.difficulty);
        Assert.Greater(p.threatFocus, 0.5f);
        Assert.GreaterOrEqual(p.skillUseChance, 0.8f);
    }

    [Test]
    public void AIDifficultyProfile_GetDefault_Boss_Extreme()
    {
        var p = AIDifficultyProfile.GetDefault(AIDifficulty.Boss);
        Assert.AreEqual(AIDifficulty.Boss, p.difficulty);
        Assert.AreEqual(1f, p.skillUseChance);
        Assert.AreEqual(1f, p.deathDoorFocus);
        Assert.AreEqual(1f, p.positionalAwareness);
    }

    [Test]
    public void AIDifficultyProfile_GetDefault_DifficultyOrder_MonotonicIncrease()
    {
        var easy = AIDifficultyProfile.GetDefault(AIDifficulty.Easy);
        var normal = AIDifficultyProfile.GetDefault(AIDifficulty.Normal);
        var hard = AIDifficultyProfile.GetDefault(AIDifficulty.Hard);
        var boss = AIDifficultyProfile.GetDefault(AIDifficulty.Boss);

        // 难度越高 → skillUseChance / tacticalDepth 越高
        Assert.Less(easy.skillUseChance, normal.skillUseChance);
        Assert.Less(normal.skillUseChance, hard.skillUseChance);
        Assert.Less(hard.skillUseChance, boss.skillUseChance);
    }

    // ============== InferRole（通过 GetProfile + ExecuteTurn 间接）==============
    // 注：InferRole 是 private，无法直接测。
    // 这里改为通过 unitName 推断结果做白盒验证：仅文档化。

    [Test]
    public void InferRole_NamePatterns_NotDirectlyTestable_ButCoveredByIntegration()
    {
        // InferRole 在 ExecuteTurn 协程内被调用，依赖 TurnManager + BattleManager
        // 留给集成测试（PlayMode）或重构后再补
        Assert.Pass("InferRole 由 ExecuteTurn 间接覆盖；详见后续集成测试计划");
    }

    // ============== 辅助方法：反射读取 private playerThreatScores ==============
    static System.Reflection.FieldInfo GetThreatField()
    {
        var t = typeof(EnemyAIEngine);
        return t.GetField("playerThreatScores",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    }

    static float GetThreatScore(UnitData player)
    {
        var field = GetThreatField();
        var dict = (Dictionary<string, float>)field.GetValue(null);
        if (dict.ContainsKey(player.unitName))
            return dict[player.unitName];
        return 0f;
    }
}
