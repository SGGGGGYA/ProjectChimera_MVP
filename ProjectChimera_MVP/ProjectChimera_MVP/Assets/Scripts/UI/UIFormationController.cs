using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFormationController : MonoBehaviour
{
    public GameObject panel;
    public Transform slotGrid;
    public Button btnClose;

    private int selectedIndex = -1;
    private readonly List<GameObject> slotGOs = new List<GameObject>();

    public static UIFormationController Instance { get; private set; }

    static readonly string[] RankLabels = { "前排", "中前", "中后", "后排" };

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
        if (Input.GetKeyDown(KeyCode.F) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel != null && panel.activeSelf) Close();
            else Open();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    void EnsurePanel()
    {
        if (panel != null) return;

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("FormationCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var root = new GameObject("FormationPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 400);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var title = new GameObject("Title", typeof(RectTransform));
        title.layer = 5;
        title.transform.SetParent(root.transform, false);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(300, 30);
        titleRt.anchoredPosition = new Vector2(0, -8);
        title.AddComponent<CanvasRenderer>();
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "阵型编排";
        titleTmp.fontSize = 20;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        var hint = new GameObject("Hint", typeof(RectTransform));
        hint.layer = 5;
        hint.transform.SetParent(root.transform, false);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 1);
        hintRt.anchorMax = new Vector2(0.5f, 1);
        hintRt.pivot = new Vector2(0.5f, 1);
        hintRt.sizeDelta = new Vector2(400, 20);
        hintRt.anchoredPosition = new Vector2(0, -34);
        hint.AddComponent<CanvasRenderer>();
        var hintTmp = hint.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "点击两个角色交换站位";
        hintTmp.fontSize = 12;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = Color.gray;
        UIFonts.Apply(hintTmp);

        var gridObj = new GameObject("SlotGrid", typeof(RectTransform));
        gridObj.layer = 5;
        gridObj.transform.SetParent(root.transform, false);
        var gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0);
        gridRt.anchorMax = new Vector2(1, 1);
        gridRt.pivot = new Vector2(0, 1);
        gridRt.offsetMin = new Vector2(30, 60);
        gridRt.offsetMax = new Vector2(-30, -50);
        var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(440, 50);
        gridLayout.spacing = new Vector2(0, 6);
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        slotGrid = gridObj.transform;

        btnClose = MakeButton(root, "CloseBtn", "关闭", new Color(0.3f, 0.3f, 0.3f));
        btnClose.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    Button MakeButton(GameObject parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120, 36);
        rt.anchoredPosition = new Vector2(0, 16);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var lbl = new GameObject("Label", typeof(RectTransform));
        lbl.layer = 5;
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        UIFonts.CreateLabel(lbl, "Label", label, 14, Color.white);
        return go.AddComponent<Button>();
    }

    public void Open()
    {
        if (panel == null) EnsurePanel();
        selectedIndex = -1;
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        selectedIndex = -1;
    }

    void Refresh()
    {
        if (slotGrid == null) return;
        foreach (Transform t in slotGrid)
            Destroy(t.gameObject);
        slotGOs.Clear();

        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData == null) return;

        for (int i = 0; i < gm.playerTeamData.Count; i++)
        {
            var unit = gm.playerTeamData[i];
            var slot = MakeSlot(unit.unitName, unit.rank, i);
            slot.transform.SetParent(slotGrid, false);
            slotGOs.Add(slot);
        }
    }

    GameObject MakeSlot(string unitName, int rank, int index)
    {
        var go = new GameObject($"Slot_{index}", typeof(RectTransform));
        go.layer = 5;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(440, 50);

        var bg = go.AddComponent<Image>();
        if (selectedIndex == index)
            bg.color = new Color(0.35f, 0.5f, 0.35f, 0.9f);
        else
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var nameTmp = new GameObject("Name", typeof(RectTransform));
        nameTmp.layer = 5;
        nameTmp.transform.SetParent(go.transform, false);
        var nrt = nameTmp.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0, 0);
        nrt.anchorMax = new Vector2(0.5f, 1);
        nrt.offsetMin = Vector2.zero;
        nrt.offsetMax = Vector2.zero;
        var ntmp = nameTmp.AddComponent<TextMeshProUGUI>();
        ntmp.text = unitName;
        ntmp.fontSize = 18;
        ntmp.alignment = TextAlignmentOptions.MidlineLeft;
        ntmp.color = Color.white;
        UIFonts.Apply(ntmp);
        nrt.offsetMin = new Vector2(10, 0);

        var rankTmp = new GameObject("Rank", typeof(RectTransform));
        rankTmp.layer = 5;
        rankTmp.transform.SetParent(go.transform, false);
        var rrt = rankTmp.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 0);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        var rtmp = rankTmp.AddComponent<TextMeshProUGUI>();
        string label = (rank >= 0 && rank < RankLabels.Length) ? RankLabels[rank] : $"站位{rank}";
        rtmp.text = $"<color=#aaaaaa>站位: </color>{label}";
        rtmp.fontSize = 16;
        rtmp.alignment = TextAlignmentOptions.MidlineRight;
        rtmp.color = Color.cyan;
        UIFonts.Apply(rtmp);
        rrt.offsetMax = new Vector2(-10, 0);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.5f, 0.35f);
        btn.colors = colors;
        int idx = index;
        btn.onClick.AddListener(() => OnSlotClick(idx));

        return go;
    }

    void OnSlotClick(int index)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData == null) return;
        if (index < 0 || index >= gm.playerTeamData.Count) return;

        if (selectedIndex == -1)
        {
            selectedIndex = index;
            Refresh();
            return;
        }

        if (selectedIndex == index)
        {
            selectedIndex = -1;
            Refresh();
            return;
        }

        SwapRanks(selectedIndex, index);
        selectedIndex = -1;
        Refresh();
    }

    void SwapRanks(int a, int b)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData == null) return;
        if (a < 0 || a >= gm.playerTeamData.Count || b < 0 || b >= gm.playerTeamData.Count) return;

        string nameA = gm.playerTeamData[a].unitName;
        string nameB = gm.playerTeamData[b].unitName;
        int rankA = gm.playerTeamData[a].rank;
        int rankB = gm.playerTeamData[b].rank;

        gm.playerTeamData[a].rank = rankB;
        gm.playerTeamData[b].rank = rankA;

        Debug.Log($"[阵型] 交换 {nameA} (站位 {rankA}) 与 {nameB} (站位 {rankB})");
    }
}
