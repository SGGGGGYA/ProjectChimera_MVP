using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRecruitController : MonoBehaviour
{
    public GameObject panel;
    public Transform slotGrid;

    public static UIRecruitController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (panel == null) EnsurePanel();
    }

    void EnsurePanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("RecruitCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var root = new GameObject("RecruitPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 420);
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

        var title = new GameObject("Title", typeof(RectTransform));
        title.layer = 5; title.transform.SetParent(root.transform, false);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1); titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1); titleRt.sizeDelta = new Vector2(200, 30);
        titleRt.anchoredPosition = new Vector2(0, -10);
        title.AddComponent<CanvasRenderer>();
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "角色招募";
        titleTmp.fontSize = 20; titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white; UIFonts.Apply(titleTmp);

        var hint = new GameObject("Hint", typeof(RectTransform));
        hint.layer = 5; hint.transform.SetParent(root.transform, false);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 1); hintRt.anchorMax = new Vector2(0.5f, 1);
        hintRt.pivot = new Vector2(0.5f, 1); hintRt.sizeDelta = new Vector2(400, 20);
        hintRt.anchoredPosition = new Vector2(0, -36);
        hint.AddComponent<CanvasRenderer>();
        var hintTmp = hint.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "选择一名英雄加入队伍（花费 50 金币）";
        hintTmp.fontSize = 12; hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = Color.gray; UIFonts.Apply(hintTmp);

        var goldTmp = MakeLabel(root, "GoldLabel", new Vector2(20, -58), $"持有金币: {GameManager.Instance?.gold ?? 0}", 13, Color.yellow);

        var gridObj = new GameObject("SlotGrid", typeof(RectTransform));
        gridObj.layer = 5; gridObj.transform.SetParent(root.transform, false);
        var gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0); gridRt.anchorMax = new Vector2(1, 1);
        gridRt.pivot = new Vector2(0, 1);
        gridRt.offsetMin = new Vector2(20, 80); gridRt.offsetMax = new Vector2(-20, -50);
        var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(460, 56);
        gridLayout.spacing = new Vector2(0, 6);
        slotGrid = gridObj.transform;

        var closeBtn = new GameObject("CloseBtn", typeof(RectTransform));
        closeBtn.layer = 5; closeBtn.transform.SetParent(root.transform, false);
        var cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0); cRt.anchorMax = new Vector2(0.5f, 0);
        cRt.pivot = new Vector2(0.5f, 0.5f); cRt.sizeDelta = new Vector2(120, 32);
        cRt.anchoredPosition = new Vector2(0, 16);
        closeBtn.AddComponent<CanvasRenderer>();
        var cImg = closeBtn.AddComponent<Image>();
        cImg.color = new Color(0.3f, 0.3f, 0.3f);
        var cLbl = new GameObject("Label", typeof(RectTransform));
        cLbl.layer = 5; cLbl.transform.SetParent(closeBtn.transform, false);
        var clRt = cLbl.GetComponent<RectTransform>();
        clRt.anchorMin = Vector2.zero; clRt.anchorMax = Vector2.one;
        clRt.offsetMin = Vector2.zero; clRt.offsetMax = Vector2.zero;
        UIFonts.CreateLabel(cLbl, "Label", "关闭", 14, Color.white);
        var cBtn = closeBtn.AddComponent<Button>();
        cBtn.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    TextMeshProUGUI MakeLabel(GameObject parent, string name, Vector2 pos, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1); rt.sizeDelta = new Vector2(0, 20);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color; UIFonts.Apply(tmp);
        return tmp;
    }

    void Refresh()
    {
        if (slotGrid == null) return;
        foreach (Transform t in slotGrid) Destroy(t.gameObject);

        var gm = GameManager.Instance;
        if (gm == null) return;

        var defs = gm.classDefinitions;
        bool full = gm.playerTeamData.Count >= 4;

        foreach (var def in defs)
        {
            bool alreadyInTeam = gm.playerTeamData.Exists(u => u.unitName == def.unitName);
            var go = new GameObject($"Slot_{def.id}", typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460, 56);

            var bg = go.AddComponent<Image>();
            bg.color = alreadyInTeam ? new Color(0.2f, 0.25f, 0.2f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var nameTmp = new GameObject("Name", typeof(RectTransform));
            nameTmp.layer = 5; nameTmp.transform.SetParent(go.transform, false);
            var nrt = nameTmp.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = new Vector2(0.4f, 1);
            nrt.offsetMin = new Vector2(10, 0); nrt.offsetMax = Vector2.zero;
            var ntmp = nameTmp.AddComponent<TextMeshProUGUI>();
            ntmp.text = def.unitName;
            ntmp.fontSize = 18; ntmp.alignment = TextAlignmentOptions.MidlineLeft;
            ntmp.color = Color.white; UIFonts.Apply(ntmp);

            var statTmp = new GameObject("Stats", typeof(RectTransform));
            statTmp.layer = 5; statTmp.transform.SetParent(go.transform, false);
            var srt = statTmp.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.4f, 0); srt.anchorMax = new Vector2(0.8f, 1);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var stmp = statTmp.AddComponent<TextMeshProUGUI>();
            stmp.text = $"HP:{def.baseHp} 力:{def.baseSTR} 敏:{def.baseAGI} 智:{def.baseINT}";
            stmp.fontSize = 11; stmp.alignment = TextAlignmentOptions.MidlineLeft;
            stmp.color = Color.gray; UIFonts.Apply(stmp);

            var btnGo = new GameObject("ActionBtn", typeof(RectTransform));
            btnGo.layer = 5; btnGo.transform.SetParent(go.transform, false);
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.8f, 0); brt.anchorMax = new Vector2(1, 1);
            brt.offsetMin = Vector2.zero; brt.offsetMax = new Vector2(-6, 0);
            var bImg = btnGo.AddComponent<Image>();

            var bLbl = new GameObject("Label", typeof(RectTransform));
            bLbl.layer = 5; bLbl.transform.SetParent(btnGo.transform, false);
            var blRt = bLbl.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero; blRt.offsetMax = Vector2.zero;
            var blTmp = bLbl.AddComponent<TextMeshProUGUI>();
            blTmp.fontSize = 12; blTmp.alignment = TextAlignmentOptions.Center;
            UIFonts.Apply(blTmp);

            if (alreadyInTeam)
            {
                bImg.color = new Color(0.25f, 0.35f, 0.25f);
                blTmp.text = "已在队";
                blTmp.color = Color.gray;
            }
            else if (full)
            {
                bImg.color = new Color(0.4f, 0.3f, 0.2f);
                blTmp.text = "替换";
                blTmp.color = Color.white;
                btnGo.AddComponent<Button>().onClick.AddListener(() => RecruitReplace(def));
            }
            else
            {
                bImg.color = new Color(0.3f, 0.5f, 0.3f);
                blTmp.text = "招募";
                blTmp.color = Color.white;
                btnGo.AddComponent<Button>().onClick.AddListener(() => RecruitNew(def));
            }

            go.transform.SetParent(slotGrid, false);
        }
    }

    void RecruitNew(ClassDefinition def)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData.Count >= 4) return;
        if (gm.gold < 50) { Debug.Log("金币不足"); return; }

        gm.gold -= 50;
        var data = MakeUnitData(def);
        gm.playerTeamData.Add(data);
        Refresh();
    }

    void RecruitReplace(ClassDefinition def)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.gold < 50) { Debug.Log("金币不足"); return; }

        gm.gold -= 50;
        // 找到同职业的替换，否则替换最后一名
        int idx = gm.playerTeamData.FindIndex(u => u.unitName == def.unitName);
        if (idx < 0) idx = gm.playerTeamData.Count - 1;
        gm.playerTeamData[idx] = MakeUnitData(def);
        Refresh();
    }

    UnitBattleData MakeUnitData(ClassDefinition def)
    {
        var data = new UnitBattleData();
        data.unitName = def.unitName;
        data.VIT = def.baseVIT;
        data.STR = def.baseSTR;
        data.DEF = def.baseDEF;
        data.AGI = def.baseAGI;
        data.INT = def.baseINT;
        data.weaponAttack = def.baseWeaponAttack;
        data.level = 1;
        data.isPlayer = true;
        data.maxHp = data.ComputeMaxHP();
        data.currentHP = data.maxHp;
        data.rank = 0;
        return data;
    }

    public void Open()
    {
        if (panel == null) EnsurePanel();
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
}
