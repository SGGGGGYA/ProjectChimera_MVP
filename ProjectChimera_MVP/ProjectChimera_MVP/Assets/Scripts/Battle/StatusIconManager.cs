using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 状态图标管理器 —— 挂在角色身上，在头顶显示 DD1 风格状态图标
/// 最多显示 4 个图标，超出显示 "+N"
/// </summary>
public class StatusIconManager : MonoBehaviour
{
    [Header("位置偏移")]
    public Vector3 worldOffset = new Vector3(0, 2.4f, 0); // 角色头顶世界坐标偏移

    [Header("图标尺寸")]
    public Vector2 iconSize = new Vector2(24, 24);
    public float iconSpacing = 2f;

    // 状态 → DD1 tray 图标文件名映射（不含 dd1_ 前缀）
    static readonly Dictionary<StatusType, string> StatusToTrayIcon = new Dictionary<StatusType, string>
    {
        { StatusType.Bleed,      "tray_bleed" },
        { StatusType.Stun,       "tray_stun" },
        { StatusType.Mark,       "tray_tag" },
        { StatusType.Taunt,      "tray_guard" },
        { StatusType.Protected,  "tray_guard" },
        { StatusType.Berserk,    "tray_berserk" },
        { StatusType.Enlightened,"tray_buff" },
    };

    private UnitData unit;
    private Transform unitTransform;
    private Canvas canvas;
    private RectTransform container;
    private List<Image> iconImages = new List<Image>();
    private TextMeshProUGUI overflowLabel;

    // 缓存上一次状态数量，用于脏检测
    private int lastStatusCount = -1;
    private List<StatusType> lastActiveTypes = new List<StatusType>();

    void Start()
    {
        unit = GetComponent<UnitData>();
        if (unit == null)
        {
            Log.Warn("[StatusIconManager] 找不到 UnitData，自动销毁");
            Destroy(this);
            return;
        }
        unitTransform = transform;

        EnsureCanvas();
        BuildContainer();
    }

    void Update()
    {
        if (unit == null || container == null) return;

        // 1. 跟随角色位置（WorldToScreenPoint）
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unitTransform.position + worldOffset);
            container.position = screenPos;
        }

        // 2. 脏检测：状态列表发生变化才重建图标
        var activeEffects = unit.statusEffects.FindAll(e => !e.expired);
        int activeCount = activeEffects.Count;

        bool dirty = activeCount != lastStatusCount;
        if (!dirty)
        {
            for (int i = 0; i < activeCount; i++)
            {
                if (i >= lastActiveTypes.Count || lastActiveTypes[i] != activeEffects[i].type)
                {
                    dirty = true;
                    break;
                }
            }
        }

        if (dirty)
        {
            RebuildIcons(activeEffects);
            lastStatusCount = activeCount;
            lastActiveTypes.Clear();
            foreach (var e in activeEffects)
                lastActiveTypes.Add(e.type);
        }
    }

    void EnsureCanvas()
    {
        if (canvas != null) return;

        // 优先查找场景中的 Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("StatusIconCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            Log.Info("[StatusIconManager] 自动创建了 Canvas");
        }
    }

    void BuildContainer()
    {
        GameObject go = new GameObject("StatusIconContainer", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);
        container = go.GetComponent<RectTransform>();
        container.sizeDelta = new Vector2(iconSize.x * 4 + iconSpacing * 3, iconSize.y);
        container.pivot = new Vector2(0.5f, 0.5f);
    }

    void RebuildIcons(List<StatusEffect> activeEffects)
    {
        // 清空旧图标
        foreach (var img in iconImages)
        {
            if (img != null && img.gameObject != null)
                Destroy(img.gameObject);
        }
        iconImages.Clear();

        if (overflowLabel != null)
        {
            Destroy(overflowLabel.gameObject);
            overflowLabel = null;
        }

        if (activeEffects.Count == 0)
        {
            container.gameObject.SetActive(false);
            return;
        }

        container.gameObject.SetActive(true);

        int showCount = Mathf.Min(activeEffects.Count, 4);
        for (int i = 0; i < showCount; i++)
        {
            Sprite sp = LoadStatusSprite(activeEffects[i].type);
            Image img = CreateIconImage(i, sp);
            iconImages.Add(img);
        }

        // 超出 4 个显示 "+N"
        if (activeEffects.Count > 4)
        {
            int overflow = activeEffects.Count - 4;
            overflowLabel = CreateOverflowLabel($"+{overflow}");
        }
    }

    Sprite LoadStatusSprite(StatusType type)
    {
        if (!StatusToTrayIcon.TryGetValue(type, out string trayName))
            return null;

        string path = $"UI/Icons/Status/dd1_{trayName}";
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp == null)
            Log.Warn($"[StatusIconManager] 加载失败: {path}，将显示灰色占位");
        return sp;
    }

    Image CreateIconImage(int index, Sprite sprite)
    {
        GameObject go = new GameObject($"Icon_{index}", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(container, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = iconSize;
        rt.anchoredPosition = new Vector2(index * (iconSize.x + iconSpacing), 0);

        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : Color.gray;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI CreateOverflowLabel(string text)
    {
        GameObject go = new GameObject("OverflowLabel", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(container, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(30, iconSize.y);
        rt.anchoredPosition = new Vector2(4 * (iconSize.x + iconSpacing), 0);

        go.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.yellow;
        tmp.raycastTarget = false;
        if (UIFonts.Chinese != null) tmp.font = UIFonts.Chinese;
        return tmp;
    }

    void OnDestroy()
    {
        if (container != null)
            Destroy(container.gameObject);
    }
}
