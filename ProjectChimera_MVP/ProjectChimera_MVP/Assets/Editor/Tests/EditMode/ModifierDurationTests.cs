using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ModifierDurationTests
{
    GameObject _go;
    UnitData _unit;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestUnit");
        _unit = _go.AddComponent<UnitData>();
        _unit.unitName = "TestHero";
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    // 辅助：直接向 modifiers 添加一个 mod
    static AttributeModifier MakeMod(AttributeTarget target, float value, int duration, string source)
    {
        return new AttributeModifier(ModifierType.Add, target, value, source, duration);
    }

    // 用例 1：duration=3 的 buff，调 3 次后被移除
    [Test]
    public void TickModifiers_ThreeDuration_RemovedAfterThreeTicks()
    {
        _unit.modifiers.Add(MakeMod(AttributeTarget.STR, 5f, 3, "test_buff"));

        _unit.TickModifiers(); // 3 -> 2
        _unit.TickModifiers(); // 2 -> 1
        _unit.TickModifiers(); // 1 -> 0, 移除

        Assert.AreEqual(0, _unit.modifiers.Count, "duration=3 的 buff 在 3 次 Tick 后应被移除");
    }

    // 用例 2：duration=3 调 1 次后 remainingTurns 减为 2，仍在列表中
    [Test]
    public void TickModifiers_OneTick_DecrementsRemainingTurnsAndKeepsInList()
    {
        var mod = MakeMod(AttributeTarget.STR, 5f, 3, "test_buff");
        _unit.modifiers.Add(mod);

        _unit.TickModifiers();

        Assert.AreEqual(1, _unit.modifiers.Count, "调 1 次后仍应在列表中");
        Assert.AreEqual(2, _unit.modifiers[0].remainingTurns, "remainingTurns 应从 3 减为 2");
        Assert.AreEqual(3, _unit.modifiers[0].duration, "duration 字段不应被修改");
    }

    // 用例 3：duration=0（永久）的 buff 调任意次 Tick 都不移除
    [Test]
    public void TickModifiers_PermanentBuff_NeverRemoved()
    {
        _unit.modifiers.Add(MakeMod(AttributeTarget.STR, 5f, 0, "permanent_buff"));
        _unit.modifiers.Add(MakeMod(AttributeTarget.DEF, 3f, -1, "negative_duration_perm"));

        for (int i = 0; i < 10; i++) _unit.TickModifiers();

        Assert.AreEqual(2, _unit.modifiers.Count, "duration<=0 的 buff 调任意次都不应被移除");
        // 永久 buff 不消耗回合
        Assert.AreEqual(0, _unit.modifiers[0].remainingTurns, "duration=0 时 remainingTurns 应始终为 0");
        Assert.AreEqual(-1, _unit.modifiers[1].remainingTurns, "duration=-1 时 remainingTurns 应始终为 -1");
    }

    // 用例 4：多个 modifier 共存时只移除到期的
    [Test]
    public void TickModifiers_MultipleMixed_OnlyExpiredRemoved()
    {
        _unit.modifiers.Add(MakeMod(AttributeTarget.STR, 5f, 1, "expire_first"));      // 1 回合
        _unit.modifiers.Add(MakeMod(AttributeTarget.SPD, 2f, 3, "keep_two_more"));     // 3 回合
        _unit.modifiers.Add(MakeMod(AttributeTarget.DEF, 1f, 0, "permanent"));          // 永久
        _unit.modifiers.Add(MakeMod(AttributeTarget.INT, 7f, 2, "expire_second"));     // 2 回合

        _unit.TickModifiers(); // expire_first 1->0 移除；keep_two_more 3->2；expire_second 2->1

        Assert.AreEqual(3, _unit.modifiers.Count, "第一次 Tick 后应剩 3 个（永久 + 1 个 3 回合 + 1 个 2 回合）");

        // 再 Tick 一次：keep_two_more 2->1；expire_second 1->0 移除
        _unit.TickModifiers();
        Assert.AreEqual(2, _unit.modifiers.Count);

        // 校验剩下的内容
        var sources = new List<string>();
        foreach (var m in _unit.modifiers) sources.Add(m.source);
        CollectionAssert.Contains(sources, "keep_two_more");
        CollectionAssert.Contains(sources, "permanent");
        CollectionAssert.DoesNotContain(sources, "expire_first");
        CollectionAssert.DoesNotContain(sources, "expire_second");
    }

    // 用例 5：AttributeModifier.IsExpired 在 duration=0 时永远 false
    [Test]
    public void IsExpired_PermanentModifier_AlwaysFalse()
    {
        var perm = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "perm", 0);
        Assert.IsFalse(perm.IsExpired, "duration=0 时 IsExpired 应为 false");

        var negative = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "neg", -1);
        Assert.IsFalse(negative.IsExpired, "duration=-1 时 IsExpired 应为 false");
    }

    [Test]
    public void IsExpired_TimedModifier_TrueOnlyWhenRemainingZero()
    {
        var m = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "timed", 3);
        Assert.IsFalse(m.IsExpired, "刚构造（remaining=3）时不应过期");

        m.remainingTurns = 1;
        Assert.IsFalse(m.IsExpired, "remaining=1 时不应过期");

        m.remainingTurns = 0;
        Assert.IsTrue(m.IsExpired, "remaining=0 且 duration>0 时应过期");

        m.remainingTurns = -5;
        Assert.IsTrue(m.IsExpired, "remaining<0 也视为过期");
    }

    // 用例 6：验证倒序遍历的安全行为 — 多个相邻到期项应全部被移除
    // （正向遍历同一 list 并 RemoveAt 会跳过后续项；此处作为回归保护）
    [Test]
    public void TickModifiers_AllExpireInSameTick_AllRemovedSafely()
    {
        _unit.modifiers.Add(MakeMod(AttributeTarget.STR, 1f, 1, "a"));
        _unit.modifiers.Add(MakeMod(AttributeTarget.SPD, 1f, 1, "b"));
        _unit.modifiers.Add(MakeMod(AttributeTarget.DEF, 1f, 1, "c"));
        _unit.modifiers.Add(MakeMod(AttributeTarget.INT, 1f, 1, "d"));

        _unit.TickModifiers(); // 全部到期

        Assert.AreEqual(0, _unit.modifiers.Count, "同 tick 内全部到期的项应都被移除（验证倒序遍历）");
    }

    // 额外：空列表调用不抛异常
    [Test]
    public void TickModifiers_EmptyList_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _unit.TickModifiers());
        Assert.AreEqual(0, _unit.modifiers.Count);
    }

    // 额外：构造时 remainingTurns 自动等于 duration
    [Test]
    public void Constructor_AssignsRemainingTurnsFromDuration()
    {
        var m = new AttributeModifier(ModifierType.Mul, AttributeTarget.SPD, 0.25f, "haste", 5);
        Assert.AreEqual(5, m.remainingTurns, "构造时应自动写 remainingTurns=duration");

        var p = new AttributeModifier(ModifierType.Add, AttributeTarget.SPD, 1f, "perm", 0);
        Assert.AreEqual(0, p.remainingTurns, "永久 buff 构造时 remainingTurns 也应等于 0");
    }
}
