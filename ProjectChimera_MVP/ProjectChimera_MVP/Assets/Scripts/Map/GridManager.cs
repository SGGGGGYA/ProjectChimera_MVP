using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("网格设置")]
    public int gridWidth = 6;
    public int gridHeight = 6;
    public float cellSize = 1.5f;
    public float gridOriginX = -3.75f;
    public float gridOriginY = -3.75f;
    public Color normalColor = new Color(0.3f, 0.5f, 0.3f, 1f);
    public Color highlightColor = Color.yellow;
    public Color battleTileColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    [Header("小队")]
    public float moveSpeed = 3f;

    private GameObject[,] tiles;
    private GameObject squadMarker;
    private Vector2Int squadGridPos = new Vector2Int(0, 0);
    private List<Vector2Int> battleTiles = new List<Vector2Int>();
    private bool isMoving;
    private Coroutine moveRoutine;
    private bool battleTriggered;

    void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.WorldMap)
        {
            Debug.Log("[GridManager] 当前不是世界地图状态，销毁自身");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        MarkAsBattleTile(3, 2);
        MarkAsBattleTile(4, 3);
        MarkAsBattleTile(2, 4);
        GenerateGrid();

        // 恢复上次保存的地图位置
        if (GameManager.Instance != null)
            squadGridPos = GameManager.Instance.savedSquadPos;

        CreateSquad();
    }

    void OnDestroy()
    {
        if (tiles != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (tiles[x, y] != null)
                        Destroy(tiles[x, y]);
                }
            }
        }
        if (squadMarker != null)
            Destroy(squadMarker);
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
                sr.color = normalColor;
                sr.sortingOrder = 0;

                Vector2Int pos = new Vector2Int(x, y);
                if (battleTiles.Contains(pos))
                    sr.color = battleTileColor;

                BoxCollider2D col = tile.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * (cellSize * 0.85f);

                TileClickHandler clickHandler = tile.AddComponent<TileClickHandler>();
                clickHandler.Init(this, x, y);

                tiles[x, y] = tile;
            }
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

        Vector2Int target = new Vector2Int(x, y);

        for (int gx = 0; gx < gridWidth; gx++)
        {
            for (int gy = 0; gy < gridHeight; gy++)
            {
                SpriteRenderer sr = tiles[gx, gy].GetComponent<SpriteRenderer>();
                Vector2Int pos = new Vector2Int(gx, gy);
                sr.color = battleTiles.Contains(pos) ? battleTileColor : normalColor;
            }
        }

        tiles[x, y].GetComponent<SpriteRenderer>().color = highlightColor;

        if (target == squadGridPos) return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveSquad(target.x, target.y));
    }

    IEnumerator MoveSquad(int targetX, int targetY)
    {
        isMoving = true;
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

        if (battleTiles.Contains(squadGridPos) && !battleTriggered)
        {
            battleTriggered = true;
            OnSquadEnteredBattleTile();
        }
    }

    public void MarkAsBattleTile(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (!battleTiles.Contains(pos))
        {
            battleTiles.Add(pos);
            if (tiles != null && tiles[x, y] != null)
            {
                tiles[x, y].GetComponent<SpriteRenderer>().color = battleTileColor;
            }
        }
    }

    void OnSquadEnteredBattleTile()
    {
        Debug.Log("[GridManager] 进入战斗格！触发遭遇战...");
        if (GameManager.Instance != null)
        {
            // 保存地图位置，战斗回来后恢复
            GameManager.Instance.savedSquadPos = squadGridPos;
            Debug.Log($"[GridManager] 保存位置 ({squadGridPos.x},{squadGridPos.y})");
            GameManager.Instance.StartBattle();
        }
        else
        {
            Debug.LogError("[GridManager] GameManager.Instance 为 null！");
        }
    }

    Sprite CreateSquareSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, UnityEngine.TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / cellSize);
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, UnityEngine.TextureFormat.RGBA32, false);
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
        if (grid != null)
        {
            grid.OnTileClicked(tileX, tileY);
        }
    }
}
