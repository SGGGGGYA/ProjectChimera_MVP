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
    Event
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
            AudioManager.Instance.PlayBGM(AudioKeys.BGM_MENU);

        GenerateTileTypes();
        GenerateGrid();

        if (GameManager.Instance != null)
            squadGridPos = GameManager.Instance.savedSquadPos;

        CreateSquad();
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
        var sr = tiles[x, y].GetComponent<SpriteRenderer>();
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

        for (int gx = 0; gx < gridWidth; gx++)
            for (int gy = 0; gy < gridHeight; gy++)
                UpdateTileColor(gx, gy);

        tiles[x, y].GetComponent<SpriteRenderer>().color = highlightColor;

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
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / cellSize);
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
