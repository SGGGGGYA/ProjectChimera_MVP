using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;

public enum WorldTileType
{
    Normal,
    Battle,
    Elite,
    Treasure,
    Camp,
    Event,
    Water
}

public class GridManager : MonoBehaviour
{
    [Header("网格设置")]
    public int gridWidth = 6;
    public int gridHeight = 6;
    public float cellSize = 1.5f;
    public float gridOriginX = -3.75f;
    public float gridOriginY = -3.75f;

    [Header("颜色")]
    public Color normalColor = new Color(0.3f, 0.5f, 0.3f, 1f);
    public Color highlightColor = Color.yellow;
    public Color battleColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color eliteColor = new Color(0.6f, 0.1f, 0.5f, 1f);
    public Color treasureColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    public Color campColor = new Color(0.3f, 0.7f, 0.5f, 1f);
    public Color eventColor = new Color(0.2f, 0.6f, 0.8f, 1f);

    [Header("小队")]
    public float moveSpeed = 3f;

    [Header("事件比例")]
    [Range(0, 1)] public float battleChance = 0.20f;
    [Range(0, 1)] public float eliteChance = 0.08f;
    [Range(0, 1)] public float treasureChance = 0.10f;
    [Range(0, 1)] public float campChance = 0.05f;
    [Range(0, 1)] public float eventChance = 0.05f;

    private WorldTileType[,] tileTypes;
    private GameObject[,] tiles;
    private GameObject squadMarker;
    private Vector2Int squadGridPos = new Vector2Int(0, 0);
    private bool isMoving;
    private Coroutine moveRoutine;
    private bool eventTriggered;

    public static bool IsUIPanelOpen()
    {
        if (UIShopController.Instance != null && UIShopController.Instance.panel != null && UIShopController.Instance.panel.activeSelf) return true;
        if (UIInventoryController.Instance != null && UIInventoryController.Instance.panel != null && UIInventoryController.Instance.panel.activeSelf) return true;
        if (UIFormationController.Instance != null && UIFormationController.Instance.panel != null && UIFormationController.Instance.panel.activeSelf) return true;
        if (UIPauseMenu.Instance != null && UIPauseMenu.Instance.panel != null && UIPauseMenu.Instance.panel.activeSelf) return true;
        if (UIRecruitController.Instance != null && UIRecruitController.Instance.panel != null && UIRecruitController.Instance.panel.activeSelf) return true;
        if (UIDungeonMapController.Instance != null && UIDungeonMapController.Instance.panel != null && UIDungeonMapController.Instance.panel.activeSelf) return true;
        if (UICampController.Instance != null && UICampController.Instance.panel != null && UICampController.Instance.panel.activeSelf) return true;
        return false;
    }

    void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.WorldMap)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioKeys.BGM_MAP);

        GenerateTileTypes();
        GenerateGrid();

        if (GameManager.Instance != null)
            squadGridPos = GameManager.Instance.savedSquadPos;

        CreateSquad();
        EnsureHUD();
    }

    void Update()
    {
        // 按 E 键结束回合
        if (Input.GetKeyDown(KeyCode.E) && !IsUIPanelOpen())
        {
            if (GameManager.Instance != null && GameManager.Instance.isPlayerTurn)
            {
                GameManager.Instance.EndPlayerTurn();
                ShowFloatingText($"<color=#ffdd44>第 {GameManager.Instance.currentDay} 天结束</color>\n行动点已恢复", new Color(0.9f, 0.8f, 0.3f), 2.5f);
                RefreshHUD();
            }
        }

        // 每帧刷新 HUD
        RefreshHUD();
    }

    void OnDestroy()
    {
        if (tiles != null)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    if (tiles[x, y] != null) Destroy(tiles[x, y]);
        }
        if (squadMarker != null) Destroy(squadMarker);
    }

    void GenerateTileTypes()
    {
        tileTypes = new WorldTileType[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x == 0 && y == 0)
                {
                    tileTypes[x, y] = WorldTileType.Normal;
                    continue;
                }

                float roll = RandomProvider.Current.Value;
                float acc = 0f;
                acc += battleChance;
                if (roll < acc) { tileTypes[x, y] = WorldTileType.Battle; continue; }
                acc += eliteChance;
                if (roll < acc) { tileTypes[x, y] = WorldTileType.Elite; continue; }
                acc += treasureChance;
                if (roll < acc) { tileTypes[x, y] = WorldTileType.Treasure; continue; }
                acc += campChance;
                if (roll < acc) { tileTypes[x, y] = WorldTileType.Camp; continue; }
                acc += eventChance;
                if (roll < acc) { tileTypes[x, y] = WorldTileType.Event; continue; }
                tileTypes[x, y] = WorldTileType.Normal;
            }
        }

        int total = gridWidth * gridHeight;
        int eventCount = 0;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (tileTypes[x, y] != WorldTileType.Normal) eventCount++;
        Log.Info($"[GridManager] 地图生成: {total}格, {eventCount}个事件格");
    }

    void GenerateGrid()
    {
        tiles = new GameObject[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.position = GridToWorld(x, y, 0f);
                tile.transform.SetParent(transform);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();
                sr.sortingOrder = 0;

                BoxCollider2D col = tile.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * (cellSize * 0.85f);

                TileClickHandler clickHandler = tile.AddComponent<TileClickHandler>();
                clickHandler.Init(this, x, y);

                tiles[x, y] = tile;
                UpdateTileColor(x, y);
            }
        }
    }

    void UpdateTileColor(int x, int y)
    {
        if (tiles[x, y] == null) return;
        var sr = tiles[x, y].GetComponent<SpriteRenderer>();
        if (sr == null) return;
        switch (tileTypes[x, y])
        {
            case WorldTileType.Battle: sr.color = battleColor; break;
            case WorldTileType.Elite: sr.color = eliteColor; break;
            case WorldTileType.Treasure: sr.color = treasureColor; break;
            case WorldTileType.Camp: sr.color = campColor; break;
            case WorldTileType.Event: sr.color = eventColor; break;
            default: sr.color = normalColor; break;
        }
    }

    public Vector3 GridToWorld(int x, int y, float z)
    {
        return new Vector3(gridOriginX + x * cellSize, gridOriginY + y * cellSize, z);
    }

    void CreateSquad()
    {
        squadMarker = new GameObject("Squad");
        SpriteRenderer sr = squadMarker.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = Color.cyan;
        sr.sortingOrder = 10;
        squadMarker.transform.position = GridToWorld(squadGridPos.x, squadGridPos.y, -0.5f);
    }

    public void OnTileClicked(int x, int y)
    {
        if (isMoving) return;

        // 宏观天系统：检查行动点
        if (GameManager.Instance != null && !GameManager.Instance.HasActionPoints())
        {
            ShowFloatingText("<color=#ff6666>行动点不足！</color>\n请结束回合", new Color(0.8f, 0.3f, 0.3f), 2f);
            return;
        }

        // 检查是否有待处理的敌人袭击
        if (GameManager.Instance != null && GameManager.Instance.pendingEnemyAttack)
        {
            GameManager.Instance.ProcessPendingEnemyAttack();
            return;
        }

        for (int gx = 0; gx < gridWidth; gx++)
            for (int gy = 0; gy < gridHeight; gy++)
                UpdateTileColor(gx, gy);

        if (tiles[x, y] != null)
        {
            var hl = tiles[x, y].GetComponent<SpriteRenderer>();
            if (hl != null) hl.color = highlightColor;
        }

        if (new Vector2Int(x, y) == squadGridPos) return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveSquad(x, y));
    }

    IEnumerator MoveSquad(int targetX, int targetY)
    {
        isMoving = true;
        eventTriggered = false;
        Vector3 startPos = squadMarker.transform.position;
        Vector3 endPos = GridToWorld(targetX, targetY, -0.5f);
        float timer = 0f;
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            squadMarker.transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
            yield return null;
        }

        squadMarker.transform.position = endPos;
        squadGridPos = new Vector2Int(targetX, targetY);
        isMoving = false;

        // 宏观天系统：消耗行动点
        if (GameManager.Instance != null)
            GameManager.Instance.ConsumeActionPoint();

        if (!eventTriggered)
            HandleTileEntry(targetX, targetY);
    }

    void HandleTileEntry(int x, int y)
    {
        eventTriggered = true;
        var type = tileTypes[x, y];
        tileTypes[x, y] = WorldTileType.Normal;
        UpdateTileColor(x, y);

        switch (type)
        {
            case WorldTileType.Battle:
                StartBattle(0);
                break;
            case WorldTileType.Elite:
                int eliteId = RandomProvider.Current.Range(10, 16);
                StartBattle(eliteId);
                break;
            case WorldTileType.Treasure:
                ShowTreasurePopup();
                break;
            case WorldTileType.Camp:
                DoFreeCamp();
                break;
            case WorldTileType.Event:
                TriggerRandomEvent();
                break;
        }
    }

    void StartBattle(int templateId)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.savedSquadPos = squadGridPos;
        if (templateId >= 10)
            GameManager.Instance.StartDungeonBattle(templateId);
        else
            GameManager.Instance.StartBattle();
    }

    void ShowTreasurePopup()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int gold = DropTable.RollGold("treasure_chest");
        gm.gold += gold;

        var drops = DropTable.RollDrops("treasure_chest");
        string lootStr = "";
        if (drops != null)
        {
            foreach (var stack in drops)
            {
                gm.AddItem(stack.itemId, stack.quantity);
                var def = ItemDatabase.Get(stack.itemId);
                string name = def != null ? def.itemName : stack.itemId;
                lootStr += $"\n  {name} x{stack.quantity}";
            }
        }

        ShowFloatingText($"<color=yellow>发现宝藏！</color>\n金币 +{gold}{lootStr}", new Color(0.9f, 0.7f, 0.1f), 3f);
    }

    void DoFreeCamp()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.playerTeamData == null) return;

        foreach (var u in gm.playerTeamData)
        {
            int heal = Mathf.RoundToInt((u.maxHp - u.currentHP) * 0.5f);
            u.currentHP = Mathf.Min(u.currentHP + heal, u.maxHp);
            u.stress = Mathf.Max(u.stress - 30, 0);
        }
        ShowFloatingText("<color=#55cc88>发现营地！</color>\n队伍已休整", new Color(0.3f, 0.7f, 0.5f), 2.5f);
    }

    void TriggerRandomEvent()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float roll = RandomProvider.Current.Value;
        if (roll < 0.30f)
        {
            int stressUp = RandomProvider.Current.Range(5, 15);
            foreach (var u in gm.playerTeamData)
                u.stress = Mathf.Min(u.stress + stressUp, 200);
            ShowFloatingText($"<color=#ff6666>遭遇险境！</color>\n全队压力 +{stressUp}", new Color(0.8f, 0.3f, 0.3f), 2.5f);
        }
        else if (roll < 0.55f)
        {
            int goldFound = RandomProvider.Current.Range(10, 30);
            gm.gold += goldFound;
            ShowFloatingText($"<color=yellow>发现遗物！</color>\n金币 +{goldFound}", Color.yellow, 2.5f);
        }
        else if (roll < 0.75f)
        {
            int heal = RandomProvider.Current.Range(10, 25);
            var target = gm.playerTeamData.Find(u => u.currentHP < u.maxHp);
            if (target != null)
            {
                target.currentHP = Mathf.Min(target.currentHP + heal, target.maxHp);
                ShowFloatingText($"<color=#88ff88>找到草药！</color>\n{target.unitName} HP +{heal}", new Color(0.4f, 0.8f, 0.4f), 2.5f);
            }
            else
                ShowFloatingText("找到一些草药，但没人需要", new Color(0.6f, 0.6f, 0.6f), 2f);
        }
        else
        {
            int stressDown = RandomProvider.Current.Range(5, 15);
            foreach (var u in gm.playerTeamData)
                u.stress = Mathf.Max(u.stress - stressDown, 0);
            ShowFloatingText($"<color=#66ccff>心情愉悦！</color>\n全队压力 -{stressDown}", new Color(0.3f, 0.6f, 0.9f), 2.5f);
        }
    }

    // ==================== 宏观天 HUD ====================

    GameObject hudCanvas;
    TextMeshProUGUI dayText;
    TextMeshProUGUI actionPointsText;
    TextMeshProUGUI goldText;
    TextMeshProUGUI buildingText;
    TextMeshProUGUI researchText;

    void EnsureHUD()
    {
        UIFonts.EnsureEventSystem();

        // 创建 HUD Canvas
        var canvasGo = new GameObject("MacroTurnHUD", typeof(RectTransform));
        canvasGo.layer = 5;
        hudCanvas = canvasGo;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // 天数显示
        var dayGo = new GameObject("DayText", typeof(RectTransform));
        dayGo.layer = 5;
        dayGo.transform.SetParent(canvasGo.transform, false);
        var dayRt = dayGo.GetComponent<RectTransform>();
        dayRt.anchorMin = new Vector2(0, 1);
        dayRt.anchorMax = new Vector2(0, 1);
        dayRt.pivot = new Vector2(0, 1);
        dayRt.sizeDelta = new Vector2(200, 30);
        dayRt.anchoredPosition = new Vector2(10, -10);
        dayGo.AddComponent<CanvasRenderer>();
        dayText = dayGo.AddComponent<TextMeshProUGUI>();
        dayText.fontSize = 18;
        dayText.alignment = TextAlignmentOptions.Left;
        dayText.color = new Color(0.9f, 0.8f, 0.3f);
        UIFonts.Apply(dayText);

        // 行动点显示
        var apGo = new GameObject("ActionPointsText", typeof(RectTransform));
        apGo.layer = 5;
        apGo.transform.SetParent(canvasGo.transform, false);
        var apRt = apGo.GetComponent<RectTransform>();
        apRt.anchorMin = new Vector2(0, 1);
        apRt.anchorMax = new Vector2(0, 1);
        apRt.pivot = new Vector2(0, 1);
        apRt.sizeDelta = new Vector2(200, 25);
        apRt.anchoredPosition = new Vector2(10, -45);
        apGo.AddComponent<CanvasRenderer>();
        actionPointsText = apGo.AddComponent<TextMeshProUGUI>();
        actionPointsText.fontSize = 14;
        actionPointsText.alignment = TextAlignmentOptions.Left;
        actionPointsText.color = Color.white;
        UIFonts.Apply(actionPointsText);

        // 金币显示
        var goldGo = new GameObject("GoldText", typeof(RectTransform));
        goldGo.layer = 5;
        goldGo.transform.SetParent(canvasGo.transform, false);
        var goldRt = goldGo.GetComponent<RectTransform>();
        goldRt.anchorMin = new Vector2(0, 1);
        goldRt.anchorMax = new Vector2(0, 1);
        goldRt.pivot = new Vector2(0, 1);
        goldRt.sizeDelta = new Vector2(200, 25);
        goldRt.anchoredPosition = new Vector2(10, -75);
        goldGo.AddComponent<CanvasRenderer>();
        goldText = goldGo.AddComponent<TextMeshProUGUI>();
        goldText.fontSize = 14;
        goldText.alignment = TextAlignmentOptions.Left;
        goldText.color = Color.yellow;
        UIFonts.Apply(goldText);

        // 建筑显示
        var buildingGo = new GameObject("BuildingText", typeof(RectTransform));
        buildingGo.layer = 5;
        buildingGo.transform.SetParent(canvasGo.transform, false);
        var buildingRt = buildingGo.GetComponent<RectTransform>();
        buildingRt.anchorMin = new Vector2(0, 1);
        buildingRt.anchorMax = new Vector2(0, 1);
        buildingRt.pivot = new Vector2(0, 1);
        buildingRt.sizeDelta = new Vector2(200, 25);
        buildingRt.anchoredPosition = new Vector2(10, -105);
        buildingGo.AddComponent<CanvasRenderer>();
        buildingText = buildingGo.AddComponent<TextMeshProUGUI>();
        buildingText.fontSize = 12;
        buildingText.alignment = TextAlignmentOptions.Left;
        buildingText.color = new Color(0.7f, 0.9f, 0.7f);
        UIFonts.Apply(buildingText);

        // 科技显示
        var researchGo = new GameObject("ResearchText", typeof(RectTransform));
        researchGo.layer = 5;
        researchGo.transform.SetParent(canvasGo.transform, false);
        var researchRt = researchGo.GetComponent<RectTransform>();
        researchRt.anchorMin = new Vector2(0, 1);
        researchRt.anchorMax = new Vector2(0, 1);
        researchRt.pivot = new Vector2(0, 1);
        researchRt.sizeDelta = new Vector2(200, 25);
        researchRt.anchoredPosition = new Vector2(10, -130);
        researchGo.AddComponent<CanvasRenderer>();
        researchText = researchGo.AddComponent<TextMeshProUGUI>();
        researchText.fontSize = 12;
        researchText.alignment = TextAlignmentOptions.Left;
        researchText.color = new Color(0.7f, 0.7f, 0.9f);
        UIFonts.Apply(researchText);

        RefreshHUD();
    }

    void RefreshHUD()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        if (dayText != null)
            dayText.text = $"第 {gm.currentDay} 天";

        if (actionPointsText != null)
        {
            string apStr = $"行动点: {gm.currentActionPoints}/{gm.actionPoints}";
            if (gm.currentActionPoints <= 0)
                apStr += " (按 E 结束回合)";
            actionPointsText.text = apStr;
        }

        if (goldText != null)
            goldText.text = $"金币: {gm.gold}";

        if (buildingText != null)
            buildingText.text = $"建筑: {gm.builtBuildings.Count}座";

        if (researchText != null)
        {
            if (!string.IsNullOrEmpty(gm.currentResearch))
            {
                var def = GameManager.TechDefinitions.ContainsKey(gm.currentResearch) ? GameManager.TechDefinitions[gm.currentResearch] : null;
                string techName = def != null ? def.name : gm.currentResearch;
                int cost = def != null ? def.cost : 5;
                researchText.text = $"研究: {techName} ({gm.researchProgress}/{cost})";
            }
            else
            {
                researchText.text = "研究: 无 (按 T 选择)";
            }
        }
    }

    // ==================== 胜利/失败界面 ====================

    /// <summary>显示胜利界面</summary>
    public void ShowVictoryUI()
    {
        UIFonts.EnsureEventSystem();
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("VictoryPanel", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 背景
        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.05f, 0.95f);

        // 标题
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5;
        titleGo.transform.SetParent(go.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.7f);
        titleRt.anchorMax = new Vector2(0.5f, 0.7f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(600, 80);
        titleRt.anchoredPosition = Vector2.zero;
        titleGo.AddComponent<CanvasRenderer>();
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<color=#ffdd44><size=36>★ 游戏胜利 ★</size></color>";
        titleTmp.fontSize = 36;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        // 统计信息
        var statsGo = new GameObject("Stats", typeof(RectTransform));
        statsGo.layer = 5;
        statsGo.transform.SetParent(go.transform, false);
        var statsRt = statsGo.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.5f, 0.5f);
        statsRt.anchorMax = new Vector2(0.5f, 0.5f);
        statsRt.pivot = new Vector2(0.5f, 0.5f);
        statsRt.sizeDelta = new Vector2(400, 200);
        statsRt.anchoredPosition = Vector2.zero;
        statsGo.AddComponent<CanvasRenderer>();
        var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            var hexMap = FindObjectOfType<HexWorldMap>();
            int playerTiles = hexMap != null ? hexMap.GetFactionTileCount(TileFaction.Player) : 0;

            statsTmp.text = $"存活天数: {gm.currentDay}\n" +
                           $"占领地块: {playerTiles}\n" +
                           $"研究科技: {gm.researchedTechnologies.Count}\n" +
                           $"金币: {gm.gold}\n" +
                           $"建筑: {gm.builtBuildings.Count}座";
        }

        statsTmp.fontSize = 18;
        statsTmp.alignment = TextAlignmentOptions.Center;
        statsTmp.color = Color.white;
        UIFonts.Apply(statsTmp);

        // 重新开始按钮
        var btnGo = new GameObject("RestartBtn", typeof(RectTransform));
        btnGo.layer = 5;
        btnGo.transform.SetParent(go.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.3f);
        btnRt.anchorMax = new Vector2(0.5f, 0.3f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(200, 50);
        btnRt.anchoredPosition = Vector2.zero;
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.6f, 0.3f);
        var btnLbl = new GameObject("Label", typeof(RectTransform));
        btnLbl.layer = 5;
        btnLbl.transform.SetParent(btnGo.transform, false);
        var lblRt = btnLbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;
        btnLbl.AddComponent<CanvasRenderer>();
        var lblTmp = btnLbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = "重新开始";
        lblTmp.fontSize = 20;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.color = Color.white;
        UIFonts.Apply(lblTmp);

        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (gm != null) gm.ResetGame();
            Destroy(go);
        });
    }

    /// <summary>显示失败界面</summary>
    public void ShowDefeatUI()
    {
        UIFonts.EnsureEventSystem();
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("DefeatPanel", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 背景
        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.05f, 0.05f, 0.95f);

        // 标题
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5;
        titleGo.transform.SetParent(go.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.7f);
        titleRt.anchorMax = new Vector2(0.5f, 0.7f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(600, 80);
        titleRt.anchoredPosition = Vector2.zero;
        titleGo.AddComponent<CanvasRenderer>();
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<color=#ff4444><size=36>★ 游戏失败 ★</size></color>";
        titleTmp.fontSize = 36;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        // 统计信息
        var statsGo = new GameObject("Stats", typeof(RectTransform));
        statsGo.layer = 5;
        statsGo.transform.SetParent(go.transform, false);
        var statsRt = statsGo.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.5f, 0.5f);
        statsRt.anchorMax = new Vector2(0.5f, 0.5f);
        statsRt.pivot = new Vector2(0.5f, 0.5f);
        statsRt.sizeDelta = new Vector2(400, 200);
        statsRt.anchoredPosition = Vector2.zero;
        statsGo.AddComponent<CanvasRenderer>();
        var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            statsTmp.text = $"存活天数: {gm.currentDay}\n" +
                           $"金币: {gm.gold}\n" +
                           $"建筑: {gm.builtBuildings.Count}座\n" +
                           $"研究科技: {gm.researchedTechnologies.Count}";
        }

        statsTmp.fontSize = 18;
        statsTmp.alignment = TextAlignmentOptions.Center;
        statsTmp.color = Color.white;
        UIFonts.Apply(statsTmp);

        // 重新开始按钮
        var btnGo = new GameObject("RestartBtn", typeof(RectTransform));
        btnGo.layer = 5;
        btnGo.transform.SetParent(go.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.3f);
        btnRt.anchorMax = new Vector2(0.5f, 0.3f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(200, 50);
        btnRt.anchoredPosition = Vector2.zero;
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.6f, 0.3f, 0.3f);
        var btnLbl = new GameObject("Label", typeof(RectTransform));
        btnLbl.layer = 5;
        btnLbl.transform.SetParent(btnGo.transform, false);
        var lblRt = btnLbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;
        btnLbl.AddComponent<CanvasRenderer>();
        var lblTmp = btnLbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = "重新开始";
        lblTmp.fontSize = 20;
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.color = Color.white;
        UIFonts.Apply(lblTmp);

        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (gm != null) gm.ResetGame();
            Destroy(go);
        });
    }

    void ShowFloatingText(string text, Color color, float duration)
    {
        UIFonts.EnsureEventSystem();
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("WorldEventCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject("EventPopup", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 120);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        var tmpGo = new GameObject("Text", typeof(RectTransform));
        tmpGo.layer = 5;
        tmpGo.transform.SetParent(go.transform, false);
        var tmpRt = tmpGo.GetComponent<RectTransform>();
        tmpRt.anchorMin = Vector2.zero; tmpRt.anchorMax = Vector2.one;
        tmpRt.offsetMin = new Vector2(10, 10); tmpRt.offsetMax = new Vector2(-10, -10);
        tmpGo.AddComponent<CanvasRenderer>();
        var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        UIFonts.Apply(tmp);

        StartCoroutine(FadeAndDestroyPopup(go, duration));
    }

    IEnumerator FadeAndDestroyPopup(GameObject popup, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (popup != null)
        {
            var fadeImg = popup.GetComponentInChildren<Image>();
            var fadeTmp = popup.GetComponentInChildren<TextMeshProUGUI>();
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float a = 1 - t / 0.5f;
                if (fadeImg != null) fadeImg.color = new Color(fadeImg.color.r, fadeImg.color.g, fadeImg.color.b, a);
                if (fadeTmp != null) fadeTmp.color = new Color(fadeTmp.color.r, fadeTmp.color.g, fadeTmp.color.b, a);
                yield return null;
            }
            Destroy(popup);
        }
    }

    Sprite CreateSquareSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / Mathf.Max(cellSize, 0.001f));
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 0.7f);
    }
}

public class TileClickHandler : MonoBehaviour
{
    private GridManager grid;
    private int tileX;
    private int tileY;

    public void Init(GridManager manager, int x, int y)
    {
        grid = manager;
        tileX = x;
        tileY = y;
    }

    void OnMouseDown()
    {
        if (grid != null && !GridManager.IsUIPanelOpen())
            grid.OnTileClicked(tileX, tileY);
    }
}
