using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
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

    /// <summary>每回合结束调用：递减所有带 duration 的 modifier，到期则移除</summary>
    public void TickModifiers()
    {
        if (modifiers == null || modifiers.Count == 0) return;
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            var m = modifiers[i];
            if (m.duration <= 0) continue; // 永久 buff 跳过
            m.remainingTurns--;
            modifiers[i] = m;
            if (m.remainingTurns <= 0)
            {
                BattleLog.Add($"[属性] {unitName} 的 [{m.target} {m.value}] 持续时间到，已移除");
                modifiers.RemoveAt(i);
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
        // 单一来源：用 DerivedAttributes 统一计算所有派生属性。
        // 之前 SPD/ACC/DOD/CRT 直接走 GetClassBaseByType → 全部 0（致命 Bug）。
        // 现在 classData=null 时 AGI 仍然按 0 算，CRUSHER 不会"白送"基础值。
        int baseHp = classData != null ? classData.baseHp : 0;
        int baseSpd = classData != null ? classData.baseSPD : 0;
        int baseAcc = classData != null ? classData.baseACC : 0;
        int baseDod = classData != null ? classData.baseDOD : 0;
        int baseCrt = classData != null ? classData.baseCRT : 0;

        var derived = new DerivedAttributes(
            primaryAttributes,
            baseDefense,
            baseHp,
            baseSpd,
            baseAcc,
            baseDod,
            baseCrt);

        switch (stat)
        {
            case StatType.VIT: return primaryAttributes.VIT;
            case StatType.STR: return GetClassBaseByType(stat) + primaryAttributes.STR;
            case StatType.AGI: return primaryAttributes.AGI;
            case StatType.INT: return GetClassBaseByType(stat) + primaryAttributes.INT;
            case StatType.DEF: return GetClassBaseByType(stat);
            case StatType.SPD: return derived.SPD;
            case StatType.ACC: return derived.ACC;
            case StatType.DOD: return derived.DOD;
            case StatType.CRT: return derived.CRT;
        }
        return 0;
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
            case StatType.SPD: return classData.baseSPD;
            case StatType.ACC: return classData.baseACC;
            case StatType.DOD: return classData.baseDOD;
            case StatType.CRT: return classData.baseCRT;
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
        if (namingQuirk != null) sum += namingQuirk.GetFlatMod(stat);
        return sum;
    }

    float GetQuirkPercent(StatType stat)
    {
        float sum = 0;
        foreach (var q in quirks)
            sum += q.GetPercentMod(stat);
        if (namingQuirk != null) sum += namingQuirk.GetPercentMod(stat);
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
    /// <summary>未分配属性点（V0.5 引入：升级不再自动加主属性，改为累加点待玩家分配）</summary>
    public int unassignedPoints;
    public int ExpToNextLevel => GetExpForLevel(level);

    // ========== 命名系统 ==========

    [Header("命名系统")]
    /// <summary>是否已命名（炮灰→英雄的质变节点，一次性触发）</summary>
    public bool isNamed;
    /// <summary>命名后压力上限额外加值（默认 0，命名后 =20）</summary>
    public int stressCapBonus;
    /// <summary>命名解锁的专属技能（命名后由玩家在英雄厅装备）</summary>
    public SkillData exclusiveSkill;
    /// <summary>命名怪癖（独立于 quirks 列表，不占 5 个怪癖位）</summary>
    public Quirk namingQuirk;

    /// <summary>V0.5: 等级上限 10（设计文档 2.3 节；领袖 15 待领袖系统建立后扩展）</summary>
    public const int MAX_LEVEL = 10;
    /// <summary>命名时机：升到 6 级时触发（设计文档"4-6 级"区间，固定为毕业半程点）</summary>
    public const int NAMING_LEVEL = 6;
    public const int NAMING_STRESS_BONUS = 20;
    public const float NAMING_ALL_STAT_PERCENT = 0.10f;
    /// <summary>每次升级获得的可分配主属性点（设计文档 2.3 节）</summary>
    public const int POINTS_PER_LEVEL = 3;

    [Header("血条引用")]
    public SpriteRenderer hpBarFill;
    public SpriteRenderer hpBarBg;
    // 字段类型故意写成 Component(非 HPBarFollower):Core 不能引用 UI 程序集,
    // 但 Inspector 仍能拖拽任何组件。UpdateHPUI 内部用 IUnitHPBar 接口转型后调用。
    public Component hpBarFollower;

    [Header("压力条引用")]
    // 同上:Core 持有 Component 引用,UpdateHPUI 用 IUnitStressBar 转型。
    public Component stressBarFollower;

    [Header("选中光圈")]
    public GameObject selectCircle;

    [Header("技能")]
    public List<SkillData> skills = new List<SkillData>();
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    [Header("状态效果")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    // ========== Spine 动画 ==========

    [Header("Spine 动画名")]
    public string idleAnimName = "Idle";
    public string attackAnimName = "atk";
    public string damagedAnimName = "Damaged";
    public string deathAnimName = "Death";

    public bool isDead => currentHP <= 0;

    /// <summary>死亡状态标记：防止 Die() 重复调用导致多次播放死亡动画</summary>
    private bool deathAnimPlayed;

    /// <summary>
    /// 执行死亡逻辑：播放死亡动画并标记单位已死。
    /// 防重入：若已调用过 Die() 或单位已死亡，静默跳过。
    /// </summary>
    public void Die()
    {
        // 防重复调用：已播过死亡动画或单位已死则跳过
        if (deathAnimPlayed || currentHP <= 0)
        {
            deathAnimPlayed = true;
            return;
        }

        deathAnimPlayed = true;
        Log.Info($"[动画] {unitName} Die() 被调用");

        var sk = GetSkeleton();
        if (sk == null || sk.AnimationState == null)
        {
            // 无骨骼时至少更新 HP 到 0
            currentHP = 0;
            UpdateHPUI();
            return;
        }

        // 播放死亡动画
        sk.AnimationState.SetAnimation(0, deathAnimName, false);
        currentHP = 0;
        UpdateHPUI();
    }

    /// <summary>
    /// 播放 Idle 动画（便利方法）。
    /// skeleton 为空或动画不存在时静默跳过。
    /// </summary>
    public void SetIdle()
    {
        Play(idleAnimName, loop: true);
    }

    /// <summary>从死亡/濒死状态恢复（死亡之门存活、战后复活等）— 立刻中断死亡动画并播 Idle</summary>
    public void Revive()
    {
        deathAnimPlayed = false; // 重置死亡标记，允许再次触发 Die()
        Log.Info($"[动画] {unitName} Revive() 被调用，currentHP={currentHP}");
        // bump guard 让所有在飞的 Complete 回调失效
        int guard = ++idleGuard;
        pendingIdle = false;

        var sk = GetSkeleton();
        if (sk == null || sk.AnimationState == null) return;

        // 如果当前 track 0 是死亡动画，强制清空并切回 Idle
        var currentT0 = sk.AnimationState.GetCurrent(0);
        if (currentT0 != null && currentT0.Animation != null && currentT0.Animation.Name == deathAnimName)
        {
            Log.Info($"[动画] {unitName} 正在播死亡动画，Revive 强制切回 Idle");
            sk.AnimationState.SetAnimation(0, idleAnimName, true);
        }
        else if (sk.AnimationState.GetCurrent(0) == null)
        {
            // 当前没在播任何东西，补一个 Idle
            Play(idleAnimName, loop: true);
        }
    }

    /// <summary>撤退/离场前调用 — 清空所有动画轨道避免残留</summary>
    public void CleanupForRetreat()
    {
        idleGuard++;
        pendingIdle = false;
        var sk = GetSkeleton();
        if (sk != null && sk.AnimationState != null)
        {
            sk.AnimationState.ClearTracks();
        }
    }

    SkeletonAnimation cachedSkeleton;

    public SkeletonAnimation GetSkeleton()
    {
        if (cachedSkeleton == null)
        {
            try
            {
                cachedSkeleton = GetComponentInChildren<SkeletonAnimation>(true);
            }
            catch
            {
                cachedSkeleton = null; // 获取失败时返回 null，不抛异常
            }
        }
        return cachedSkeleton;
    }

    /// <summary>在指定轨道上播放动画。loop=false 时播完会自动回到 Idle（已死亡则不回）。</summary>
    public void Play(string name, bool loop = false, int track = 0)
    {
        if (string.IsNullOrEmpty(name)) return; // 动画名为空时静默跳过
        var sk = GetSkeleton();
        if (sk == null || sk.AnimationState == null) return;
        var anim = sk.skeleton.Data.FindAnimation(name);
        if (anim == null) { Log.Warn($"[动画] {unitName} 找不到动画 {name}"); return; }

        Log.Info($"[动画] {unitName} Play({name}) track={track} loop={loop} duration={anim.Duration}s");
        var entry = sk.AnimationState.SetAnimation(track, name, loop);

        if (entry != null && !loop)
        {
            int guard = ++idleGuard;
            entry.Complete += (te) =>
            {
                Log.Info($"[动画] {unitName} Complete 事件触发 (guard={guard})");
                if (idleGuard == guard && !isDead)
                    pendingIdle = true;
            };
        }
    }

    bool pendingIdle;
    int idleGuard;

    void Start()
    {
        if (selectCircle == null)
        {
            var found = transform.Find("SelectCircle");
            if (found != null)
                selectCircle = found.gameObject;
        }

        // 缓存 Spine 骨骼，开始播放 Idle
        Play(idleAnimName, loop: true);
    }

    int frameSkip;
    string lastT0, lastT1;

    void LateUpdate()
    {
        if (isDead) return;
        var sk = GetSkeleton();
        if (sk == null || sk.AnimationState == null) return;

        if (pendingIdle)
        {
            pendingIdle = false;
            Log.Info($"[动画] {unitName} LateUpdate 消费 pendingIdle，恢复 Idle");
            Play(idleAnimName, loop: true);
            return;
        }

        if (sk.AnimationState.GetCurrent(0) == null)
        {
            Log.Info($"[动画] {unitName} track 0 为空，补 Idle");
            Play(idleAnimName, loop: true);
        }

        if (++frameSkip % 60 == 0)
        {
            var t0 = sk.AnimationState.GetCurrent(0);
            var t1 = sk.AnimationState.GetCurrent(1);
            string n0 = t0?.Animation?.Name ?? "null";
            string n1 = t1?.Animation?.Name ?? "null";
            if (n0 != lastT0 || n1 != lastT1)
            {
                lastT0 = n0; lastT1 = n1;
                Log.Info($"[动画] {unitName} 状态 T0={n0} T1={n1}");
            }
        }
    }

    public void UpdateHPUI()
    {
        // 转型到 Core 内的 IUnitHPBar 接口调用,避免编译期依赖 UI.HPBarFollower。
        if (hpBarFollower is IUnitHPBar bar)
        {
            bar.UpdateHP(currentHP, MaxHp);
            if (isOnDeathsDoor && currentHP <= 0)
                bar.SetDeathsDoor(true);
            else
                bar.SetDeathsDoor(false);
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

    /// <summary>获取职业基础HP（不含VIT加成）。优先 classData.baseHp，没有则返回 0。</summary>
    public int GetClassBaseHP()
    {
        if (classData != null) return classData.baseHp;
        // 反查 GameManager 已加载的职业定义
        if (GameManager.Instance != null)
        {
            var cd = GameManager.Instance.GetClassDefinition(unitName);
            if (cd != null) return cd.baseHp;
        }
        Log.Warn($"[UnitData] {unitName} 找不到 ClassDefinition，baseHp 返回 0");
        return 0;
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
            skeleton.skeleton.A = alpha;
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

    /// <summary>
    /// 升到下一级所需经验（设计文档 2.3 节：100 * L^1.8 指数曲线）。
    /// L=1..MAX_LEVEL；L<=0 视为 100；L>MAX_LEVEL 视为封顶（int.MaxValue）。
    /// 期望：1→2:100, 2→3:348, 3→4:722, 4→5:1213, 5→6:1812, 6→7:2523, 7→8:3346, 8→9:4280, 9→10:5315
    /// </summary>
    public static int GetExpForLevel(int lvl)
    {
        if (lvl <= 0) return 100;
        if (lvl >= MAX_LEVEL) return int.MaxValue;
        return Mathf.RoundToInt(100f * Mathf.Pow(lvl, 1.8f));
    }

    public void GainExp(int amount)
    {
        if (level >= MAX_LEVEL) return;
        currentExp += amount;
        BattleLog.Add($"[经验] {unitName} 获得 {amount} 经验 ({currentExp}/{ExpToNextLevel})");

        while (currentExp >= ExpToNextLevel && level < MAX_LEVEL)
        {
            currentExp -= ExpToNextLevel;
            level++;
            // V0.5: 升级不再自动加主属性，改为累加可分配点
            unassignedPoints += POINTS_PER_LEVEL;

            currentHP = MaxHp;
            UpdateHPUI();
            BattleLog.Add($"{unitName} 升级到 {level} 级！+{POINTS_PER_LEVEL} 属性点待分配（剩余 {unassignedPoints}）");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_LEVEL_UP);

            // 命名时刻：升到 NAMING_LEVEL 时一次性触发
            if (level >= NAMING_LEVEL && !isNamed)
                TriggerNamingMoment();
        }
    }

    /// <summary>
    /// V0.5 加点系统：从 unassignedPoints 取出 1 点分配到指定主属性。
    /// 返回是否成功（unassignedPoints <= 0 或 stat 不合法时返回 false）。
    /// </summary>
    public bool AllocatePoint(StatType stat)
    {
        if (unassignedPoints <= 0) return false;
        switch (stat)
        {
            case StatType.VIT: primaryAttributes.VIT++; break;
            case StatType.STR: primaryAttributes.STR++; break;
            case StatType.AGI: primaryAttributes.AGI++; break;
            case StatType.INT: primaryAttributes.INT++; break;
            default: return false;
        }
        unassignedPoints--;
        BattleLog.Add($"[属性] {unitName} 分配 1 点 → {stat}（剩余 {unassignedPoints}）");
        return true;
    }

    // ========== 命名时刻 ==========

    /// <summary>
    /// 命名时刻：全属性+10%、压力上限+20、解锁专属技能槽、获得命名怪癖。
    /// 一次性触发（isNamed 守卫），HP 满血。
    /// </summary>
    public void TriggerNamingMoment()
    {
        if (isNamed) return;
        isNamed = true;
        stressCapBonus = NAMING_STRESS_BONUS;
        namingQuirk = CreateNamingQuirk();
        // 命名怪癖为正面怪癖，参与美德概率加成（不占 5 个怪癖位）
        positiveQuirkCount++;

        // 全属性 +10%：对所有 AttributeTarget 注入 Mul 修饰器
        // 走 GetFinalStat 现有管线，永久 buff（duration=0）
        foreach (AttributeTarget target in System.Enum.GetValues(typeof(AttributeTarget)))
        {
            modifiers.Add(new AttributeModifier(
                ModifierType.Mul, target, NAMING_ALL_STAT_PERCENT, "naming_moment", 0));
        }

        // 满血
        currentHP = MaxHp;
        UpdateHPUI();

        // 日志 + 音效
        BattleLog.Add($"<color=#ffdd44>★★★ 命名时刻 ★★★ {unitName} 从炮灰蜕变为传奇！</color>");
        BattleLog.Add($"<color=#ffdd44>    → 全属性 +10% | 压力上限 +20 | 解锁专属技能槽 | 获得 [传奇之名] 怪癖</color>");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioKeys.SFX_LEVEL_UP);
    }

    /// <summary>
    /// 按 skillName 在 classData.skillPool 中查找并设为专属技能。
    /// 找不到时返回 null（调用方应记录 warn）。BattleSetup.CreateUnit 在战斗开始时调用此方法。
    /// </summary>
    public SkillData ApplyExclusiveSkillFromId(string skillId)
    {
        if (classData == null || classData.skillPool == null) return null;
        if (string.IsNullOrEmpty(skillId)) return null;
        SkillData matched = classData.skillPool.Find(s => s != null && s.skillName == skillId);
        if (matched != null)
        {
            exclusiveSkill = matched;
        }
        return matched;
    }

    /// <summary>
    /// 构造"传奇之名"命名怪癖：+5% ACC、+3% CRT、+5% SPD。
    /// 独立于 quirks 列表，不占 5 个怪癖位。
    /// </summary>
    static Quirk CreateNamingQuirk()
    {
        return new Quirk
        {
            id = "naming_legacy",
            localizedName = "传奇之名",
            isPositive = true,
            isLocked = true, // 锁定不可移除
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.ACC, amount = 5, isPercent = true },
                new QuirkEffect { stat = StatType.CRT, amount = 3, isPercent = true },
                new QuirkEffect { stat = StatType.SPD, amount = 5, isPercent = true },
            }
        };
    }
}
