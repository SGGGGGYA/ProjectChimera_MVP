/// <summary>
/// 伤害 / 状态 弹窗类型。定义在 ProjectChimera.Core，供 IBattleContext 和 UI 层共用。
/// DamagePopup MonoBehaviour 也在 UI 侧引用此枚举（位于 Assets/Scripts/DamagePopup.cs）。
/// </summary>
public enum PopupType
{
    Damage,
    Crit,
    Heal,
    Miss,
    Shield,
    Stress
}
