using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitData : MonoBehaviour
{
    [Header("基本信息")]
    public string unitName;
    public ClassDefinition classData;

    [Header("三层属性")]
    public PrimaryAttributes primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
    public int baseDefense = 5;
    public List<AttributeModifier> modifiers = new List<AttributeModifier>();

    public int VIT { get => primaryAttributes.VIT; set => primaryAttributes.VIT = value; }
    public int STR { get => primaryAttributes.STR; set => primaryAttributes.STR = value; }
    public int AGI { get => primaryAttributes.AGI; set => primaryAttributes.AGI = value; }
    public int INT { get => primaryAttributes.INT; set => primaryAttributes.INT = value; }

    [Header("装备")]
    public int weaponAttack;
    public Weapon equippedWeapon;
    public Armor equippedArmor;

    [Header("特质")]
    public List<Quirk> quirks = new List<Quirk>();
    public Dictionary<string, int> quirkCooldowns = new Dictionary<string, int>();

    public bool HasQuirk(string id)
    {
        return quirks.Exists(q => q.id == id);
    }

    public bool IsQuirkOnCooldown(string id)
    {
        return quirkCooldowns.ContainsKey(id) && quirkCooldowns[id] > 0;
    }

    public void SetQuirkCooldown(string id, int turns)
    {
        quirkCooldowns[id] = turns;
    }

    public void TickQuirkCooldowns()
    {
        var keys = new List<string>(quirkCooldowns.Keys);
        foreach (var key in keys)
        {
            if (quirkCooldowns[key] > 0)
            {
                quirkCooldowns[key]--;
                if (quirkCooldowns[key] == 0)
                {
                    RemoveQuirkModifiers(key);
                    quirkCooldowns.Remove(key);
                }
            }
        }
    }

    public void RemoveQuirkModifiers(string quirkId)
    {
        string source = "quirk_" + quirkId;
        modifiers.RemoveAll(m => m.source == source);
    }

    // ========== GetFinalStat 管线 ==========

    static readonly Dictionary<StatType, AttributeTarget> statToTarget = new Dictionary<StatType, AttributeTarget>
    {
        { StatType.MaxHP, AttributeTarget.MaxHP },
        { StatType.SPD, AttributeTarget.SPD },
        { StatType.ACC, AttributeTarget.ACC },
        { StatType.DOD, AttributeTarget.DOD },
        { StatType.DEF, AttributeTarget.DEF },
        { StatType.STR, AttributeTarget.STR },
        { StatType.INT, AttributeTarget.INT },
        { StatType.CRT, AttributeTarget.CRT },
    };

    static AttributeTarget ToAttributeTarget(StatType stat)
    {
        statToTarget.TryGetValue(stat, out var t);
        return t;
    }

    int GetBuffFlat(StatType stat)
    {
        int sum = 0;
        foreach (var mod in modifiers)
            if (mod.type == ModifierType.Add && mod.target == ToAttributeTarget(stat))
                sum += Mathf.RoundToInt(mod.value);
        return sum;
    }

    float GetBuffPercent(StatType stat)
    {
        float sum = 0;
        foreach (var mod in modifiers)
            if (mod.type == ModifierType.Mul && mod.target == ToAttributeTarget(stat))
                sum += mod.value;
        return sum;
    }

    int GetBaseStat(StatType stat)
    {
        int classBase = classData != null ? GetClassBaseByType(stat) : 0;
        return classBase + GetPrimaryByType(stat);
    }

    int GetClassBaseByType(StatType stat)
    {
        if (classData == null) return 0;
        switch (stat)
        {
            case StatType.VIT: return classData.baseVIT;
            case StatType.STR: return classData.baseSTR;
            case StatType.AGI: return classData.baseAGI;
            case StatType.INT: return classData.baseINT;
            case StatType.DEF: return classData.baseDEF;
        }
        return 0;
    }

    int GetPrimaryByType(StatType stat)
    {
        switch (stat)
        {
            case StatType.VIT: return primaryAttributes.VIT;
            case StatType.STR: return primaryAttributes.STR;
            case StatType.AGI: return primaryAttributes.AGI;
            case StatType.INT: return primaryAttributes.INT;
        }
        return 0;
    }

    int GetEquipFlat(StatType stat)
    {
        int sum = 0;
        if (equippedWeapon != null) sum += equippedWeapon.GetFlatMod(stat);
        if (equippedArmor != null) sum += equippedArmor.GetFlatMod(stat);
        return sum;
    }

    float GetEquipPercent(StatType stat)
    {
        float sum = 0;
        if (equippedWeapon != null) sum += equippedWeapon.GetPercentMod(stat);
        if (equippedArmor != null) sum += equippedArmor.GetPercentMod(stat);
        return sum;
    }

    int GetQuirkFlat(StatType stat)
    {
        int sum = 0;
        foreach (var q in quirks)
            sum += q.GetFlatMod(stat);
        return sum;
    }

    float GetQuirkPercent(StatType stat)
    {
        float sum = 0;
        foreach (var q in quirks)
            sum += q.GetPercentMod(stat);
        return sum;
    }

    public int GetFinalStat(StatType stat)
    {
        if (stat == StatType.MaxHP)
        {
            int baseHp = primaryAttributes.VIT * 5 + (classData != null ? classData.baseHp : GetClassBaseHP());
            int equipFlat = GetEquipFlat(stat);
            int quirkFlat = GetQuirkFlat(stat);
            int buffFlat = GetBuffFlat(stat);
            int additive = baseHp + equipFlat + quirkFlat + buffFlat;

            float equipPct = GetEquipPercent(stat);
            float quirkPct = GetQuirkPercent(stat);
            float buffPct = GetBuffPercent(stat);
            float multiplier = 1f + equipPct + quirkPct + buffPct;

            return Mathf.RoundToInt(additive * Mathf.Max(0, multiplier));
        }

        int baseVal = GetBaseStat(stat);
        int equipFlat2 = GetEquipFlat(stat);
        int quirkFlat2 = GetQuirkFlat(stat);
        int buffFlat2 = GetBuffFlat(stat);
        int additive2 = baseVal + equipFlat2 + quirkFlat2 + buffFlat2;

        float equipPct2 = GetEquipPercent(stat);
        float quirkPct2 = GetQuirkPercent(stat);
        float buffPct2 = GetBuffPercent(stat);
        float multiplier2 = 1f + equipPct2 + quirkPct2 + buffPct2;

        float stressMod = 1f;
        if (stat == StatType.STR || stat == StatType.INT || stat == StatType.DEF)
            stressMod = StressManager.GetStatModifier(this);

        return Mathf.RoundToInt(additive2 * Mathf.Max(0, multiplier2) * stressMod);
    }

    // ========== 向后兼容的有效属性 ==========

    public int STR_Effective => GetFinalStat(StatType.STR);
    public int INT_Effective => GetFinalStat(StatType.INT);
    public int DEF_Effective => GetFinalStat(StatType.DEF);
    public int MaxHp => GetFinalStat(StatType.MaxHP);
    public int SPD => GetFinalStat(StatType.SPD);
    public int ACC => GetFinalStat(StatType.ACC);
    public int DOD => GetFinalStat(StatType.DOD);
    public int CRT => GetFinalStat(StatType.CRT);

    [Header("阵型")]
    public int rank; // 0=前排(近敌), 3=后排(远敌)

    [Header("身份标记")]
    public bool isPlayer;

    [Header("血量状态")]
    public int currentHP;
    public int shieldHP;

    [Header("死亡之门")]
    public bool isOnDeathsDoor;
    public float deathsDoorResist = 0.67f;

    [Header("压力系统")]
    public int stress;
    public const int MaxStressConst = 200;
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

    [Header("压力条引用")]
    public StressBarFollower stressBarFollower;

    [Header("选中光圈")]
    public GameObject selectCircle;

    [Header("技能")]
    public List<SkillData> skills = new List<SkillData>();
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    [Header("状态效果")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    void Start()
    {
        if (selectCircle == null)
        {
            var found = transform.Find("SelectCircle");
            if (found != null)
                selectCircle = found.gameObject;
        }
    }

    public void UpdateHPUI()
    {
        if (hpBarFollower != null)
        {
            hpBarFollower.UpdateHP(currentHP, MaxHp);
            if (isOnDeathsDoor && currentHP <= 0)
                hpBarFollower.SetDeathsDoor(true);
            else
                hpBarFollower.SetDeathsDoor(false);
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

    /// <summary>获取职业基础HP（不含VIT加成）</summary>
    public int GetClassBaseHP()
    {
        if (classData != null) return classData.baseHp;
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
        if (selectCircle != null)
        {
            var sr = selectCircle.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = (state == HighlightState.Highlighted);
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

    // ========== 便捷治疗 ==========

    public void HealHP(int amount)
    {
        int healed = Mathf.Min(amount, MaxHp - currentHP);
        currentHP += healed;
        BattleLog.Add($"[治疗] {unitName} 恢复 {healed} HP ({currentHP}/{MaxHp})");
        UpdateHPUI();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioKeys.SFX_HEAL);
        VFXManager.FlashHeal(this);
    }

    // ========== 压力系统 ==========

    public void AddStress(int amount)
    {
        stress = Mathf.Min(stress + amount, StressManager.config.maxStress);
        BattleLog.Add($"[压力] {unitName} 压力 +{amount} ({stress}/{StressManager.config.maxStress})");
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
        string color = stress < 50 ? "#88ff88" : stress < 100 ? "#ffaa00" : "#ff4444";
        return $"<color={color}>{bar}</color> {stress}/{MaxStressConst}";
    }

    public Color GetStressColor()
    {
        if (stress < 50) return Color.green;
        if (stress < 100) return Color.yellow;
        if (stress < 150) return new Color(1f, 0.55f, 0f);
        return Color.red;
    }

    public MentalState GetMentalState()
    {
        if (currentHP <= 0) return MentalState.HeartAttack;
        if (breakdownState == BreakdownState.HeartAttack) return MentalState.HeartAttack;
        if (breakdownState == BreakdownState.Affliction) return MentalState.Afflicted;
        if (breakdownState == BreakdownState.Virtue) return MentalState.Virtuous;
        return MentalState.Normal;
    }

    public bool IsStressImmune()
    {
        return breakdownState == BreakdownState.Virtue && StressManager.config.virtueStressImmunity;
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

    public int GetEffectiveSTR() => GetFinalStat(StatType.STR);
    public int GetEffectiveDEF() => GetFinalStat(StatType.DEF);
    public int GetEffectiveINT() => GetFinalStat(StatType.INT);

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
        float raw = skill.baseDamage + weaponAttack + GetFinalStat(StatType.STR) * skill.strScaling + AGI * skill.agiScaling;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public float GetStrengthCoefficient()
    {
        float baseCoeff = 1f + (GetEffectiveSTR() - 5) * 0.05f;
        float breakdownMod = GetDamageModifier();
        return baseCoeff * breakdownMod;
    }

    public bool IsSkillOnCooldown(SkillData skill)
    {
        return skillCooldowns != null &&
               skillCooldowns.ContainsKey(skill.skillName) &&
               skillCooldowns[skill.skillName] > 0;
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
            BattleLog.Add($"{unitName} 升级到 {level} 级！HP 全恢复");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_LEVEL_UP);
        }
    }
}
