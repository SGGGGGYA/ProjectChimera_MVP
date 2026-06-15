using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 英雄厅 — 命名后的英雄专属技能选择 UI。
///
/// 入口：WorldMap 状态下按 H 打开（与其他面板一致：I/C/B/F/H）。
///
/// 数据流：
///   - 读取：GameManager.playerTeamData 中所有 isNamed == true 的英雄
///   - 写入：UnitBattleData.exclusiveSkillId（按 skillName 匹配）
///   - 持久化：随 SaveData.playerTeam 一起被 SaveManager 保存
///
/// 显示规则：
///   - 左侧：英雄列表（仅显示已命名 + 已升 5 级的英雄）
///   - 右侧：当前选中英雄在 classData.skillPool 中可选的技能
///   - 已装备技能标记为 ★ 传奇专属 ★；其他可点"装备"按钮
///   - 未命名英雄不可选，提示"该英雄尚未命名（需升至 5 级）"
/// </summary>
public class UIHeroHallController : MonoBehaviour
{
    public static UIHeroHallController Instance { get; private set; }

    public GameObject panel;
    Transform heroList;
    Transform skillList;
    TextMeshProUGUI titleTmp;
    TextMeshProUGUI detailTmp;
    UnitBattleData selectedHero;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (panel == null) EnsurePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel != null && panel.activeSelf) Close();
            else Open();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ==================== 面板构建 ====================

    void EnsurePanel()
    {
        if (panel != null) return;

        UIFonts.EnsureEventSystem();

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("HeroHallCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        var root = new GameObject("HeroHallPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(720, 460);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5; bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // 标题
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5; titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(600, 32);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleGo.AddComponent<CanvasRenderer>();
        titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "★ 英雄厅 ★  传奇专属技能选择";
        titleTmp.fontSize = 22;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.86f, 0.27f); // 金色
        UIFonts.Apply(titleTmp);

        // 左侧英雄列表
        var heroListObj = new GameObject("HeroList", typeof(RectTransform));
        heroListObj.layer = 5; heroListObj.transform.SetParent(root.transform, false);
        var hlRt = heroListObj.GetComponent<RectTransform>();
        hlRt.anchorMin = new Vector2(0, 0);
        hlRt.anchorMax = new Vector2(0, 1);
        hlRt.pivot = new Vector2(0, 1);
        hlRt.offsetMin = new Vector2(20, 60);
        hlRt.offsetMax = new Vector2(-500, -55);
        var hlImg = heroListObj.AddComponent<Image>();
        hlImg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        var hlLayout = heroListObj.AddComponent<VerticalLayoutGroup>();
        hlLayout.spacing = 4;
        hlLayout.padding = new RectOffset(6, 6, 6, 6);
        hlLayout.childControlHeight = false;
        hlLayout.childControlWidth = true;
        hlLayout.childForceExpandWidth = true;
        heroList = heroListObj.transform;

        // 右侧技能列表
        var skillListObj = new GameObject("SkillList", typeof(RectTransform));
        skillListObj.layer = 5; skillListObj.transform.SetParent(root.transform, false);
        var slRt = skillListObj.GetComponent<RectTransform>();
        slRt.anchorMin = new Vector2(0, 0);
        slRt.anchorMax = new Vector2(0, 1);
        slRt.pivot = new Vector2(0, 1);
        slRt.offsetMin = new Vector2(240, 100);
        slRt.offsetMax = new Vector2(-20, -55);
        var slImg = skillListObj.AddComponent<Image>();
        slImg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        var slLayout = skillListObj.AddComponent<VerticalLayoutGroup>();
        slLayout.spacing = 6;
        slLayout.padding = new RectOffset(6, 6, 6, 6);
        slLayout.childControlHeight = false;
        slLayout.childControlWidth = true;
        slLayout.childForceExpandWidth = true;
        skillList = skillListObj.transform;

        // 右侧详情
        var detailGo = new GameObject("Detail", typeof(RectTransform));
        detailGo.layer = 5; detailGo.transform.SetParent(root.transform, false);
        var dRt = detailGo.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0, 0);
        dRt.anchorMax = new Vector2(1, 0);
        dRt.pivot = new Vector2(0.5f, 0);
        dRt.sizeDelta = new Vector2(0, 50);
        dRt.anchoredPosition = new Vector2(0, 8);
        detailGo.AddComponent<CanvasRenderer>();
        detailTmp = detailGo.AddComponent<TextMeshProUGUI>();
        detailTmp.text = "从左侧选择一位英雄，查看其可选的传奇专属技能。";
        detailTmp.fontSize = 12;
        detailTmp.alignment = TextAlignmentOptions.Center;
        detailTmp.color = new Color(0.7f, 0.7f, 0.7f);
        UIFonts.Apply(detailTmp);

        // 关闭按钮
        var closeBtn = new GameObject("CloseBtn", typeof(RectTransform));
        closeBtn.layer = 5; closeBtn.transform.SetParent(root.transform, false);
        var cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(1, 1);
        cRt.sizeDelta = new Vector2(40, 40);
        cRt.anchoredPosition = new Vector2(-4, -4);
        closeBtn.AddComponent<CanvasRenderer>();
        var cImg = closeBtn.AddComponent<Image>();
        cImg.color = new Color(0.4f, 0.15f, 0.15f);
        var cLbl = new GameObject("X", typeof(RectTransform));
        cLbl.layer = 5; cLbl.transform.SetParent(closeBtn.transform, false);
        var cLblRt = cLbl.GetComponent<RectTransform>();
        cLblRt.anchorMin = Vector2.zero; cLblRt.anchorMax = Vector2.one;
        cLblRt.offsetMin = Vector2.zero; cLblRt.offsetMax = Vector2.zero;
        cLbl.AddComponent<CanvasRenderer>();
        var cTmp = cLbl.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.fontSize = 16;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;
        UIFonts.Apply(cTmp);
        closeBtn.AddComponent<Button>().onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLOSE); Close(); });

        panel.SetActive(false);
    }

    // ==================== Open / Close ====================

    public void Open()
    {
        EnsurePanel();
        selectedHero = null;
        Refresh();
        panel.SetActive(true);
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_OPEN);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLOSE);
    }

    // ==================== 刷新 ====================

    void Refresh()
    {
        if (heroList == null || skillList == null) return;
        ClearChildren(heroList);
        ClearChildren(skillList);

        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData == null) return;

        // 没有命名英雄时显示空态
        bool hasAnyNamed = gm.playerTeamData.Exists(u => u != null && u.isNamed);
        if (!hasAnyNamed)
        {
            var empty = MakeLabel(heroList.gameObject, "EmptyTip", 110, 14, new Color(0.6f, 0.6f, 0.6f));
            empty.text = "<color=#aaaaaa>暂无可命名的英雄\n需升至 5 级后解锁</color>";
            empty.alignment = TextAlignmentOptions.Center;
            detailTmp.text = "提示：在战斗中获取经验，英雄升到 5 级时会触发「命名时刻」。";
            return;
        }

        foreach (var u in gm.playerTeamData)
        {
            if (u == null) continue;
            var btn = MakeHeroRow(u);
            btn.transform.SetParent(heroList, false);
        }

        // 选中第一个命名英雄
        if (selectedHero == null || !selectedHero.isNamed)
        {
            selectedHero = gm.playerTeamData.Find(u => u != null && u.isNamed);
        }
        RenderSkillList();
    }

    void RenderSkillList()
    {
        ClearChildren(skillList);

        if (selectedHero == null || !selectedHero.isNamed)
        {
            var tip = MakeLabel(skillList.gameObject, "Tip", 24, 12, new Color(0.6f, 0.6f, 0.6f));
            tip.text = "请从左侧选择一位已命名英雄。";
            tip.alignment = TextAlignmentOptions.Center;
            return;
        }

        var cd = GameManager.Instance?.GetClassDefinition(selectedHero.unitName);
        if (cd == null || cd.skillPool == null || cd.skillPool.Count == 0)
        {
            var tip = MakeLabel(skillList.gameObject, "Tip", 24, 12, new Color(0.6f, 0.6f, 0.6f));
            tip.text = $"{selectedHero.unitName} 的职业没有可选的专属技能池。";
            tip.alignment = TextAlignmentOptions.Center;
            return;
        }

        bool noneSelected = string.IsNullOrEmpty(selectedHero.exclusiveSkillId);
        detailTmp.text = noneSelected
            ? $"<b>{selectedHero.unitName}</b>  传奇 <color=#ffdd44>★ {selectedHero.level} 级 ★</color>  尚未选择专属技能"
            : $"<b>{selectedHero.unitName}</b>  传奇 <color=#ffdd44>★ {selectedHero.level} 级 ★</color>  当前专属: <color=#ffdd44>{selectedHero.exclusiveSkillId}</color>";

        foreach (var skill in cd.skillPool)
        {
            if (skill == null) continue;
            var row = MakeSkillRow(selectedHero, skill);
            row.transform.SetParent(skillList, false);
        }
    }

    // ==================== 行构造 ====================

    GameObject MakeHeroRow(UnitBattleData u)
    {
        var go = new GameObject($"Hero_{u.unitName}", typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 48);

        var img = go.AddComponent<Image>();
        bool isSel = (selectedHero == u);
        if (!u.isNamed)
            img.color = new Color(0.18f, 0.18f, 0.18f, 0.8f);
        else if (isSel)
            img.color = new Color(0.35f, 0.30f, 0.15f, 0.9f); // 选中（暖色）
        else
            img.color = new Color(0.25f, 0.25f, 0.20f, 0.85f);

        var lbl = MakeLabel(go, "Lbl", 48, 12, u.isNamed ? new Color(1f, 0.86f, 0.27f) : Color.gray);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = new Vector2(8, 0);
        lbl.rectTransform.offsetMax = new Vector2(-8, 0);

        if (u.isNamed)
            lbl.text = $"<b>{u.unitName}</b>  Lv.{u.level}  <color=#ffdd44>★ 传奇 ★</color>\n<size=10>专属: {(string.IsNullOrEmpty(u.exclusiveSkillId) ? "<color=#888888>未选</color>" : u.exclusiveSkillId)}</size>";
        else
            lbl.text = $"{u.unitName}  Lv.{u.level}\n<size=10><color=#888888>需升至 5 级解锁</color></size>";

        if (u.isNamed)
        {
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                selectedHero = u;
                AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
                Refresh();
            });
        }
        return go;
    }

    GameObject MakeSkillRow(UnitBattleData hero, SkillData skill)
    {
        bool isEquipped = !string.IsNullOrEmpty(hero.exclusiveSkillId) && hero.exclusiveSkillId == skill.skillName;

        var go = new GameObject($"Skill_{skill.skillName}", typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        var img = go.AddComponent<Image>();
        img.color = isEquipped
            ? new Color(0.45f, 0.35f, 0.10f, 0.9f)    // 装备中（金色）
            : new Color(0.15f, 0.20f, 0.25f, 0.85f);   // 未装备（蓝灰）

        // 技能描述（左 70%）
        var lbl = MakeLabel(go, "Lbl", 60, 11, Color.white);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = new Vector2(0.72f, 1);
        lbl.rectTransform.offsetMin = new Vector2(8, 0);
        lbl.rectTransform.offsetMax = Vector2.zero;
        string target = SkillTargetLabel(skill.targetType);
        string rankRange = (skill.minUserRank == skill.maxUserRank)
            ? $"rank {skill.minUserRank}"
            : $"rank {skill.minUserRank}-{skill.maxUserRank}";
        string prefix = isEquipped ? "<color=#ffdd44>★ 传奇专属 ★</color>  " : "";
        lbl.text = $"{prefix}<b>{skill.skillName}</b>\n<size=10><color=#bbbbbb>{target} · {rankRange}</color>\n{skill.description}</size>";

        // 装备按钮（右 28%）
        var btnGo = new GameObject("EquipBtn", typeof(RectTransform));
        btnGo.layer = 5; btnGo.transform.SetParent(go.transform, false);
        var brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.72f, 0);
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(4, 6);
        brt.offsetMax = new Vector2(-6, -6);
        var bImg = btnGo.AddComponent<Image>();
        bImg.color = isEquipped ? new Color(0.3f, 0.4f, 0.3f) : new Color(0.3f, 0.5f, 0.3f);
        var bLbl = MakeLabel(btnGo, "Lbl", 32, 12, isEquipped ? new Color(0.7f, 1f, 0.7f) : Color.white);
        bLbl.text = isEquipped ? "已装备" : "装备";

        if (!isEquipped)
        {
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                EquipExclusiveSkill(hero, skill);
                AudioManager.Instance?.PlaySFX(AudioKeys.SFX_LEVEL_UP);
                Refresh();
            });
        }
        return go;
    }

    // ==================== 操作 ====================

    /// <summary>
    /// 把 skill 设为 hero 的专属技能（仅修改 UnitBattleData 持久化字段，
    /// 下次 BattleSetup.CreateUnit 会读取并赋给 UnitData.exclusiveSkill）。
    /// </summary>
    public static void EquipExclusiveSkill(UnitBattleData hero, SkillData skill)
    {
        if (hero == null || skill == null) return;
        if (!hero.isNamed)
        {
            Log.Warn($"[英雄厅] {hero.unitName} 尚未命名，无法装备专属技能");
            return;
        }
        hero.exclusiveSkillId = skill.skillName;
        BattleLog.Add($"<color=#ffdd44>[英雄厅] {hero.unitName} 装备传奇专属技能：{skill.skillName}</color>");
        Log.Info($"[英雄厅] {hero.unitName} 装备专属技能 → {skill.skillName}");
    }

    // ==================== 工具 ====================

    TextMeshProUGUI MakeLabel(GameObject parent, string name, float height, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        UIFonts.Apply(tmp);
        return tmp;
    }

    static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    static string SkillTargetLabel(SkillTargetType t)
    {
        switch (t)
        {
            case SkillTargetType.SingleEnemy: return "单体敌方";
            case SkillTargetType.SingleAlly:  return "单体友方";
            case SkillTargetType.AllEnemies:  return "敌方全体";
            case SkillTargetType.Self:        return "自身";
            default: return t.ToString();
        }
    }
}
