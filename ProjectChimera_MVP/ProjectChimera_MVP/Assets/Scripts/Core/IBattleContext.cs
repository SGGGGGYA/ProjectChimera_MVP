using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗 UI/状态上下文 — TurnManager / Command / StressManager 共用的 BattleManager 能力抽象。
/// 定义在 ProjectChimera.Core（基础设施层），由 ProjectChimera.Battle 中的 BattleManager 实现。
/// 让 TurnManager / Command / StressManager 不再直接引用 BattleManager 类，消除循环依赖。
///
/// 调用方需要：
///   - 查询战斗是否结束（IsBattleOver）
///   - 在角色头顶弹出伤害/状态数字（SpawnDamagePopup / SpawnTextPopup）
///   - 真正结算伤害（DealDamage）
///   - 查询玩家/敌人编队（PlayerUnits / EnemyUnits）
///   - 查询指定编队中第一个存活单位（GetFirstAlive）
/// </summary>
public interface IBattleContext
{
    /// <summary>战斗是否已结束（任何一边全灭）。</summary>
    bool IsBattleOver { get; }

    /// <summary>玩家阵营单位列表（只读）。</summary>
    IReadOnlyList<UnitData> PlayerUnits { get; }

    /// <summary>敌人阵营单位列表（只读）。</summary>
    IReadOnlyList<UnitData> EnemyUnits { get; }

    /// <summary>返回指定列表中第一个 HP > 0 的单位；找不到返回 null。</summary>
    UnitData GetFirstAlive(IReadOnlyList<UnitData> units);

    /// <summary>在指定世界坐标弹出伤害数字（红/绿区分伤害/治疗）。</summary>
    void SpawnDamagePopup(Vector3 worldPos, int amount, PopupType type);

    /// <summary>在指定世界坐标弹出自定义文字（崩溃名/抵抗等）。</summary>
    void SpawnTextPopup(Vector3 worldPos, string text, Color color, float fontSize = 4f);

    /// <summary>结算单次伤害（由 BattleManager / NullBattleContext 实现）。</summary>
    void DealDamage(UnitData attacker, UnitData target, int damage, bool isCrit = false);

    /// <summary>是否处于"选目标"模式（玩家点了技能后等待点击目标的中间态）。</summary>
    bool IsInTargetingMode();

    /// <summary>在选目标模式下，给定单位是否是合法可点击的目标。</summary>
    bool IsValidTarget(UnitData unit);

    /// <summary>逃生通道：返回实现者的具体类型（BattleManager / NullBattleContext）。
    /// 仅在 IBattleContext 抽象能力不足时（例如 EnemyAIEngine.ExecuteSkill 因为 SkillData
    /// 在 Data 程序集、不能放上 Core 中的接口）作为过渡使用。调用方需要 downcast 到具体类型。
    /// 新代码应优先扩展 IBattleContext 接口（把所需类型也搬到 Core）而不是走此通道。</summary>
    object RawContext { get; }
}

/// <summary>
/// BattleManager 端的可空实现：把 BattleManager 的字段/方法包成 IBattleContext，
/// 由 BattleSetup 注入到 TurnManager 中。如果 BattleManager 还未注入，
/// TurnManager 的相关调用会安静跳过（与原 FindObjectOfType==null 行为一致）。
/// </summary>
public class NullBattleContext : IBattleContext
{
    public bool IsBattleOver => false;
    public IReadOnlyList<UnitData> PlayerUnits => System.Array.Empty<UnitData>();
    public IReadOnlyList<UnitData> EnemyUnits => System.Array.Empty<UnitData>();
    public UnitData GetFirstAlive(IReadOnlyList<UnitData> units) => null;
    public void SpawnDamagePopup(Vector3 worldPos, int amount, PopupType type) { }
    public void SpawnTextPopup(Vector3 worldPos, string text, Color color, float fontSize = 4f) { }
    public void DealDamage(UnitData attacker, UnitData target, int damage, bool isCrit = false) { }
    public bool IsInTargetingMode() => false;
    public bool IsValidTarget(UnitData unit) => false;
    public object RawContext => null;
}
