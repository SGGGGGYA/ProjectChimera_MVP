using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    DealDamage,
    ApplyStatus,
    RemoveStatus,
    Heal,
    Shield,
    MoveTarget,
    MoveSelf,
    ModifyAttribute,
    Summon,
    ConsumeResource
}

public class CommandContext
{
    public UnitData attacker;
    public UnitData selectedTarget;
    public BattleManager battleManager;
    public List<UnitData> allPlayers;
    public List<UnitData> allEnemies;
}

public class CommandResult
{
    public bool skipRemainingCommands;
    public List<string> logEntries = new List<string>();
}

[System.Serializable]
public abstract class Command
{
    public abstract CommandResult Execute(CommandContext ctx);
}

[System.Serializable]
public class DealDamageCommand : Command
{
    public int baseDamage;
    public float strScaling;
    public float agiScaling;
    public bool isAoE;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (isAoE)
        {
            var aliveEnemies = ctx.allEnemies.FindAll(e => e.currentHP > 0);
            foreach (var enemy in aliveEnemies)
            {
                bool hit = CombatSystem.IsHit(ctx.attacker, enemy);
                if (!hit)
                {
                    ctx.battleManager.SpawnDamagePopup(enemy.transform.position + Vector3.up * 1.5f, 0, PopupType.Miss);
                    BattleLog.Add($"{ctx.attacker.unitName} 攻击 {enemy.unitName} —— <color=#aaaaaa>未命中！</color>");
                    continue;
                }
                bool crit = CombatSystem.IsCrit(ctx.attacker, enemy);
                int dmg = CombatSystem.CalculateDamage(ctx.attacker, enemy, BuildSkillData());
                if (crit)
                {
                    dmg = Mathf.RoundToInt(dmg * 1.5f);
                    BattleLog.Add($"<color=#ffdd00>暴击！</color> {ctx.attacker.unitName} 对 {enemy.unitName} 造成致命一击！");
                }
                ctx.battleManager.DealDamage(ctx.attacker, enemy, dmg);
            }
        }
        else
        {
            if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
            {
                result.skipRemainingCommands = true;
                return result;
            }

            if (!CombatSystem.IsHit(ctx.attacker, ctx.selectedTarget))
            {
                ctx.battleManager.SpawnDamagePopup(ctx.selectedTarget.transform.position + Vector3.up * 1.5f, 0, PopupType.Miss);
                BattleLog.Add($"{ctx.attacker.unitName} 攻击 {ctx.selectedTarget.unitName} —— <color=#aaaaaa>未命中！</color>");
                result.skipRemainingCommands = true;
                return result;
            }

            bool isCrit = CombatSystem.IsCrit(ctx.attacker, ctx.selectedTarget);
            int dmg = CombatSystem.CalculateDamage(ctx.attacker, ctx.selectedTarget, BuildSkillData());
            if (isCrit)
            {
                dmg = Mathf.RoundToInt(dmg * 1.5f);
                BattleLog.Add($"<color=#ffdd00>暴击！</color> {ctx.attacker.unitName} 对 {ctx.selectedTarget.unitName} 造成致命一击！");
            }
            ctx.battleManager.DealDamage(ctx.attacker, ctx.selectedTarget, dmg);
            if (ctx.selectedTarget.currentHP <= 0)
                result.skipRemainingCommands = true;
        }

        return result;
    }

    SkillData BuildSkillData()
    {
        return new SkillData
        {
            baseDamage = this.baseDamage,
            strScaling = this.strScaling,
            agiScaling = this.agiScaling
        };
    }
}

[System.Serializable]
public class ApplyStatusCommand : Command
{
    public StatusType statusType;
    public int duration;
    public float effectValue;
    public bool applyToAllAllies;
    public bool applyToAllEnemies;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (applyToAllAllies)
        {
            var allies = ctx.attacker.isPlayer ? ctx.allPlayers : ctx.allEnemies;
            foreach (var ally in allies)
            {
                if (ally.currentHP > 0)
                    ally.AddStatus(new StatusEffect(statusType, ctx.attacker.unitName, duration, effectValue));
            }
        }
        else if (applyToAllEnemies)
        {
            var enemies = ctx.attacker.isPlayer ? ctx.allEnemies : ctx.allPlayers;
            foreach (var enemy in enemies)
            {
                if (enemy.currentHP > 0)
                    enemy.AddStatus(new StatusEffect(statusType, ctx.attacker.unitName, duration, effectValue));
            }
        }
        else
        {
            if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
            {
                result.skipRemainingCommands = true;
                return result;
            }
            ctx.selectedTarget.AddStatus(new StatusEffect(statusType, ctx.attacker.unitName, duration, effectValue));
        }

        return result;
    }
}

[System.Serializable]
public class RemoveStatusCommand : Command
{
    public StatusType statusType;
    public bool removeFromAllAllies;
    public bool removeFromAllEnemies;

    public override CommandResult Execute(CommandContext ctx)
    {
        if (removeFromAllAllies)
        {
            var allies = ctx.attacker.isPlayer ? ctx.allPlayers : ctx.allEnemies;
            foreach (var ally in allies)
                ally.RemoveStatus(statusType);
        }
        else if (removeFromAllEnemies)
        {
            var enemies = ctx.attacker.isPlayer ? ctx.allEnemies : ctx.allPlayers;
            foreach (var enemy in enemies)
                enemy.RemoveStatus(statusType);
        }
        else
        {
            if (ctx.selectedTarget != null)
                ctx.selectedTarget.RemoveStatus(statusType);
        }

        return new CommandResult();
    }
}

[System.Serializable]
public class HealCommand : Command
{
    public int baseHeal;
    public float intScaling;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        int rawHeal = baseHeal + Mathf.RoundToInt(ctx.attacker.GetEffectiveINT() * intScaling);
        int healAmount = Mathf.Min(rawHeal, ctx.selectedTarget.MaxHp - ctx.selectedTarget.currentHP);
        ctx.selectedTarget.currentHP += healAmount;
        ctx.selectedTarget.UpdateHPUI();
        ctx.battleManager.SpawnDamagePopup(ctx.selectedTarget.transform.position + Vector3.up * 1.5f, healAmount, PopupType.Heal);
        BattleLog.Add($"[治疗] {ctx.attacker.unitName} 为 {ctx.selectedTarget.unitName} 恢复了 {healAmount} 点生命");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioKeys.SFX_HEAL);
        VFXManager.FlashHeal(ctx.selectedTarget);
        // 触发 OnHeal 特质
        QuirkTriggerSystem.CheckTriggers(ctx.selectedTarget, QuirkTriggerType.OnHeal);

        return result;
    }
}

[System.Serializable]
public class ShieldCommand : Command
{
    public int shieldAmount;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        ctx.selectedTarget.shieldHP += shieldAmount;
        ctx.battleManager.SpawnDamagePopup(ctx.selectedTarget.transform.position + Vector3.up * 1.5f, shieldAmount, PopupType.Shield);
        BattleLog.Add($"[护盾] {ctx.attacker.unitName} 为 {ctx.selectedTarget.unitName} 附加了 {shieldAmount} 点护盾");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioKeys.SFX_SHIELD);

        return result;
    }
}

[System.Serializable]
public class ConsumeResourceCommand : Command
{
    public enum ResourceType { Stress }

    public ResourceType resourceType;
    public int amount;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        if (resourceType == ResourceType.Stress)
        {
            if (amount >= 0)
            {
                ctx.battleManager.SpawnDamagePopup(ctx.selectedTarget.transform.position + Vector3.up * 1.5f, amount, PopupType.Stress);
                StressManager.AddStress(ctx.selectedTarget, amount, StressTag.Combat);
                StressManager.CheckResolve(ctx.selectedTarget, ctx.battleManager);
            }
            else
            {
                ctx.battleManager.SpawnTextPopup(ctx.selectedTarget.transform.position + Vector3.up * 1.5f, $"-{-amount}", new Color(0.6f, 0.2f, 0.8f), 3f);
                StressManager.ReduceStress(ctx.selectedTarget, -amount);
            }
        }

        return result;
    }
}

[System.Serializable]
public class ModifyAttributeCommand : Command
{
    public AttributeModifier modifier;
    public float duration; // 0 = permanent

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();

        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        ctx.selectedTarget.modifiers.Add(modifier);
        string sign = modifier.type == ModifierType.Add ? "+" : "x";
        BattleLog.Add($"[属性修改] {ctx.selectedTarget.unitName} {modifier.target} {sign}{modifier.value}");

        return result;
    }
}

/// <summary>嗜血斩击：伤害+自损10%最大HP+吸血50%自损量</summary>
[System.Serializable]
public class BloodthirstyStrikeCommand : Command
{
    public int baseDamage;
    public float strScaling;
    public float selfDamagePercent = 0.1f;
    public float lifeStealPercent = 0.5f;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();
        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        UnitData attacker = ctx.attacker;
        UnitData target = ctx.selectedTarget;

        float raw = baseDamage + attacker.weaponAttack + attacker.GetEffectiveSTR() * strScaling;
        float strCoeff = 1f + (attacker.GetEffectiveSTR() - 5) * 0.05f;
        int rawDmg = Mathf.RoundToInt(raw);
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(rawDmg * strCoeff - target.GetEffectiveDEF()));
        float markMult = target.GetIncomingDamageMultiplier();
        finalDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg * markMult));

        ctx.battleManager.DealDamage(attacker, target, finalDmg);

        int selfDmg = Mathf.Max(1, Mathf.RoundToInt(attacker.MaxHp * selfDamagePercent));
        int actualSelfDmg = Mathf.Min(selfDmg, attacker.currentHP - 1);
        attacker.currentHP -= actualSelfDmg;
        attacker.UpdateHPUI();
        ctx.battleManager.SpawnDamagePopup(attacker.transform.position + Vector3.up * 1.5f, actualSelfDmg, PopupType.Damage);
        BattleLog.Add($"[自损] {attacker.unitName} 自损 {actualSelfDmg} 点生命");

        int healAmount = Mathf.RoundToInt(actualSelfDmg * lifeStealPercent);
        int actualHeal = Mathf.Min(healAmount, attacker.MaxHp - attacker.currentHP);
        attacker.currentHP += actualHeal;
        attacker.UpdateHPUI();
        if (actualHeal > 0)
            ctx.battleManager.SpawnDamagePopup(attacker.transform.position + Vector3.up * 1.5f, actualHeal, PopupType.Heal);
        BattleLog.Add($"[吸血] {attacker.unitName} 恢复了 {actualHeal} 点生命");

        return result;
    }
}

/// <summary>殊死一搏：血量越低伤害越高</summary>
[System.Serializable]
public class DesperateStrikeCommand : Command
{
    public int baseDamage;
    public float strScaling;
    public float maxBonusMultiplier = 1.0f;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();
        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        UnitData attacker = ctx.attacker;
        UnitData target = ctx.selectedTarget;

        float hpMissing = 1f - (float)attacker.currentHP / attacker.MaxHp;
        float multiplier = 1f + hpMissing * maxBonusMultiplier;

        float raw = baseDamage + attacker.weaponAttack + attacker.GetEffectiveSTR() * strScaling;
        float strCoeff = 1f + (attacker.GetEffectiveSTR() - 5) * 0.05f;
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(raw * strCoeff * multiplier - target.GetEffectiveDEF()));
        float markMult = target.GetIncomingDamageMultiplier();
        finalDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg * markMult));

        BattleLog.Add($"[殊死一搏] 已损失{Mathf.RoundToInt(hpMissing * 100)}%HP，倍率{multiplier:F2}");
        ctx.battleManager.DealDamage(attacker, target, finalDmg);

        return result;
    }
}

/// <summary>精神冲击：基于INT的伤害+施加压力</summary>
[System.Serializable]
public class MindShockCommand : Command
{
    public int baseDamage;
    public float intScaling;
    public int stressAmount;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();
        if (ctx.selectedTarget == null || ctx.selectedTarget.currentHP <= 0)
        {
            result.skipRemainingCommands = true;
            return result;
        }

        UnitData attacker = ctx.attacker;
        UnitData target = ctx.selectedTarget;

        float raw = baseDamage + attacker.weaponAttack + attacker.GetEffectiveINT() * intScaling;
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(raw - target.GetEffectiveDEF()));
        float markMult = target.GetIncomingDamageMultiplier();
        finalDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg * markMult));

        ctx.battleManager.DealDamage(attacker, target, finalDmg);

        if (target.currentHP > 0 && stressAmount != 0)
        {
            if (stressAmount > 0)
            {
                ctx.battleManager.SpawnDamagePopup(target.transform.position + Vector3.up * 1.5f, stressAmount, PopupType.Stress);
                StressManager.AddStress(target, stressAmount, StressTag.Combat);
                StressManager.CheckResolve(target, ctx.battleManager);
            }
            else
                StressManager.ReduceStress(target, -stressAmount);
        }

        return result;
    }
}

/// <summary>自损命令：对自己造成伤害或压力</summary>
[System.Serializable]
public class SelfInflictCommand : Command
{
    public enum SelfInflictType { HP, Stress }

    public SelfInflictType inflictType;
    public int amount;

    public override CommandResult Execute(CommandContext ctx)
    {
        var result = new CommandResult();
        if (ctx.attacker == null) return result;

        if (inflictType == SelfInflictType.HP)
        {
            int dmg = Mathf.Min(amount, ctx.attacker.currentHP - 1);
            ctx.attacker.currentHP -= dmg;
            ctx.attacker.UpdateHPUI();
            ctx.battleManager.SpawnDamagePopup(ctx.attacker.transform.position + Vector3.up * 1.5f, dmg, PopupType.Damage);
            BattleLog.Add($"[自损] {ctx.attacker.unitName} 损失 {dmg} 点生命");
        }
        else if (inflictType == SelfInflictType.Stress)
        {
            if (amount >= 0)
            {
                ctx.battleManager.SpawnDamagePopup(ctx.attacker.transform.position + Vector3.up * 1.5f, amount, PopupType.Stress);
                StressManager.AddStress(ctx.attacker, amount, StressTag.SelfHarm);
                StressManager.CheckResolve(ctx.attacker, ctx.battleManager);
            }
            else
            {
                ctx.battleManager.SpawnTextPopup(ctx.attacker.transform.position + Vector3.up * 1.5f, $"-{-amount}", new Color(0.6f, 0.2f, 0.8f), 3f);
                StressManager.ReduceStress(ctx.attacker, -amount);
            }
        }

        return result;
    }
}
