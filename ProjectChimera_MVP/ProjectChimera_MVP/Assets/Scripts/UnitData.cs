using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitData : MonoBehaviour
{
    [Header("基本信息")]
    public string unitName;

    [Header("三层属性")]
    public PrimaryAttributes primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
    public int baseDefense = 5;
    public List<AttributeModifier> modifiers = new List<AttributeModifier>();

    public int VIT { get => primaryAttributes.VIT; set => primaryAttributes.VIT = value; }
    public int STR { get => primaryAttributes.STR; set => primaryAttributes.STR = value; }
    public int AGI { get => primaryAttributes.AGI; set => primaryAttributes.AGI = value; }
    public int INT { get => primaryAttributes.INT; set => primaryAttributes.INT = value; }
    public int STR_Effective => primaryAttributes.STR + GetModifierSum(AttributeTarget.STR);
    public int INT_Effective => primaryAttributes.INT + GetModifierSum(AttributeTarget.INT);
    public int DEF_Effective => baseDefense + GetModifierSum(AttributeTarget.DEF);
    public int MaxHp => primaryAttributes.VIT * 5 + GetClassBaseHP() + GetModifierSum(AttributeTarget.MaxHP);
    public int SPD => primaryAttributes.AGI * 2 + GetModifierSum(AttributeTarget.SPD);
    public int ACC => 80 + primaryAttributes.AGI * 2 + GetModifierSum(AttributeTarget.ACC);
    public int DOD => Mathf.RoundToInt(primaryAttributes.AGI * 1.5f) + GetModifierSum(AttributeTarget.DOD);

    int GetModifierSum(AttributeTarget target)
    {
        int sum = 0;
        foreach (var mod in modifiers)
            if (mod.type == ModifierType.Add && mod.target == target)
                sum += Mathf.RoundToInt(mod.value);
        return sum;
    }

    [Header("装备")]
    public int weaponAttack;

    [Header("身份标记")]
    public bool isPlayer;

    [Header("血量状态")]
    public int currentHP;
    public int shieldHP;

    [Header("压力系统")]
    public int stress;
    public const int MaxStressConst = 100;
    public float stressResistRate;
    public List<StressEvent> stressEventHistory = new List<StressEvent>();
    public int positiveQuirkCount;
    public BreakdownState breakdownState = BreakdownState.None;
    public string currentBreakdownId;
    public int breakdownDurationRemaining;
    public int breakdownCooldown;

    [Header("经验等级")]
    public int level = 1;
    public int currentExp;
    public int ExpToNextLevel => GetExpForLevel(level);

    [Header("血条引用")]
    public SpriteRenderer hpBarFill;
    public SpriteRenderer hpBarBg;
    public HPBarFollower hpBarFollower;

    [Header("选中光圈")]
    public GameObject selectCircle;

    [Header("技能")]
    public List<SkillData> skills = new List<SkillData>();

    [Header("状态效果")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    void Start()
    {
        if (selectCircle == null)
        {
            var found = transform.Find("SelectCircle");
            if (found != null)
                selectCircle = found.gameObject;
            Debug.Log($"[UnitData] {unitName} selectCircle 自动查找: {selectCircle != null}");
        }
    }

    public void UpdateHPUI()
    {
        if (hpBarFollower != null)
        {
            hpBarFollower.UpdateHP(currentHP, MaxHp);
            return;
        }

        if (hpBarFill != null)
        {
            float hpPercent = Mathf.Clamp01((float)currentHP / MaxHp);
            hpBarFill.transform.localScale = new Vector3(1.2f * hpPercent, 0.2f, 1);
            return;
        }
    }

    // ========== 状态效果管理 ==========

    public bool HasStatus(StatusType type)
    {
        return statusEffects.Exists(e => e.type == type && !e.expired);
    }

    public StatusEffect GetStatus(StatusType type)
    {
        return statusEffects.Find(e => e.type == type && !e.expired);
    }

    public void AddStatus(StatusEffect effect)
    {
        StatusEffect existing = statusEffects.Find(e => e.type == effect.type);
        if (existing != null)
        {
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, effect.remainingTurns);
            existing.sourceName = effect.sourceName;
            existing.value = effect.value;
        }
        else
        {
            statusEffects.Add(effect);
        }
        BattleLog.Add($"[状态] {unitName} 获得 [{effect.type}]");
    }

    public void RemoveStatus(StatusType type)
    {
        statusEffects.RemoveAll(e => e.type == type);
    }

    public void TickStatusEffects()
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            statusEffects[i].Tick();
            if (statusEffects[i].expired)
            {
                BattleLog.Add($"[状态] {unitName} 的 [{statusEffects[i].type}] 已消退");
                OnStatusExpired(statusEffects[i]);
                statusEffects.RemoveAt(i);
            }
        }
    }

    void OnStatusExpired(StatusEffect effect)
    {
        if (effect.type == StatusType.Berserk)
        {
            modifiers.RemoveAll(m => m.target == AttributeTarget.STR && m.source == "狂暴之力");
            modifiers.RemoveAll(m => m.target == AttributeTarget.DEF && m.source == "狂暴之力");
        }
        else if (effect.type == StatusType.Enlightened)
        {
            modifiers.RemoveAll(m => m.target == AttributeTarget.INT && m.source == "智慧启迪");
        }
    }

    /// <summary>获取职业基础HP</summary>
    public int GetClassBaseHP()
    {
        if (unitName.Contains("狂战士")) return 50;
        if (unitName.Contains("学者")) return 25;
        if (unitName.Contains("战士")) return 40;
        if (unitName.Contains("游侠")) return 30;
        return 40;
    }

    public bool ProcessBleed()
    {
        StatusEffect bleed = GetStatus(StatusType.Bleed);
        if (bleed != null)
        {
            int bleedDmg = Mathf.RoundToInt(bleed.value);
            currentHP -= bleedDmg;
            BattleLog.Add($"[流血] {unitName} 受到 {bleedDmg} 点流血伤害 (HP: {currentHP}/{MaxHp})");
            StressManager.AddStress(this, StressManager.config.onBleedTick, StressTag.Blood);
            UpdateHPUI();
            return currentHP > 0;
        }
        return true;
    }

    // ========== 高亮 ==========

    public enum HighlightState { Normal, Highlighted, Dimmed }

    public void SetHighlightState(HighlightState state)
    {
        Debug.Log($"[高亮] {unitName} → {state}, selectCircle={selectCircle != null}");

        if (selectCircle != null)
        {
            var sr = selectCircle.GetComponent<SpriteRenderer>();
            Debug.Log($"[高亮] {unitName} selectCircle SR={sr != null}");
            if (sr != null)
            {
                sr.enabled = (state == HighlightState.Highlighted);
                Debug.Log($"[高亮] {unitName} sr.enabled → {sr.enabled}");
            }
        }

        var skeleton = GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.skeleton != null)
        {
            float alpha = (state == HighlightState.Dimmed) ? 0.35f : 1f;
            skeleton.skeleton.a = alpha;
        }
    }

    public void SetHighlight(bool isSelected)
    {
        SetHighlightState(isSelected ? HighlightState.Highlighted : HighlightState.Normal);
    }

    // ========== 压力系统 ==========

    public void AddStress(int amount)
    {
        stress = Mathf.Min(stress + amount, MaxStressConst);
        BattleLog.Add($"[压力] {unitName} 压力 +{amount} ({stress}/{MaxStressConst})");
    }

    public void ReduceStress(int amount)
    {
        stress = Mathf.Max(stress - amount, 0);
        BattleLog.Add($"[压力] {unitName} 压力 -{amount} ({stress}/{MaxStressConst})");
    }

    public string GetStressBar()
    {
        int barLen = Mathf.RoundToInt((float)stress / MaxStressConst * 15);
        string bar = new string('\u2588', barLen) + new string('\u2591', 15 - barLen);
        string color = stress < 50 ? "#88ff88" : stress < 80 ? "#ffaa00" : "#ff4444";
        return $"<color={color}>{bar}</color> {stress}/{MaxStressConst}";
    }

    public bool IsBreakdownActive()
    {
        return breakdownState != BreakdownState.None;
    }

    public void ClearBreakdown()
    {
        breakdownState = BreakdownState.None;
        currentBreakdownId = null;
        breakdownDurationRemaining = 0;
    }

    public float GetDamageModifier()
    {
        if (breakdownState == BreakdownState.None) return 1f;
        BreakdownEntry entry = BreakdownDatabase.Get(currentBreakdownId);
        if (entry != null && entry.statModPercent != 0)
            return 1f + entry.statModPercent;
        return 1f;
    }

    public int GetEffectiveSTR()
    {
        float mod = StressManager.GetStatModifier(this);
        return Mathf.RoundToInt(STR_Effective * mod);
    }

    public int GetEffectiveDEF()
    {
        float mod = StressManager.GetStatModifier(this);
        return Mathf.RoundToInt(DEF_Effective * mod);
    }

    public int GetEffectiveINT()
    {
        float mod = StressManager.GetStatModifier(this);
        return Mathf.RoundToInt(INT_Effective * mod);
    }

    public float GetIncomingDamageMultiplier()
    {
        float multiplier = 1f;
        StatusEffect mark = GetStatus(StatusType.Mark);
        if (mark != null)
            multiplier += mark.value;
        return multiplier;
    }

    public int CalculateDamage(SkillData skill)
    {
        float raw = skill.baseDamage + weaponAttack + STR_Effective * skill.strScaling + AGI * skill.agiScaling;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public float GetStrengthCoefficient()
    {
        float baseCoeff = 1f + (STR_Effective - 5) * 0.05f;
        float breakdownMod = GetDamageModifier();
        return baseCoeff * breakdownMod;
    }

    // ========== 经验升级 ==========

    public static int GetExpForLevel(int lvl)
    {
        if (lvl <= 0) return 100;
        if (lvl > 5) return int.MaxValue;
        return 100 * (int)Mathf.Pow(2, lvl - 1);
    }

    public void GainExp(int amount)
    {
        if (level >= 5) return;
        currentExp += amount;
        BattleLog.Add($"[经验] {unitName} 获得 {amount} 经验 ({currentExp}/{ExpToNextLevel})");

        while (currentExp >= ExpToNextLevel && level < 5)
        {
            currentExp -= ExpToNextLevel;
            level++;

            primaryAttributes.VIT++;
            if (primaryAttributes.STR > primaryAttributes.AGI && primaryAttributes.STR > primaryAttributes.INT)
                primaryAttributes.STR++;
            else if (primaryAttributes.AGI > primaryAttributes.STR && primaryAttributes.AGI > primaryAttributes.INT)
                primaryAttributes.AGI++;
            else if (isPlayer && skills.Exists(s => s.agiScaling > 0.5f))
                primaryAttributes.AGI++;
            else
                primaryAttributes.STR++;

            currentHP = MaxHp;
            UpdateHPUI();

            BattleLog.Add($"🎉 {unitName} 升级到 {level} 级！HP 全恢复");
        }
    }
}
