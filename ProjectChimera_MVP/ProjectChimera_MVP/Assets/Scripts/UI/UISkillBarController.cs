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

        // 为每个技能生成一个按钮
        for (int i = 0; i < unit.skills.Count; i++)
        {
            int skillIndex = i; // 闭包捕获
            SkillData skill = unit.skills[i];

            GameObject btnObj = Instantiate(skillButtonPrefab, skillBarContainer);
            btnObj.name = $"Btn_{skill.skillName}";

            // 设置按钮文字
            TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"[{i + 1}] {skill.skillName}\n{skill.description}";

            // 绑定点击事件
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null && battleManager != null)
            {
                btn.onClick.AddListener(() => battleManager.SelectSkill(skillIndex));
            }

            currentButtons.Add(btnObj);
        }
    }
}
