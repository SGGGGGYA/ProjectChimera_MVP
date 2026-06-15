using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 技能栏控制器 — 挂在 SkillBar 容器上
/// 玩家回合开始后自动生成技能按钮
/// </summary>
public class UISkillBarController : MonoBehaviour
{
    [Header("引用")]
    public RectTransform skillBarContainer;  // 技能按钮的父容器
    public GameObject skillButtonPrefab;     // 技能按钮预制体
    public BattleManager battleManager;      // 战斗管理器

    private List<GameObject> currentButtons = new List<GameObject>();
    private Image backgroundPanel;

    // 技能序号 → DD1 ability 序号映射
    static readonly string[] AbilityIndexMap = { "one", "two", "three", "four", "five", "six", "seven" };

    bool CanUseSkill(UnitData unit, SkillData skill)
    {
        if (unit == null || skill == null) return false;
        return unit.rank >= skill.minUserRank && unit.rank <= skill.maxUserRank;
    }

    Sprite SafeLoadSprite(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
            Log.Warn($"[DD1Art] 加载失败: {path}，将使用灰色占位");
        return sp;
    }

    void EnsureBackgroundPanel()
    {
        if (backgroundPanel != null) return;
        if (skillBarContainer == null) return;

        // 查找或创建背景底板
        Transform bgTr = skillBarContainer.Find("DD1_SkillBar_BG");
        if (bgTr != null)
        {
            backgroundPanel = bgTr.GetComponent<Image>();
            return;
        }

        GameObject bgGo = new GameObject("DD1_SkillBar_BG", typeof(RectTransform));
        bgGo.layer = 5;
        bgGo.transform.SetParent(skillBarContainer, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.SetAsFirstSibling();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        bgGo.AddComponent<CanvasRenderer>();
        backgroundPanel = bgGo.AddComponent<Image>();
        // 使用 DD1 banner 面板作为技能栏底板
        Sprite panelSprite = SafeLoadSprite("UI/Panels/dd1_panel_banner");
        backgroundPanel.sprite = panelSprite;
        backgroundPanel.color = panelSprite != null ? Color.white : new Color(0.1f, 0.1f, 0.1f, 0.8f);
        backgroundPanel.raycastTarget = false;
    }

    Sprite FindSkillIcon(UnitData unit, int skillIndex)
    {
        // 尝试按角色名 + 技能序号匹配 DD1 ability 图标
        string key = unit.unitName.ToLower().Replace(" ", "_")
            .Replace("\u6e38\u4fa0", "highwayman")
            .Replace("\u6218\u58eb", "crusader")
            .Replace("\u5b66\u8005", "antiquarian")
            .Replace("\u72c2\u6218\u58eb", "hellion");

        if (skillIndex >= 0 && skillIndex < AbilityIndexMap.Length)
        {
            string abilityIdx = AbilityIndexMap[skillIndex];
            Sprite sp = SafeLoadSprite($"UI/Icons/Skills/dd1_{key}_{key}.ability.{abilityIdx}");
            if (sp != null) return sp;
        }

        // 模糊匹配：遍历所有技能图标
        var allIcons = Resources.LoadAll<Sprite>("UI/Icons/Skills");
        foreach (var icon in allIcons)
        {
            if (icon.name.ToLower().Contains(key))
                return icon;
        }
        return null;
    }

    /// <summary>刷新技能栏：清空旧按钮，为指定单位生成新按钮</summary>
    public void RefreshSkills(UnitData unit)
    {
        // 清空旧按钮
        foreach (var btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        // 隐藏整个技能栏
        if (unit == null || unit.skills.Count == 0)
        {
            skillBarContainer.gameObject.SetActive(false);
            return;
        }

        skillBarContainer.gameObject.SetActive(true);
        EnsureBackgroundPanel();

        // 为每个技能生成一个按钮
        for (int i = 0; i < unit.skills.Count; i++)
        {
            int skillIndex = i; // 闭包捕获
            SkillData skill = unit.skills[i];
            bool canUse = CanUseSkill(unit, skill);
            bool isExclusive = unit.exclusiveSkill != null && unit.exclusiveSkill.skillName == skill.skillName;

            GameObject btnObj = Instantiate(skillButtonPrefab, skillBarContainer);
            btnObj.name = $"Btn_{skill.skillName}";

            // 设置按钮文字
            TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                string exclusiveTag = isExclusive ? "<color=#ffdd44>★ 传奇专属 ★</color>  " : "";
                tmp.text = $"{exclusiveTag}[{i + 1}] {skill.skillName}\n{skill.description}";
                if (isExclusive)
                    tmp.color = new Color(1f, 0.92f, 0.55f);  // 专属技能: 金色
                else if (!canUse)
                    tmp.color = Color.gray;
            }

            // 绑定点击事件
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = canUse;
                if (canUse && battleManager != null)
                    btn.onClick.AddListener(() => battleManager.SelectSkill(skillIndex));
            }

            // 添加 DD1 技能图标（按钮左侧）
            Sprite skillIcon = FindSkillIcon(unit, i);
            if (skillIcon != null)
            {
                GameObject iconGo = new GameObject("SkillIcon", typeof(RectTransform));
                iconGo.layer = 5;
                iconGo.transform.SetParent(btnObj.transform, false);
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.pivot = new Vector2(0, 0.5f);
                iconRt.sizeDelta = new Vector2(32, 32);
                iconRt.anchoredPosition = new Vector2(4, 0);

                iconGo.AddComponent<CanvasRenderer>();
                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = skillIcon;
                iconImg.color = canUse ? Color.white : Color.gray;
                iconImg.raycastTarget = false;
            }

            currentButtons.Add(btnObj);
        }
    }
}
