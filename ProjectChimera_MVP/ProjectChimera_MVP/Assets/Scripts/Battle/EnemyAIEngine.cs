using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

public enum AIRole
{
    Default,
    Healer,
    Aggressor,
    Debuffer,
    Tank,
    Sniper
}

/// <summary>
/// 编辑器测试模式：当为 true 时，所有 yield return new WaitForSeconds(...) 替换为 yield return null，
/// 因为编辑器模式下 Time.time 永远为 0，WaitForSeconds 永远不会完成。
/// </summary>
public static class EnemyAIEngine
{
    /// <summary>由 BattleTestRunner 在编辑器测试开始前设为 true</summary>
    public static bool IsEditorTestMode = false;

    static Dictionary<UnitData, Dictionary<string, int>> skillUseCount = new Dictionary<UnitData, Dictionary<string, int>>();
    static Dictionary<UnitData, AIDifficultyProfile> profileCache = new Dictionary<UnitData, AIDifficultyProfile>();
    static Dictionary<UnitData, string> lastSkillUsed = new Dictionary<UnitData, string>();
    static Dictionary<string, float> playerThreatScores = new Dictionary<string, float>();

    public static void Reset(UnitData unit)
    {
        if (skillUseCount.ContainsKey(unit)) skillUseCount.Remove(unit);
        if (profileCache.ContainsKey(unit)) profileCache.Remove(unit);
        if (lastSkillUsed.ContainsKey(unit)) lastSkillUsed.Remove(unit);
        if (playerThreatScores.ContainsKey(unit.unitName)) playerThreatScores.Remove(unit.unitName);
    }

    public static void ResetAll()
    {
        skillUseCount.Clear();
        profileCache.Clear();
        lastSkillUsed.Clear();
        playerThreatScores.Clear();
    }

    public static void InitializeThreats(BattleManager bm)
    {
        playerThreatScores.Clear();
        foreach (var pu in bm.playerUnits)
        {
            if (pu == null) continue;
            float score = 0;
            score += pu.GetFinalStat(StatType.STR) * 1.5f;
            score += pu.GetFinalStat(StatType.INT) * 1.5f;
            score += pu.MaxHp * 0.1f;
            score += pu.GetFinalStat(StatType.SPD) * 0.5f;
            if (pu.skills.Exists(s => s.aiCategory == SkillEffectType.Heal))
                score += 20;
            if (pu.skills.Exists(s => s.aiCategory == SkillEffectType.Stress))
                score += 15;
            playerThreatScores[pu.unitName] = Mathf.Max(1f, score);
        }
    }

    public static void UpdateThreatAfterKill(UnitData killed)
    {
        if (playerThreatScores.ContainsKey(killed.unitName))
            playerThreatScores[killed.unitName] = 0f;
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
        // P1 防御：入口校验，确保战斗上下文合法且双方编队非空
        if (bm == null || bm.playerUnits == null || bm.enemyUnits == null ||
            bm.playerUnits.Count == 0 || bm.enemyUnits.Count == 0)
        {
            Log.Warn("[EnemyAIEngine] ExecuteTurn 入口校验失败，跳过 AI 回合");
            endTurn?.Invoke();
            yield break;
        }

        // P1 防御：确认当前单位有效
        if (unit == null || unit.currentHP <= 0)
        {
            Log.Warn("[EnemyAIEngine] ExecuteTurn 单位无效或已死亡，跳过回合");
            endTurn?.Invoke();
            yield break;
        }

        Log.Info($"[AI] {unit.unitName} 思考中...");

        var profile = GetProfile(unit);

        if (!skillUseCount.ContainsKey(unit))
            skillUseCount[unit] = new Dictionary<string, int>();

        yield return IsEditorTestMode ? null : new WaitForSeconds(0.8f);

        UnitData chosenTarget = null;
        SkillData chosenSkill = null;

        if (unit.breakdownState == BreakdownState.Affliction)
        {
            ExecuteAfflictedAction(unit, bm, ref chosenTarget, ref chosenSkill);
            if (chosenTarget != null)
            {
                yield return ExecuteAction(unit, bm, tm, chosenTarget, chosenSkill);
                TrackLastSkill(unit, chosenSkill);
                endTurn();
                yield break;
            }
        }

        StatusEffect taunt = unit.GetStatus(StatusType.Taunt);
        if (taunt != null)
        {
            UnitData tauntTarget = bm.playerUnits.Find(u => u != null && u.unitName == taunt.sourceName && u.currentHP > 0);
            if (tauntTarget != null)
            {
                BattleLog.Add($"[嘲讽] {unit.unitName} 被嘲讽，强制攻击 {tauntTarget.unitName}");
                SkillData basicSkill = GetBestDamageSkill(unit, profile);
                yield return ExecuteAction(unit, bm, tm, tauntTarget, basicSkill);
                TrackLastSkill(unit, basicSkill);
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
            TrackLastSkill(unit, chosenSkill);
        }
        else
        {
            BattleLog.Add($"[敌人] {unit.unitName} 没有可执行的动作");
            ClearLastSkill(unit);
        }

        yield return IsEditorTestMode ? null : new WaitForSeconds(0.3f);
        TickCooldowns(unit);
        endTurn();
    }

    static void ExecuteAfflictedAction(UnitData unit, BattleManager bm,
        ref UnitData target, ref SkillData skill)
    {
        float roll = RandomProvider.Current.Value;

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

        var allies = bm.enemyUnits.Where(u => u != null && u.currentHP > 0 && u != unit).ToList();
        var players = bm.playerUnits.Where(u => u != null && u.currentHP > 0).ToList();

        // Priority 1: Emergency heal
        if (role == AIRole.Healer && TryHeal(unit, allies, profile, out target, out skill))
            return;

        // Priority 2: Finish death's door targets
        if (profile.deathDoorFocus > RandomProvider.Current.Value &&
            TryFinishDeathsDoor(unit, players, bm, out target, out skill))
            return;

        // Priority 3: Emergency shield (protect low HP ally)
        if (role == AIRole.Healer && TryEmergencyShield(unit, allies, profile, out target, out skill))
            return;

        // Priority 4: Skill combo follow-up
        if (profile.comboChance > RandomProvider.Current.Value &&
            TryComboFollowUp(unit, players, profile, out target, out skill))
            return;

        // Priority 5: Apply stress (if player stress high)
        if (profile.tacticalDepth > RandomProvider.Current.Value * 0.5f &&
            TryStressAttack(unit, players, profile, out target, out skill))
            return;

        // Priority 6a: Aggressor - target marked/bleeding targets
        if (role == AIRole.Aggressor && TryAttackMarked(unit, players, out target, out skill))
            return;

        // Priority 6b: Debuff / mark
        if (profile.tacticalDepth > 0.4f &&
            TryDebuff(unit, players, out target, out skill))
            return;

        // Priority 6c: Threat focus - target highest threat
        if (profile.threatFocus > RandomProvider.Current.Value &&
            TryFocusThreat(unit, players, profile, bm, out target, out skill))
            return;

        // Priority 7: Buff self / allies
        if (role != AIRole.Aggressor && profile.tacticalDepth > 0.3f &&
            TryBuff(unit, allies, out target, out skill))
            return;

        // Priority 8: AOE (if multiple targets)
        if (profile.aoePreference > RandomProvider.Current.Value * 0.5f &&
            TryAoE(unit, players, bm, out target, out skill))
            return;

        // Priority 9: Single target damage (with position awareness)
        TryDamage(unit, players, profile, role, bm, out target, out skill);
    }

    static bool TryHeal(UnitData unit, List<UnitData> allies, AIDifficultyProfile profile,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        if (allies == null || allies.Count == 0) return false;

        var healSkill = FindUsableSkill(unit, SkillEffectType.Heal);
        if (healSkill == null) return false;

        var needHeal = allies
            .Where(a => a != null && (float)a.currentHP / a.MaxHp < profile.emergencyHealThreshold)
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

        if (allies == null || allies.Count == 0) return false;

        var shieldSkill = FindUsableSkill(unit, SkillEffectType.Shield);
        if (shieldSkill == null) return false;

        var lowHpAlly = allies
            .Where(a => a != null && (float)a.currentHP / a.MaxHp < 0.3f)
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
                target = unstunned[RandomProvider.Current.Range(0, unstunned.Count)];
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
            target = allies[RandomProvider.Current.Range(0, allies.Count)];
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

        if (RandomProvider.Current.Value < profile.skillUseChance)
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
                skill = usableSkills[RandomProvider.Current.Range(0, usableSkills.Count)];
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
            .ToList();

        if (damageSkills.Count == 0) return null;

        // Score-based selection
        float bestScore = float.MinValue;
        SkillData bestSkill = null;

        foreach (var s in damageSkills)
        {
            float score = CalculateSkillScore(unit, s, profile);
            if (score > bestScore)
            {
                bestScore = score;
                bestSkill = s;
            }
        }

        return bestSkill;
    }

    static float CalculateSkillScore(UnitData unit, SkillData skill, AIDifficultyProfile profile)
    {
        float score = skill.baseDamage;

        // AoE bonus per extra target
        if (skill.aiCategory == SkillEffectType.AoEDamage)
            score *= 1.5f;

        // Bleed bonus (damage over time)
        if (skill.aiCategory == SkillEffectType.Bleed)
            score += skill.effectValue * (skill.effectDuration > 0 ? skill.effectDuration : 2) * 0.5f;

        // Stun utility bonus
        if (skill.aiCategory == SkillEffectType.Stun)
            score += 10f;

        // Cooldown penalty (prefer skills available sooner next time)
        if (skill.cooldown > 0 && unit.skillCooldowns != null &&
            unit.skillCooldowns.ContainsKey(skill.skillName) &&
            unit.skillCooldowns[skill.skillName] > 0)
            score -= 30f;

        // Variety bonus (prefer skills used less)
        if (skillUseCount.ContainsKey(unit) && skillUseCount[unit].ContainsKey(skill.skillName))
            score -= skillUseCount[unit][skill.skillName] * 5f;

        // Tactical depth: high depth = more deterministic (prefer highest raw), low = more random
        if (profile.tacticalDepth < 0.5f)
            score += RandomProvider.Current.Range(0f, 15f);

        return score;
    }

    static void TrackLastSkill(UnitData unit, SkillData skill)
    {
        if (skill != null)
            lastSkillUsed[unit] = skill.skillName;
        else
            ClearLastSkill(unit);
    }

    static void ClearLastSkill(UnitData unit)
    {
        if (lastSkillUsed.ContainsKey(unit))
            lastSkillUsed.Remove(unit);
    }

    static bool WasLastSkill(UnitData unit, SkillEffectType category)
    {
        if (!lastSkillUsed.ContainsKey(unit)) return false;
        string last = lastSkillUsed[unit];
        var lastSkill = unit.skills.Find(s => s.skillName == last);
        return lastSkill != null && lastSkill.aiCategory == category;
    }

    static bool TryComboFollowUp(UnitData unit, List<UnitData> players, AIDifficultyProfile profile,
        out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        if (!lastSkillUsed.ContainsKey(unit)) return false;

        var lastSkill = unit.skills.Find(s => s.skillName == lastSkillUsed[unit]);
        if (lastSkill == null) return false;

        // Combo: Mark → direct damage on marked target
        if (lastSkill.aiCategory == SkillEffectType.Mark || lastSkill.aiCategory == SkillEffectType.Stun)
        {
            var markedTarget = players.FirstOrDefault(p =>
                p.HasStatus(StatusType.Mark) || p.HasStatus(StatusType.Stun));
            if (markedTarget != null)
            {
                skill = GetBestDamageSkill(unit, profile);
                if (skill != null)
                {
                    target = markedTarget;
                    BattleLog.Add($"[AI连招] {unit.unitName} 追击 {target.unitName} (连招衔接)");
                    return true;
                }
            }
        }

        // Combo: BerserkBuff → strongest attack on most threatening target
        if (lastSkill.aiCategory == SkillEffectType.BerserkBuff)
        {
            var highThreat = players
                .OrderByDescending(p => GetThreatScore(p))
                .FirstOrDefault();
            if (highThreat != null)
            {
                skill = GetBestDamageSkill(unit, profile);
                target = highThreat;
                BattleLog.Add($"[AI连招] {unit.unitName} 狂暴后猛攻 {target.unitName}");
                return true;
            }
        }

        return false;
    }

    static bool TryFocusThreat(UnitData unit, List<UnitData> players, AIDifficultyProfile profile,
        BattleManager bm, out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        if (playerThreatScores == null || playerThreatScores.Count == 0)
            InitializeThreats(bm);

        var sorted = players
            .OrderByDescending(p => GetThreatScore(p))
            .ToList();

        if (sorted.Count == 0) return false;

        target = sorted[0];
        skill = GetBestDamageSkill(unit, profile);
        if (skill != null)
            BattleLog.Add($"[AI威胁] {unit.unitName} 集中攻击高威胁目标 {target.unitName}");
        return true;
    }

    static float GetThreatScore(UnitData player)
    {
        if (player == null || player.currentHP <= 0) return 0f;
        if (playerThreatScores.ContainsKey(player.unitName))
            return playerThreatScores[player.unitName];
        return 1f;
    }

    static UnitData SelectBestTarget(UnitData unit, BattleManager bm, AIDifficultyProfile profile)
    {
        if (profile == null) profile = AIDifficultyProfile.GetDefault(AIDifficulty.Normal);

        // bm 由调用方（ExecuteTurn 等）保证非 null；之前 `bm ?? FindObjectOfType<BattleManager>()`
        // 是一道防御性兜底，但它会把 EnemyAIEngine 与 BattleManager 类硬绑定。
        // 现在 AI 完全走 BattleManager 引用：调用链是 TurnManager.EnemyTurn() →
        // EnemyAIEngine.ExecuteTurn(unit, bm, ...)，bm 在 TurnManager 那一侧就已经从
        // Context.RawContext（逃生通道）拿到，不可能为 null。直接断言即可。
        if (bm == null)
        {
            Log.Error("[EnemyAIEngine] SelectBestTarget: bm 为 null，调用方必须传入 BattleManager");
            return null;
        }

        var players = bm.playerUnits.Where(u => u != null && u.currentHP > 0).ToList();
        if (players.Count == 0) return null;

        // Death's door priority (always highest)
        if (profile.deathDoorFocus > RandomProvider.Current.Value)
        {
            var dd = players.Where(p => p.isOnDeathsDoor).ToList();
            if (dd.Count > 0) return dd[0];
        }

        // Low HP cleanup
        var lowHp = players.Where(p => (float)p.currentHP / p.MaxHp < 0.25f)
                           .OrderBy(p => p.currentHP).ToList();
        if (lowHp.Count > 0 && RandomProvider.Current.Value > profile.targetVariance)
            return lowHp[0];

        // Position-aware + threat-weighted scoring
        bool isBackline = unit.rank >= 2;
        float bestScore = float.MinValue;
        UnitData bestTarget = null;

        foreach (var p in players)
        {
            if (p == null) continue;
            float score = 0;

            // Position preference: backline targets backline, frontline targets frontline
            if (isBackline)
                score += p.rank >= 2 ? 15f * profile.positionalAwareness : 0f;
            else
                score += p.rank <= 1 ? 10f * profile.positionalAwareness : 0f;

            // Threat score
            score += GetThreatScore(p) * 0.5f * profile.threatFocus;

            // Low HP bonus (finishing bonus)
            float hpRatio = (float)p.currentHP / p.MaxHp;
            score += (1f - hpRatio) * 20f;

            // Marked/bleeding bonus
            if (p.HasStatus(StatusType.Mark)) score += 15f;
            if (p.HasStatus(StatusType.Bleed)) score += 10f;
            if (p.HasStatus(StatusType.Stun)) score += 12f;

            // Random variance
            score += RandomProvider.Current.Range(0f, 10f * profile.targetVariance);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = p;
            }
        }

        // 如果评分未命中任何目标，回退到第一个可用目标
        return bestTarget ?? players.FirstOrDefault();
    }

    static UnitData SelectStrategicTarget(List<UnitData> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;
        var backline = candidates.Where(c => c.rank >= 2).ToList();
        if (backline.Count > 0) return backline[RandomProvider.Current.Range(0, backline.Count)];
        return candidates[RandomProvider.Current.Range(0, candidates.Count)];
    }

    static UnitData SelectRandomTarget(UnitData unit, BattleManager bm, bool includeAlly)
    {
        var allTargets = new List<UnitData>();
        allTargets.AddRange(bm.playerUnits.Where(u => u != null && u.currentHP > 0));
        if (includeAlly)
            allTargets.AddRange(bm.enemyUnits.Where(u => u != null && u.currentHP > 0 && u != unit));
        return allTargets.Count > 0 ? allTargets[RandomProvider.Current.Range(0, allTargets.Count)] : null;
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
            // Track usage count for variety scoring
            if (!skillUseCount.ContainsKey(unit))
                skillUseCount[unit] = new Dictionary<string, int>();
            if (!skillUseCount[unit].ContainsKey(skill.skillName))
                skillUseCount[unit][skill.skillName] = 0;
            skillUseCount[unit][skill.skillName]++;

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

        yield return IsEditorTestMode ? null : new WaitForSeconds(0.3f);
    }
}
