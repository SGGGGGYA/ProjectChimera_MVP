using ProjectChimera.Core;
using UnityEngine;

/// <summary>
/// 特质触发系统 — 遍历 unit.quirks，匹配触发类型并执行对应的效果
/// </summary>
public static class QuirkTriggerSystem
{
    public static void CheckTriggers(UnitData unit, QuirkTriggerType trigger, object context = null)
    {
        if (unit == null || unit.currentHP <= 0) return;
        if (unit.quirks == null || unit.quirks.Count == 0) return;

        foreach (var quirk in unit.quirks)
        {
            if (quirk == null) continue;
            if (quirk.triggerType != trigger) continue;
            if (unit.IsQuirkOnCooldown(quirk.id)) continue;

            if (RandomProvider.Current.Value > quirk.procChance) continue;

            ApplyQuirkEffect(unit, quirk, context);
        }
    }

    static void ApplyQuirkEffect(UnitData unit, Quirk quirk, object context)
    {
        string name = string.IsNullOrEmpty(quirk.localizedName) ? quirk.id : quirk.localizedName;
        switch (quirk.id)
        {
            // ========== 正面特质 ==========

            case "zhanYiAngYang":
                unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "quirk_zhanYiAngYang"));
                unit.SetQuirkCooldown(quirk.id, int.MaxValue);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: STR +5");
                break;

            case "tieBi":
                unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.DEF, 3f, "quirk_tieBi"));
                unit.SetQuirkCooldown(quirk.id, 2);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: DEF +3 (2回合)");
                break;

            case "shiXue":
                int heal = Mathf.RoundToInt(unit.MaxHp * 0.1f);
                int actualHeal = Mathf.Min(heal, unit.MaxHp - unit.currentHP);
                unit.currentHP += actualHeal;
                unit.UpdateHPUI();
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 恢复 {actualHeal} HP");
                break;

            case "lengJing":
                int stressReduction = Mathf.Min(5, unit.stress);
                unit.stress -= stressReduction;
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 压力 -{stressReduction}");
                break;

            case "wanQiang":
                int healHp = Mathf.RoundToInt(unit.MaxHp * 0.5f);
                int actualHealHp = Mathf.Min(healHp, unit.MaxHp - unit.currentHP);
                unit.currentHP += actualHealHp;
                unit.UpdateHPUI();
                unit.SetQuirkCooldown(quirk.id, int.MaxValue);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 恢复 {actualHealHp} HP");
                break;

            // ========== 负面特质 ==========

            case "nuoRuo":
                unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, -3f, "quirk_nuoRuo"));
                unit.SetQuirkCooldown(quirk.id, int.MaxValue);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: STR -3");
                break;

            case "cuiRuo":
                StressManager.AddStress(unit, 2, StressTag.Combat);
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 压力 +2");
                break;

            case "buXiangYuGan":
                StressManager.AddStress(unit, 5, StressTag.Combat);
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 压力 +5");
                break;

            case "ziNue":
                StressManager.AddStress(unit, 5, StressTag.Combat);
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 压力 +5");
                break;

            case "yiShang":
                if (context is QuirkContext qc)
                {
                    qc.intValue = Mathf.RoundToInt(qc.intValue * 1.2f);
                    qc.valueModified = true;
                    unit.SetQuirkCooldown(quirk.id, 1);
                    BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 受到的伤害 +20%");
                }
                break;

            // ========== 新增触发类型示例 ==========

            case "baoJiShouHuo":  // 暴击回收：每次暴击回 2 压力
                StressManager.ReduceStress(unit, 2);
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 暴击回收压力 -2");
                break;

            case "shanBiHuiFu":   // 闪避恢复：闪避时恢复 5%HP
                int hpRecover = Mathf.RoundToInt(unit.MaxHp * 0.05f);
                int actual = Mathf.Min(hpRecover, unit.MaxHp - unit.currentHP);
                unit.currentHP += actual;
                unit.UpdateHPUI();
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 闪避恢复 {actual} HP");
                break;

            case "jieJuZhiLi":    // 借句之力：每回合结束压力 -1
                StressManager.ReduceStress(unit, 1);
                unit.SetQuirkCooldown(quirk.id, 1);
                BattleLog.Add($"[特质] {unit.unitName} 的 [{name}] 触发了！效果: 回合末减压 -1");
                break;

            default:
                Log.Warn($"[QuirkTriggerSystem] 未知特质 ID: {quirk.id}");
                break;
        }
    }
}
