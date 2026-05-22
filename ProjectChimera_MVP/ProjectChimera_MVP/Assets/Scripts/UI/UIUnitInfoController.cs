using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 单位详情面板 — 平时隐藏，点击单位后亮起显示该单位完整信息
/// </summary>
public class UIUnitInfoController : MonoBehaviour
{
    [Header("引用")]
    public RectTransform container;       // 放 InfoRow 的容器
    public GameObject infoRowPrefab;      // 一行文字的 TMP 预制体

    private List<TextMeshProUGUI> rows = new List<TextMeshProUGUI>();
    private UnitData currentDisplayed;

    void Awake()
    {
        UnitClickDetector.OnUnitClicked += OnUnitClicked;
    }

    void OnDestroy()
    {
        UnitClickDetector.OnUnitClicked -= OnUnitClicked;
    }

    void Update()
    {
        if (gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
            Hide();
    }

    // ==================== 事件响应 ====================

    void OnUnitClicked(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("[UIUnitInfo] 收到 null 单位数据，忽略");
            return;
        }
        currentDisplayed = unit;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentDisplayed = null;
    }

    // ==================== 刷新面板 ====================

    void Refresh()
    {
        if (currentDisplayed == null) return;
        if (container == null)
        {
            Debug.LogWarning("[UIUnitInfo] container 未绑定，无法刷新");
            return;
        }
        if (infoRowPrefab == null)
        {
            Debug.LogWarning("[UIUnitInfo] infoRowPrefab 未绑定，无法刷新");
            return;
        }

        var lines = new List<string>();

        string classTag = currentDisplayed.isPlayer ? "⚔️ 己方" : "👹 敌方";
        lines.Add($"<b>{classTag}</b>");
        lines.Add($"<size=130%><b>{currentDisplayed.unitName}</b></size>");
        lines.Add($"等级: {currentDisplayed.level}");

        lines.Add("");
        string hpColor = currentDisplayed.currentHP > 0 ? "white" : "gray";
        lines.Add($"<b>生命值</b>");
        lines.Add($"HP: <color={hpColor}>{currentDisplayed.currentHP} / {currentDisplayed.MaxHp}</color>");
        float hpPct = (float)currentDisplayed.currentHP / currentDisplayed.MaxHp;
        int hpBarLen = Mathf.RoundToInt(hpPct * 20);
        string hpBar = new string('█', hpBarLen) + new string('░', 20 - hpBarLen);
        string hpBarColor = hpPct > 0.5f ? "#88ff88" : hpPct > 0.25f ? "#ffaa00" : "#ff4444";
        lines.Add($"  <color={hpBarColor}>{hpBar}</color>");

        if (currentDisplayed.shieldHP > 0)
            lines.Add($"🛡️ 护盾: {currentDisplayed.shieldHP}");

        // 压力
        lines.Add($"");
        lines.Add($"<b>压力</b>");
        lines.Add($"  {currentDisplayed.GetStressBar()}");

        lines.Add("");
        lines.Add($"<b>属性</b>");
        lines.Add($"VIT:{currentDisplayed.VIT}  STR:{currentDisplayed.STR}  DEF:{currentDisplayed.DEF_Effective}");
        lines.Add($"AGI:{currentDisplayed.AGI}  INT:{currentDisplayed.INT}");
        lines.Add($"武器攻击: {currentDisplayed.weaponAttack}");

        lines.Add("");
        if (currentDisplayed.level < 5)
        {
            float expPct = (float)currentDisplayed.currentExp / currentDisplayed.ExpToNextLevel;
            int expBarLen = Mathf.RoundToInt(expPct * 15);
            string expBar = new string('█', expBarLen) + new string('░', 15 - expBarLen);
            lines.Add($"<b>经验值</b>");
            lines.Add($"  <color=#88ff88>{expBar}</color>");
            lines.Add($"  {currentDisplayed.currentExp} / {currentDisplayed.ExpToNextLevel}");
        }
        else
        {
            lines.Add($"<b>经验值</b>");
            lines.Add("  <color=#88ff88>已满级</color>");
        }

        if (currentDisplayed.statusEffects != null && currentDisplayed.statusEffects.Count > 0)
        {
            lines.Add("");
            lines.Add($"<b>状态效果</b>");
            foreach (var e in currentDisplayed.statusEffects)
            {
                if (e != null && !e.expired)
                {
                    string dur = e.type == StatusType.Bleed ? $"每回合{e.value}点" : $"剩余{e.remainingTurns}回合";
                    lines.Add($"  [{e.type}] {dur}");
                }
            }
        }

        lines.Add("");
        lines.Add($"<color=#888888>按 E / ESC 关闭</color>");

        // --- 更新 UI ---
        EnsureRows(lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            if (rows[i] != null)
                rows[i].text = lines[i];
        }
    }

    // ========== 内部 ==========

    void EnsureRows(int count)
    {
        while (rows.Count < count)
        {
            if (infoRowPrefab == null) break;
            GameObject go = Instantiate(infoRowPrefab, container);
            if (go == null) break;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                rows.Add(tmp);
            else
                rows.Add(null);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null)
                rows[i].gameObject.SetActive(i < count);
        }
    }
}
