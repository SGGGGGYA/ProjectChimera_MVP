// Core/IUnitBars.cs
// Core ↔ UI 的反向桥:UnitData 字段需要能引用血条/压力条组件,但 Core 不能直接依赖 UI 程序集。
// 解法:在 Core 定义抽象接口(纯行为,不依赖 UI),让 UI 的 HPBarFollower / StressBarFollower 实现它们。
// UnitData 持有 Component 引用(Inspector 仍可拖拽),在调用处 `is IUnitHPBar` 转型后调用接口方法。
using UnityEngine;

/// <summary>血条跟随者抽象接口。UI.HPBarFollower 实现它。</summary>
public interface IUnitHPBar
{
    void SetTarget(Transform t);
    void UpdateHP(int currentHP, int maxHP);
    void SetDeathsDoor(bool active);
}

/// <summary>压力条跟随者抽象接口。UI.StressBarFollower 实现它。</summary>
public interface IUnitStressBar
{
    void SetTarget(Transform t, UnitData unit);
    void Refresh();
}
