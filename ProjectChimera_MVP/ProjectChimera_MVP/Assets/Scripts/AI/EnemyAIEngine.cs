using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AIRole
{
    Default,
    Healer,
    Aggressor,
    Debuffer,
    Tank,
    Sniper
}

public static class EnemyAIEngine
{
    static Dictionary<UnitData, Dictionary<string, int>> skillUseCount = new Dictionary<UnitData, Dictionary<string, int>>();
    static Dictionary<UnitData, AIDifficultyProfile> profileCache = new Dictionary<UnitData, AIDifficultyProfile>();

    public static void Reset(UnitData unit)
    {
        if (skillUseCount.ContainsKey(unit)) skillUseCount.Remove(unit);
        if (profileCache.ContainsKey(unit)) profileCache.Remove(unit);
    }

    public static void ResetAll()
    {
        skillUseCount.Clear();
        profileCache.Clear();
    }

    public static AIDifficultyProfile GetProfile(UnitData unit)
    {
        if (!profileCache.ContainsKey(unit))
        {
            AIDifficulty diff = AIDifficulty.Normal;
            if (unit.unitName.Contains("精英") || unit.unitName.Contains("Elite"))
                diff = AIDifficulty.Hard;
            if (unit.level >= 6)
                diff = AIDifficulty.Boss;
            profileCache[unit] = AIDifficultyProfile.GetDefault(diff);
        }
        return profileCache[unit];
    }

    public static IEnumerator ExecuteTurn(
        UnitData unit, BattleManager bm, TurnManager tm, System.Action endTurn)
    {
        Debug.Log($"[AI] {unit.unitName} 思考中...");

        var profile = GetProfile(unit);

        if (!skillUseCount.ContainsKey(unit))
            skillUseCount[unit] = new Dictionary<string, int>();

        yield return new WaitForSeconds(0.8f);

        UnitData chosenTarget = null;
        SkillData chosenSkill = null;

        if (unit.breakdownState == BreakdownState.Affliction)
        {
            ExecuteAfflictedAction(unit, bm, ref chosenTarget, ref chosenSkill);
            if (chosenTarget != null)
            {
                yield return ExecuteAction(unit, bm, tm, chosenTarget, chosenSkill);
                endTurn();
                yield break;
            }
        }

        StatusEffect taunt = unit.GetStatus(StatusType.Taunt);
        if (taunt != null)
        {
            UnitData tauntTarget = bm.playerUnits.Find(u => u.unitName == taunt.sourceName && u.currentHP > 0);
            if (tauntTarget != null)
            {
                BattleLog.Add($"[嘲讽] {unit.unitName} 被嘲讽，强制攻击 {tauntTarget.unitName}");
                SkillData basicSkill = GetBestDamageSkill(unit, profile);
                yield return ExecuteAction(unit, bm, tm, tauntTarget, basicSkill);
                endTurn();
                yield break;
            }
        }

        AIRole role = InferRole(unit);
        EvaluateActions(unit, bm, profile, role, out chosenTarget, out chosenSkill);

        if (chosenTarget != null)
        {
            if (chosenSkill != null && chosenSkill.targetType == SkillTargetType.SingleEnemy &&
                !tm.RankCanHit(unit, chosenTarget, chosenSkill))
            {
                chosenTarget = SelectBestTarget(unit, bm, profile);
            }

            yield return ExecuteAction(unit, bm, tm, chosenTarget, chosenSkill);
        }
        else
        {
            BattleLog.Add($"[敌人] {unit.unitName} 没有可执行的动作");
        }

        yield return new WaitForSeconds(0.3f);
        TickCooldowns(unit);
        endTurn();
    }

    static void ExecuteAfflictedAction(UnitData unit, BattleManager bm,
        ref UnitData target, ref SkillData skill)
    {
        float roll = Random.value;

        if (roll < 0.25f)
        {
            target = SelectRandomTarget(unit, bm, includeAlly: true);
            if (target != null)
                BattleLog.Add($"<color=red>[崩溃] {unit.unitName} 失控攻击了 {target.unitName}！</color>");
        }
        else if (roll < 0.45f)
        {
            BattleLog.Add($"<color=red>[崩溃] {unit.unitName} 陷入恐慌，跳过回合！</color>");
            target = unit;
            skill = null;
        }
        else if (roll < 0.60f)
        {
            BattleLog.Add($"<color=red>[崩溃] {unit.unitName} 自暴自弃，防御下降！</color>");
            unit.modifiers.Add(new AttributeModifier(
                ModifierType.Add, AttributeTarget.DEF, -3f, "affliction_self"));
            target = SelectBestTarget(unit, bm, AIDifficultyProfile.GetDefault(AIDifficulty.Normal));
        }
        else
        {
            BattleLog.Add($"<color=red>[崩溃] {unit.unitName} 陷入疯狂，全力攻击！</color>");
            target = SelectBestTarget(unit, bm, AIDifficultyProfile.GetDefault(AIDifficulty.Hard));
            skill = GetBestDamageSkill(unit, AIDifficultyProfile.GetDefault(AIDifficulty.Hard));
        }
    }

    static AIRole InferRole(UnitData unit)
    {
        if (unit.unitName.Contains("萨满") || unit.unitName.Contains("Shaman"))
            return AIRole.Healer;
        if (unit.unitName.Contains("狼") || unit.unitName.Contains("Wolf"))
            return AIRole.Aggressor;
        if (unit.unitName.Contains("弓手") || unit.unitName.Contains("Archer"))
            return AIRole.Sniper;
        if (unit.unitName.Contains("战士"))
            return AIRole.Tank;
        return AIRole.Default;
    }

    static void EvaluateActions(UnitData unit, BattleManager bm,
        AIDifficultyProfile profile, AIRole role,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var allies = bm.enemyUnits.Where(u => u.currentHP > 0 && u != unit).ToList();
        var players = bm.playerUnits.Where(u => u.currentHP > 0).ToList();

        // Priority 1: Emergency heal
        if (role == AIRole.Healer && TryHeal(unit, allies, profile, out target, out skill))
            return;

        // Priority 2: Finish death's door targets
        if (profile.deathDoorFocus > Random.value &&
            TryFinishDeathsDoor(unit, players, bm, out target, out skill))
            return;

        // Priority 3: Emergency shield (protect low HP ally)
        if (role == AIRole.Healer && TryEmergencyShield(unit, allies, profile, out target, out skill))
            return;

        // Priority 4: Apply stress (if player stress high)
        if (profile.tacticalDepth > Random.value * 0.5f &&
            TryStressAttack(unit, players, profile, out target, out skill))
            return;

        // Priority 5a: Aggressor - target marked/bleeding targets
        if (role == AIRole.Aggressor && TryAttackMarked(unit, players, out target, out skill))
            return;

        // Priority 5b: Debuff / mark
        if (profile.tacticalDepth > 0.4f &&
            TryDebuff(unit, players, out target, out skill))
            return;

        // Priority 6: Buff self / allies
        if (role != AIRole.Aggressor && profile.tacticalDepth > 0.3f &&
            TryBuff(unit, allies, out target, out skill))
            return;

        // Priority 7: AOE (if multiple targets)
        if (profile.aoePreference > Random.value * 0.5f &&
            TryAoE(unit, players, bm, out target, out skill))
            return;

        // Priority 8: Single target damage
        TryDamage(unit, players, profile, role, bm, out target, out skill);
    }

    static bool TryHeal(UnitData unit, List<UnitData> allies, AIDifficultyProfile profile,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var healSkill = FindUsableSkill(unit, SkillEffectType.Heal);
        if (healSkill == null) return false;

        var needHeal = allies
            .Where(a => (float)a.currentHP / a.MaxHp < profile.emergencyHealThreshold)
            .OrderBy(a => (float)a.currentHP / a.MaxHp)
            .ToList();

        if (needHeal.Count > 0)
        {
            skill = healSkill;
            target = needHeal[0];
            BattleLog.Add($"[AI治疗] {unit.unitName} 治疗 {target.unitName}");
            return true;
        }
        return false;
    }

    static bool TryFinishDeathsDoor(UnitData unit, List<UnitData> players, BattleManager bm,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var ddTargets = players.Where(p => p.isOnDeathsDoor || p.currentHP <= 0).ToList();
        if (ddTargets.Count == 0) return false;

        SkillData dmgSkill = GetBestDamageSkill(unit, AIDifficultyProfile.GetDefault(AIDifficulty.Normal));
        if (dmgSkill == null)
        {
            target = ddTargets[0];
            return true;
        }

        target = ddTargets[0];
        skill = dmgSkill;
        BattleLog.Add($"[AI斩杀] {unit.unitName} 试图击杀濒死的 {target.unitName}");
        return true;
    }

    static bool TryEmergencyShield(UnitData unit, List<UnitData> allies, AIDifficultyProfile profile,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var shieldSkill = FindUsableSkill(unit, SkillEffectType.Shield);
        if (shieldSkill == null) return false;

        var lowHpAlly = allies
            .Where(a => (float)a.currentHP / a.MaxHp < 0.3f)
            .OrderBy(a => (float)a.currentHP / a.MaxHp)
            .FirstOrDefault();

        if (lowHpAlly != null)
        {
            skill = shieldSkill;
            target = lowHpAlly;
            BattleLog.Add($"[AI护盾] {unit.unitName} 给 {target.unitName} 附加护盾");
            return true;
        }
        return false;
    }

    static bool TryStressAttack(UnitData unit, List<UnitData> players, AIDifficultyProfile profile,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var stressSkill = FindUsableSkill(unit, SkillEffectType.Stress);
        if (stressSkill == null) return false;

        var highStress = players
            .Where(p => p.stress >= profile.stressTargetThreshold)
            .OrderByDescending(p => p.stress)
            .ToList();

        if (highStress.Count > 0)
        {
            skill = stressSkill;
            target = highStress[0];
            BattleLog.Add($"[AI压力] {unit.unitName} 用压力攻击 {target.unitName}");
            return true;
        }
        return false;
    }

    static bool TryAttackMarked(UnitData unit, List<UnitData> players,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var markedTarget = players.FirstOrDefault(p =>
            p.HasStatus(StatusType.Mark) || p.HasStatus(StatusType.Bleed));
        if (markedTarget == null) return false;

        skill = GetBestDamageSkill(unit, AIDifficultyProfile.GetDefault(AIDifficulty.Hard));
        target = markedTarget;
        BattleLog.Add($"[AI追击] {unit.unitName} 追击受创的 {target.unitName}");
        return true;
    }

    static bool TryDebuff(UnitData unit, List<UnitData> players,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var markSkill = FindUsableSkill(unit, SkillEffectType.Mark);
        if (markSkill != null)
        {
            var unmarked = players.Where(p => !p.HasStatus(StatusType.Mark)).ToList();
            if (unmarked.Count > 0)
            {
                skill = markSkill;
                target = SelectStrategicTarget(unmarked);
                BattleLog.Add($"[AI标记] {unit.unitName} 标记了 {target.unitName}");
                return true;
            }
        }

        var stunSkill = FindUsableSkill(unit, SkillEffectType.Stun);
        if (stunSkill != null)
        {
            var unstunned = players.Where(p => !p.HasStatus(StatusType.Stun) && p.rank <= 1).ToList();
            if (unstunned.Count > 0)
            {
                skill = stunSkill;
                target = unstunned[Random.Range(0, unstunned.Count)];
                BattleLog.Add($"[AI眩晕] {unit.unitName} 试图眩晕 {target.unitName}");
                return true;
            }
        }

        return false;
    }

    static bool TryBuff(UnitData unit, List<UnitData> allies,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        if (!unit.HasStatus(StatusType.Berserk))
        {
            var buffSkill = FindUsableSkill(unit, SkillEffectType.BerserkBuff);
            if (buffSkill != null)
            {
                skill = buffSkill;
                target = unit;
                BattleLog.Add($"[AI增益] {unit.unitName} 对自己施放增益");
                return true;
            }
        }

        var protectSkill = FindUsableSkill(unit, SkillEffectType.Protect);
        if (protectSkill != null && allies.Count > 0)
        {
            skill = protectSkill;
            target = allies[Random.Range(0, allies.Count)];
            BattleLog.Add($"[AI保护] {unit.unitName} 保护 {target.unitName}");
            return true;
        }

        return false;
    }

    static bool TryAoE(UnitData unit, List<UnitData> players, BattleManager bm,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        var aoeSkill = FindUsableSkill(unit, SkillEffectType.AoEDamage);
        if (aoeSkill != null && players.Count >= 2)
        {
            skill = aoeSkill;
            target = players[0];
            BattleLog.Add($"[AI群攻] {unit.unitName} 使用群体攻击");
            return true;
        }

        return false;
    }

    static void TryDamage(UnitData unit, List<UnitData> players, AIDifficultyProfile profile,
        AIRole role, BattleManager bm, out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        if (Random.value < profile.skillUseChance)
        {
            skill = GetBestDamageSkill(unit, profile);
        }

        target = SelectBestTarget(unit, bm, profile);
        if (target == null) return;

        if (skill == null && unit.skills.Count > 0)
        {
            var usableSkills = unit.skills.FindAll(s =>
                s.aiCategory == SkillEffectType.DirectDamage &&
                SkillUsable(unit, s) && !IsSkillOnCooldown(unit, s));
            if (usableSkills.Count > 0)
                skill = usableSkills[Random.Range(0, usableSkills.Count)];
        }

        if (skill != null)
        {
            BattleLog.Add($"[AI攻击] {unit.unitName} 使用 [{skill.skillName}] -> {target.unitName}");
        }
    }

    static SkillData FindUsableSkill(UnitData unit, SkillEffectType category)
    {
        return unit.skills.Find(s =>
            s.aiCategory == category &&
            SkillUsable(unit, s) &&
            !IsSkillOnCooldown(unit, s));
    }

    static SkillData GetBestDamageSkill(UnitData unit, AIDifficultyProfile profile)
    {
        var damageSkills = unit.skills
            .Where(s => SkillUsable(unit, s) && !IsSkillOnCooldown(unit, s) &&
                       (s.aiCategory == SkillEffectType.DirectDamage ||
                        s.aiCategory == SkillEffectType.AoEDamage ||
                        s.aiCategory == SkillEffectType.Bleed ||
                        s.aiCategory == SkillEffectType.Stun))
            .OrderByDescending(s => s.baseDamage)
            .ToList();

        if (damageSkills.Count == 0) return null;

        if (profile.tacticalDepth > 0.6f && damageSkills.Count > 1)
        {
            return damageSkills[0];
        }

        return damageSkills[Random.Range(0, damageSkills.Count)];
    }

    static UnitData SelectBestTarget(UnitData unit, BattleManager bm, AIDifficultyProfile profile)
    {
        if (profile == null) profile = AIDifficultyProfile.GetDefault(AIDifficulty.Normal);

        var bmInstance = bm ?? GameObject.FindObjectOfType<BattleManager>();
        if (bmInstance == null) return null;

        var players = bmInstance.playerUnits.Where(u => u.currentHP > 0).ToList();
        if (players.Count == 0) return null;

        if (profile.deathDoorFocus > Random.value)
        {
            var dd = players.Where(p => p.isOnDeathsDoor).ToList();
            if (dd.Count > 0) return dd[0];
        }

        var lowHp = players.Where(p => (float)p.currentHP / p.MaxHp < 0.25f)
                           .OrderBy(p => p.currentHP).ToList();
        if (lowHp.Count > 0 && Random.value > profile.targetVariance)
            return lowHp[0];

        var front = players.Where(p => p.rank <= 1).ToList();
        if (front.Count > 0) return front[Random.Range(0, front.Count)];

        return players[Random.Range(0, players.Count)];
    }

    static UnitData SelectStrategicTarget(List<UnitData> candidates)
    {
        var backline = candidates.Where(c => c.rank >= 2).ToList();
        if (backline.Count > 0) return backline[Random.Range(0, backline.Count)];
        return candidates[Random.Range(0, candidates.Count)];
    }

    static UnitData SelectRandomTarget(UnitData unit, BattleManager bm, bool includeAlly)
    {
        var allTargets = new List<UnitData>();
        allTargets.AddRange(bm.playerUnits.Where(u => u.currentHP > 0));
        if (includeAlly)
            allTargets.AddRange(bm.enemyUnits.Where(u => u.currentHP > 0 && u != unit));
        return allTargets.Count > 0 ? allTargets[Random.Range(0, allTargets.Count)] : null;
    }

    static bool SkillUsable(UnitData unit, SkillData skill)
    {
        return unit.rank >= skill.minUserRank && unit.rank <= skill.maxUserRank;
    }

    static bool IsSkillOnCooldown(UnitData unit, SkillData skill)
    {
        return unit.skillCooldowns != null &&
               unit.skillCooldowns.ContainsKey(skill.skillName) &&
               unit.skillCooldowns[skill.skillName] > 0;
    }

    static void TickCooldowns(UnitData unit)
    {
        if (unit.skillCooldowns == null) return;
        var keys = new List<string>(unit.skillCooldowns.Keys);
        foreach (var key in keys)
        {
            if (unit.skillCooldowns[key] > 0)
                unit.skillCooldowns[key]--;
        }
    }

    static IEnumerator ExecuteAction(UnitData unit, BattleManager bm,
        TurnManager tm, UnitData target, SkillData skill)
    {
        if (unit == null || target == null) yield break;

        if (skill == null)
        {
            int rawDmg = unit.GetEffectiveSTR() + unit.weaponAttack;
            float strCoeff = unit.GetStrengthCoefficient();
            int damage = Mathf.Max(1, Mathf.RoundToInt(rawDmg * strCoeff - target.GetEffectiveDEF()));
            bm.DealDamage(unit, target, damage);
        }
        else
        {
            if (skill.cooldown > 0)
            {
                if (unit.skillCooldowns == null)
                    unit.skillCooldowns = new Dictionary<string, int>();
                unit.skillCooldowns[skill.skillName] = skill.cooldown;
            }

            if (skill.targetType == SkillTargetType.Self)
            {
                bm.ExecuteSkill(unit, skill, unit);
            }
            else if (skill.targetType == SkillTargetType.SingleAlly)
            {
                bm.ExecuteSkill(unit, skill, target);
            }
            else
            {
                bm.ExecuteSkill(unit, skill, target);
            }
        }

        yield return new WaitForSeconds(0.3f);
    }
}
