using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIDungeonMapController : MonoBehaviour
{
    public static UIDungeonMapController Instance { get; private set; }

    public GameObject panel;
    public Transform nodeContainer;
    public Transform lineContainer;
    public TextMeshProUGUI floorLabel;
    public TextMeshProUGUI infoLabel;

    private DungeonMap currentMap;
    private List<GameObject> nodeButtons = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();

    const float NODE_SPACING_X = 130f;
    const float NODE_SPACING_Y = 110f;
    const float PANEL_OFFSET_X = 0f;
    const float PANEL_OFFSET_Y = 30f;
    const float NODE_SIZE = 36f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance != null && GameManager.Instance.dungeonSession.isInDungeon)
        {
            Open();
        }
    }

    void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            var session = GameManager.Instance?.dungeonSession;
            if (session != null && session.isInDungeon)
            {
                ConfirmExitDungeon();
            }
        }
    }

    public void Open()
    {
        if (panel == null) EnsurePanel();

        var session = GameManager.Instance?.dungeonSession;
        if (session == null || session.currentMap == null)
        {
            Log.Error("[DungeonMap] 没有地牢会话或地图数据");
            return;
        }

        currentMap = session.currentMap;
        RefreshMap();
        panel.SetActive(true);
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_OPEN);
    }

    public void Close()
    {
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLOSE);
        if (panel != null) panel.SetActive(false);
    }

    void EnsurePanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("DungeonMapCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
        UIFonts.EnsureEventSystem();

        var root = new GameObject("DungeonMapPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.97f);
        bgImg.raycastTarget = true;

        floorLabel = CreateLabel(root, "FloorLabel", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), "地牢 第 1 层", 22, Color.yellow);

        infoLabel = CreateLabel(root, "InfoLabel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), "点击可访问的节点", 14, Color.gray);

        var linesObj = new GameObject("Lines", typeof(RectTransform));
        linesObj.layer = 5;
        linesObj.transform.SetParent(root.transform, false);
        var lrt = linesObj.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        lineContainer = linesObj.transform;

        var nodesObj = new GameObject("Nodes", typeof(RectTransform));
        nodesObj.layer = 5;
        nodesObj.transform.SetParent(root.transform, false);
        var nrt = nodesObj.GetComponent<RectTransform>();
        nrt.anchorMin = Vector2.zero;
        nrt.anchorMax = Vector2.one;
        nrt.offsetMin = Vector2.zero;
        nrt.offsetMax = Vector2.zero;
        nodeContainer = nodesObj.transform;

        var exitBtn = CreateButton(root, "ExitBtn", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -15), "退出", 14, new Color(0.4f, 0.2f, 0.2f), () =>
        {
            ConfirmExitDungeon();
        });

        panel.SetActive(false);
    }

    void RefreshMap()
    {
        foreach (var go in nodeButtons) Destroy(go);
        foreach (var go in lineObjects) Destroy(go);
        nodeButtons.Clear();
        lineObjects.Clear();

        if (currentMap == null) return;

        floorLabel.text = $"地牢 第 {currentMap.currentFloor} 层";

        var session = GameManager.Instance?.dungeonSession;
        if (session == null) return;

        HashSet<int> accessible = GetAccessibleNodes();

        float totalWidth = (currentMap.maxNodesPerRow - 1) * NODE_SPACING_X;
        float startX = -totalWidth / 2f;

        foreach (var node in currentMap.nodes)
        {
            float xPos = PANEL_OFFSET_X + startX + node.x * NODE_SPACING_X;
            float yPos = PANEL_OFFSET_Y + (3 - node.y) * NODE_SPACING_Y;

            var btnGO = CreateNodeButton(node, new Vector2(xPos, yPos), accessible.Contains(currentMap.nodes.IndexOf(node)));
            nodeButtons.Add(btnGO);
        }

        foreach (var node in currentMap.nodes)
        {
            int idx = currentMap.nodes.IndexOf(node);
            Vector2 fromPos = GetNodeScreenPos(node);

            foreach (int nextIdx in node.nextNodeIndices)
            {
                if (nextIdx < 0 || nextIdx >= currentMap.nodes.Count) continue;
                var nextNode = currentMap.nodes[nextIdx];
                Vector2 toPos = GetNodeScreenPos(nextNode);
                var lineGO = CreateConnectionLine(fromPos, toPos, node.visited);
                lineObjects.Add(lineGO);
            }
        }
    }

    HashSet<int> GetAccessibleNodes()
    {
        HashSet<int> accessible = new HashSet<int>();
        if (currentMap == null) return accessible;

        var session = GameManager.Instance?.dungeonSession;
        if (session == null) return accessible;

        bool anyVisited = false;
        for (int i = 0; i < currentMap.nodes.Count; i++)
        {
            if (currentMap.nodes[i].visited)
            {
                anyVisited = true;
                foreach (int nextIdx in currentMap.nodes[i].nextNodeIndices)
                {
                    if (nextIdx >= 0 && nextIdx < currentMap.nodes.Count && !currentMap.nodes[nextIdx].visited)
                        accessible.Add(nextIdx);
                }
            }
        }

        if (!anyVisited)
        {
            for (int i = 0; i < currentMap.nodes.Count; i++)
            {
                if (currentMap.nodes[i].y == 0 && !currentMap.nodes[i].visited)
                    accessible.Add(i);
            }
        }

        return accessible;
    }

    Vector2 GetNodeScreenPos(DungeonNode node)
    {
        if (currentMap == null) return Vector2.zero;
        float totalWidth = (currentMap.maxNodesPerRow - 1) * NODE_SPACING_X;
        float startX = -totalWidth / 2f;
        float xPos = PANEL_OFFSET_X + startX + node.x * NODE_SPACING_X;
        float yPos = PANEL_OFFSET_Y + (3 - node.y) * NODE_SPACING_Y;
        return new Vector2(xPos, yPos);
    }

    GameObject CreateNodeButton(DungeonNode node, Vector2 pos, bool isAccessible)
    {
        var go = new GameObject("Node_" + currentMap.nodes.IndexOf(node), typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(nodeContainer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(NODE_SIZE, NODE_SIZE);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = GetNodeColor(node.type, node.visited);

        string labelText = GetNodeLabel(node.type);
        var label = new GameObject("Label", typeof(RectTransform));
        label.layer = 5;
        label.transform.SetParent(go.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = node.visited ? Color.gray : Color.white;
        UIFonts.Apply(tmp);

        int nodeIndex = currentMap.nodes.IndexOf(node);
        var btn = go.AddComponent<Button>();
        btn.interactable = isAccessible && !node.visited;
        if (btn.interactable)
        {
            btn.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); OnNodeClicked(nodeIndex); });
        }

        if (node.visited)
        {
            var check = new GameObject("Check", typeof(RectTransform));
            check.layer = 5;
            check.transform.SetParent(go.transform, false);
            var crt = check.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1);
            crt.sizeDelta = new Vector2(14, 14);
            crt.anchoredPosition = new Vector2(2, 2);
            var ctmp = check.AddComponent<TextMeshProUGUI>();
            ctmp.text = "✓";
            ctmp.fontSize = 10;
            ctmp.alignment = TextAlignmentOptions.Center;
            ctmp.color = Color.green;
            UIFonts.Apply(ctmp);
        }

        return go;
    }

    GameObject CreateConnectionLine(Vector2 from, Vector2 to, bool fromVisited)
    {
        var go = new GameObject("Line", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(lineContainer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);

        Vector2 dir = to - from;
        float dist = dir.magnitude;
        rt.sizeDelta = new Vector2(dist, 3f);
        rt.anchoredPosition = from;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        var img = go.AddComponent<Image>();
        img.color = fromVisited ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : new Color(0.3f, 0.3f, 0.3f, 0.4f);
        img.raycastTarget = false;

        return go;
    }

    void OnNodeClicked(int nodeIndex)
    {
        if (currentMap == null || nodeIndex < 0 || nodeIndex >= currentMap.nodes.Count) return;

        var node = currentMap.nodes[nodeIndex];
        if (node.visited) return;

        var session = GameManager.Instance?.dungeonSession;
        if (session == null) return;

        node.visited = true;
        session.lastVisitedNode = node;

        switch (node.type)
        {
            case DungeonNodeType.Battle:
                GameManager.Instance.StartDungeonBattle(node.enemyTemplateId);
                break;
            case DungeonNodeType.Elite:
                GameManager.Instance.StartDungeonBattle(node.enemyTemplateId);
                break;
            case DungeonNodeType.Rest:
                HandleRestNode();
                break;
            case DungeonNodeType.Treasure:
                HandleTreasureNode();
                break;
            case DungeonNodeType.Boss:
                GameManager.Instance.StartBossBattle(node.enemyTemplateId);
                break;
            case DungeonNodeType.Exit:
                HandleExitNode();
                break;
        }

        RefreshMap();
    }

    void HandleRestNode()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (var unit in gm.playerTeamData)
        {
            int healAmount = Mathf.RoundToInt(unit.maxHp * 0.3f);
            unit.currentHP = Mathf.Min(unit.currentHP + healAmount, unit.maxHp);
            unit.stress = Mathf.Max(unit.stress - 20, 0);
        }

        ShowInfoPanel("休息", "全队恢复 30% HP\n压力 -20");
    }

    void HandleTreasureNode()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int goldAmount = DropTable.RollGold("treasure_chest");
        gm.gold += goldAmount;

        var drops = DropTable.RollDrops("treasure_chest");
        string itemNames = "";
        foreach (var stack in drops)
        {
            gm.AddItem(stack.itemId, stack.quantity);
            var def = ItemDatabase.Get(stack.itemId);
            string name = def != null ? def.itemName : stack.itemId;
            if (stack.quantity > 1) name += $" x{stack.quantity}";
            itemNames += name + " ";
        }

        ShowInfoPanel("宝箱", $"获得 {goldAmount} 金币\n{itemNames}");
    }

    void HandleExitNode()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var session = gm.dungeonSession;
        if (session == null) return;

        ShowFloorCompletePanel();
    }

    void ShowFloorCompletePanel()
    {
        var gm = GameManager.Instance;
        var session = gm?.dungeonSession;
        if (session == null) return;

        string msg = $"地牢第 {session.currentFloor} 层完成！\n金币: {gm.gold}\n\n继续进入下一层？";
        ShowConfirmPanel("楼层完成", msg, "继续下一层", "返回世界地图",
            () =>
            {
                session.NextFloor();
                currentMap = session.currentMap;
                RefreshMap();
            },
            () =>
            {
                session.ExitDungeon();
                Close();
                gm.currentState = GameState.WorldMap;
            }
        );
    }

    void ConfirmExitDungeon()
    {
        ShowConfirmPanel("退出地牢", "确定要退出地牢吗？\n（进度将保存）", "确认退出", "取消",
            () =>
            {
                var gm = GameManager.Instance;
                var session = gm?.dungeonSession;
                if (session != null) session.ExitDungeon();
                Close();
                if (gm != null) gm.currentState = GameState.WorldMap;
            },
            null
        );
    }

    // ========== Reusable UI Helpers ==========

    void ShowConfirmPanel(string title, string message, string confirmText, string cancelText,
        UnityEngine.Events.UnityAction onConfirm, UnityEngine.Events.UnityAction onCancel)
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var overlay = new GameObject("ConfirmOverlay", typeof(RectTransform));
        overlay.layer = 5;
        overlay.transform.SetParent(canvas.transform, false);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        var oimg = overlay.AddComponent<Image>();
        oimg.color = new Color(0, 0, 0, 0.6f);

        var popup = new GameObject("ConfirmPopup", typeof(RectTransform));
        popup.layer = 5;
        popup.transform.SetParent(overlay.transform, false);
        var prt = popup.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(350, 200);
        prt.anchoredPosition = Vector2.zero;
        var pimg = popup.AddComponent<Image>();
        pimg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(popup.transform, false);
        var trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1);
        trt.anchorMax = new Vector2(0.5f, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.sizeDelta = new Vector2(300, 30);
        trt.anchoredPosition = new Vector2(0, -12);
        var ttmp = titleObj.AddComponent<TextMeshProUGUI>();
        ttmp.text = title;
        ttmp.fontSize = 20;
        ttmp.alignment = TextAlignmentOptions.Center;
        ttmp.color = Color.yellow;
        UIFonts.Apply(ttmp);

        var msgObj = new GameObject("Message", typeof(RectTransform));
        msgObj.layer = 5;
        msgObj.transform.SetParent(popup.transform, false);
        var mrt = msgObj.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.5f, 0.5f);
        mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(300, 80);
        mrt.anchoredPosition = Vector2.zero;
        var mtmp = msgObj.AddComponent<TextMeshProUGUI>();
        mtmp.text = message;
        mtmp.fontSize = 16;
        mtmp.alignment = TextAlignmentOptions.Center;
        mtmp.color = Color.white;
        UIFonts.Apply(mtmp);

        var confirmBtn = CreateButtonObj(popup, "ConfirmBtn", new Color(0.3f, 0.5f, 0.3f));
        var crt = confirmBtn.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.3f, 0);
        crt.anchorMax = new Vector2(0.3f, 0);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(110, 32);
        crt.anchoredPosition = new Vector2(0, 20);
        var ctmp = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (ctmp != null) ctmp.text = confirmText;
        var cbtn = confirmBtn.GetComponent<Button>();
        cbtn.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); Destroy(overlay); onConfirm(); });

        if (onCancel != null)
        {
            var cancelBtn = CreateButtonObj(popup, "CancelBtn", new Color(0.3f, 0.2f, 0.2f));
            var crt2 = cancelBtn.GetComponent<RectTransform>();
            crt2.anchorMin = new Vector2(0.7f, 0);
            crt2.anchorMax = new Vector2(0.7f, 0);
            crt2.pivot = new Vector2(0.5f, 0.5f);
            crt2.sizeDelta = new Vector2(110, 32);
            crt2.anchoredPosition = new Vector2(0, 20);
            var ctmp2 = cancelBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (ctmp2 != null) ctmp2.text = cancelText;
            var cbtn2 = cancelBtn.GetComponent<Button>();
            cbtn2.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); Destroy(overlay); onCancel(); });
        }
        else
        {
            var okBtn = CreateButtonObj(popup, "OK", new Color(0.3f, 0.3f, 0.3f));
            var ortb = okBtn.GetComponent<RectTransform>();
            ortb.anchorMin = new Vector2(0.5f, 0);
            ortb.anchorMax = new Vector2(0.5f, 0);
            ortb.pivot = new Vector2(0.5f, 0.5f);
            ortb.sizeDelta = new Vector2(100, 32);
            ortb.anchoredPosition = new Vector2(0, 20);
            var otmp = okBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (otmp != null) otmp.text = cancelText;
            var obtn = okBtn.GetComponent<Button>();
            obtn.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); Destroy(overlay); });
        }
    }

    void ShowInfoPanel(string title, string message)
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var overlay = new GameObject("InfoOverlay", typeof(RectTransform));
        overlay.layer = 5;
        overlay.transform.SetParent(canvas.transform, false);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        var oimg = overlay.AddComponent<Image>();
        oimg.color = new Color(0, 0, 0, 0.6f);

        var popup = new GameObject("InfoPopup", typeof(RectTransform));
        popup.layer = 5;
        popup.transform.SetParent(overlay.transform, false);
        var prt = popup.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(350, 200);
        prt.anchoredPosition = Vector2.zero;
        var pimg = popup.AddComponent<Image>();
        pimg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(popup.transform, false);
        var trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1);
        trt.anchorMax = new Vector2(0.5f, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.sizeDelta = new Vector2(300, 30);
        trt.anchoredPosition = new Vector2(0, -12);
        var ttmp = titleObj.AddComponent<TextMeshProUGUI>();
        ttmp.text = title;
        ttmp.fontSize = 20;
        ttmp.alignment = TextAlignmentOptions.Center;
        ttmp.color = Color.yellow;
        UIFonts.Apply(ttmp);

        var msgObj = new GameObject("Message", typeof(RectTransform));
        msgObj.layer = 5;
        msgObj.transform.SetParent(popup.transform, false);
        var mrt = msgObj.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.5f, 0.5f);
        mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(300, 80);
        mrt.anchoredPosition = Vector2.zero;
        var mtmp = msgObj.AddComponent<TextMeshProUGUI>();
        mtmp.text = message;
        mtmp.fontSize = 16;
        mtmp.alignment = TextAlignmentOptions.Center;
        mtmp.color = Color.white;
        UIFonts.Apply(mtmp);

        var btnObj = CreateButtonObj(popup, "OK", new Color(0.3f, 0.3f, 0.3f));
        var brt = btnObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0);
        brt.anchorMax = new Vector2(0.5f, 0);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(100, 32);
        brt.anchoredPosition = new Vector2(0, 20);
        var btmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btmp != null) btmp.text = "确定";
        var btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); Destroy(overlay); });
    }

    GameObject CreateButtonObj(GameObject parent, string name, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
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
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = "Button";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        UIFonts.Apply(tmp);

        go.AddComponent<Button>();
        return go;
    }

    TextMeshProUGUI CreateLabel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 30);
        rt.anchoredPosition = anchoredPos;
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        UIFonts.Apply(tmp);
        return tmp;
    }

    GameObject CreateButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, string text, int fontSize, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100, 30);
        rt.anchoredPosition = anchoredPos;
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
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        UIFonts.Apply(tmp);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK); onClick(); });
        return go;
    }

    Color GetNodeColor(DungeonNodeType type, bool visited)
    {
        if (visited) return new Color(0.2f, 0.2f, 0.2f, 0.8f);
        switch (type)
        {
            case DungeonNodeType.Battle: return new Color(0.8f, 0.2f, 0.2f, 0.9f);
            case DungeonNodeType.Elite: return new Color(0.6f, 0.2f, 0.6f, 0.9f);
            case DungeonNodeType.Rest: return new Color(0.2f, 0.4f, 0.8f, 0.9f);
            case DungeonNodeType.Treasure: return new Color(0.8f, 0.7f, 0.1f, 0.9f);
            case DungeonNodeType.Boss: return new Color(0.9f, 0.6f, 0.1f, 0.9f);
            case DungeonNodeType.Exit: return new Color(0.3f, 0.8f, 0.3f, 0.9f);
            default: return Color.gray;
        }
    }

    string GetNodeLabel(DungeonNodeType type)
    {
        switch (type)
        {
            case DungeonNodeType.Battle: return "🔴";
            case DungeonNodeType.Elite: return "🟣";
            case DungeonNodeType.Rest: return "💧";
            case DungeonNodeType.Treasure: return "🟡";
            case DungeonNodeType.Boss: return "⭐";
            case DungeonNodeType.Exit: return "🚪";
            default: return "?";
        }
    }
}
