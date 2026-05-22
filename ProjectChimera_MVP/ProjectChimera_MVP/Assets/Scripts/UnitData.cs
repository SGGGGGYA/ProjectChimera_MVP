using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitData : MonoBehaviour
{
    [Header("基础属性")]
    public string unitName;
    public int VIT = 10;
    public int STR = 10;
    public int DEF = 5;
    public int AGI = 5;
    public int INT = 3;

    [Header("装备")]
    public int weaponAttack;    // 武器攻击力

    [Header("身份标记")]
    public bool isPlayer;

    [Header("血量状态")]
    public int currentHP;
    public int MaxHp => VIT * 5 + 40;
    public int shieldHP;              // 护盾值（抵消伤害）

    [Header("压力系统")]
    public int stress;                // 当前压力值 0~100
    public const int MaxStress = 100;

    [Header("经验等级")]
    public int level = 1;
    public int currentExp;
    public int ExpToNextLevel => GetExpForLevel(level);

    [Header("血条引用")]
    public SpriteRenderer hpBarFill;    // 旧版 SpriteRenderer（兼容）
    public SpriteRenderer hpBarBg;      // 旧版 SpriteRenderer（兼容）
    public HPBarFollower hpBarFollower;   // uGUI 血条

    [Header("选中光圈")]
    public GameObject selectCircle;       // 脚底的黄色光圈，选中时亮起

    [Header("技能")]
    public List<SkillData> skills = new List<SkillData>();

    [Header("状态效果")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    void Start()
    {
        // 初始血量已在 CreateUnit() 中设置
        // 初始血条显示已在 CreateHPBar() 中通过 follower.UpdateHP() 完成
        // 这里不再重复调用 UpdateHPUI()，避免时序问题

        // 自动查找 SelectCircle（Inspector 没绑定的情况）
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
        // uGUI 血条（优先）
        if (hpBarFollower != null)
        {
            hpBarFollower.UpdateHP(currentHP, MaxHp);
            return;
        }

        // 旧版 SpriteRenderer 血条（兼容）
        if (hpBarFill != null)
        {
            float hpPercent = Mathf.Clamp01((float)currentHP / MaxHp);
            hpBarFill.transform.localScale = new Vector3(1.2f * hpPercent, 0.2f, 1);
            return;
        }

        // 两种血条都没有（初始化阶段或未绑定）
        // 这是正常的：Start() 中 hpBarFollower 可能尚未被 BattleSetup 赋值
        // DealDamage() 阶段如果还 null 则需检查绑定
    }

    // ========== 状态效果管理 ==========

    /// <summary>是否有指定类型的状态效果</summary>
    public bool HasStatus(StatusType type)
    {
        return statusEffects.Exists(e => e.type == type && !e.expired);
    }

    /// <summary>获取指定类型的第一个有效状态</summary>
    public StatusEffect GetStatus(StatusType type)
    {
        return statusEffects.Find(e => e.type == type && !e.expired);
    }

    /// <summary>添加状态效果（同类叠加刷新持续时间）</summary>
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

    /// <summary>移除指定类型的状态效果</summary>
    public void RemoveStatus(StatusType type)
    {
        statusEffects.RemoveAll(e => e.type == type);
    }

    /// <summary>每回合结束调用，让所有状态减1回合</summary>
    public void TickStatusEffects()
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            statusEffects[i].Tick();
            if (statusEffects[i].expired)
            {
                BattleLog.Add($"[状态] {unitName} 的 [{statusEffects[i].type}] 已消退");
                statusEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>处理流血伤害（回合开始时调用），返回是否存活</summary>
    public bool ProcessBleed()
    {
        StatusEffect bleed = GetStatus(StatusType.Bleed);
        if (bleed != null)
        {
            int bleedDmg = Mathf.RoundToInt(bleed.value);
            currentHP -= bleedDmg;
            BattleLog.Add($"[流血] {unitName} 受到 {bleedDmg} 点流血伤害 (HP: {currentHP}/{MaxHp})");
            UpdateHPUI();
            return currentHP > 0;
        }
        return true;
    }

    /// <summary>高亮状态</summary>
    public enum HighlightState { Normal, Highlighted, Dimmed }

    /// <summary>设置高亮状态（暗黑地牢风格）</summary>
    public void SetHighlightState(HighlightState state)
    {
        Debug.Log($"[高亮] {unitName} → {state}, selectCircle={selectCircle != null}");

        // 脚底光圈控制
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

        // 全身变暗（不可选目标视觉效果）
        var skeleton = GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.skeleton != null)
        {
            float alpha = (state == HighlightState.Dimmed) ? 0.35f : 1f;
            skeleton.skeleton.a = alpha;
        }
    }

    /// <summary>旧接口兼容：true=Highlighted, false=Normal</summary>
    public void SetHighlight(bool isSelected)
    {
        SetHighlightState(isSelected ? HighlightState.Highlighted : HighlightState.Normal);
    }

    // ========== 压力系统 ==========

    /// <summary>增加压力值，返回是否已满</summary>
    public bool AddStress(int amount)
    {
        stress = Mathf.Min(stress + amount, MaxStress);
        BattleLog.Add($"[压力] {unitName} 压力 +{amount} ({stress}/{MaxStress})");
        return stress >= MaxStress;
    }

    /// <summary>减少压力值</summary>
    public void ReduceStress(int amount)
    {
        stress = Mathf.Max(stress - amount, 0);
        BattleLog.Add($"[压力] {unitName} 压力 -{amount} ({stress}/{MaxStress})");
    }

    /// <summary>检查压力值状态</summary>
    public string GetStressBar()
    {
        int barLen = Mathf.RoundToInt((float)stress / MaxStress * 15);
        string bar = new string('█', barLen) + new string('░', 15 - barLen);
        string color = stress < 50 ? "#88ff88" : stress < 80 ? "#ffaa00" : "#ff4444";
        return $"<color={color}>{bar}</color> {stress}/{MaxStress}";
    }

    /// <summary>计算受伤倍率（标记 = 1.0 + 标记值）</summary>
    public float GetIncomingDamageMultiplier()
    {
        float multiplier = 1f;
        StatusEffect mark = GetStatus(StatusType.Mark);
        if (mark != null)
            multiplier += mark.value; // 如 0.2 = +20%
        return multiplier;
    }

    /// <summary>
    /// 计算技能原始伤害（技能 + 武器 + 属性补正，不含力量系数和防御减免）
    /// </summary>
    public int CalculateDamage(SkillData skill)
    {
        float raw = skill.baseDamage + weaponAttack + STR * skill.strScaling + AGI * skill.agiScaling;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>计算力量系数：1 + (STR - 5) * 0.05</summary>
    public float GetStrengthCoefficient()
    {
        return 1f + (STR - 5) * 0.05f;
    }

    // ========== 经验升级 ==========

    /// <summary>获取升到指定等级所需的经验值（MVP 线性：100/200/400/800/1600）</summary>
    public static int GetExpForLevel(int lvl)
    {
        if (lvl <= 0) return 100;
        if (lvl > 5) return int.MaxValue; // 满级
        return 100 * (int)Mathf.Pow(2, lvl - 1);
    }

    /// <summary>获得经验值，触发升级</summary>
    public void GainExp(int amount)
    {
        if (level >= 5) return; // 满级
        currentExp += amount;
        BattleLog.Add($"[经验] {unitName} 获得 {amount} 经验 ({currentExp}/{ExpToNextLevel})");

        while (currentExp >= ExpToNextLevel && level < 5)
        {
            currentExp -= ExpToNextLevel;
            level++;

            // 自动加点：+1 VIT，+1 主属性
            VIT++;
            if (STR > AGI && STR > INT) STR++;
            else if (AGI > STR && AGI > INT) AGI++;
            else if (isPlayer && skills.Exists(s => s.agiScaling > 0.5f)) AGI++;
            else STR++;

            // 满血恢复
            currentHP = MaxHp;
            UpdateHPUI();

            BattleLog.Add($"🎉 {unitName} 升级到 {level} 级！HP 全恢复");
        }
    }
}
