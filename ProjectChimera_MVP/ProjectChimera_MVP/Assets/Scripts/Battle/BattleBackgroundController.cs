using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗场景背景控制器：远景 + 中景（两侧石柱）+ 地面 三层结构，配合呼吸光效模拟暗黑地牢氛围。
/// 背景资源缺失时自动回退到纯色，不会导致空引用或崩溃。
/// </summary>
public class BattleBackgroundController : MonoBehaviour
{
    [Header("远景")]
    public Image farBgImage;
    public string farBgPath = "Backgrounds/Dungeon/ruins";
    /// <summary>远景暗色滤镜：压暗背景突出前景单位</summary>
    public Color farBgTint = new Color(0.65f, 0.65f, 0.7f, 1f);

    [Header("中景（两侧石柱装饰）")]
    public Image leftPillarImage;
    public Image rightPillarImage;
    public string pillarPath = "Backgrounds/Battle/side_decor";
    /// <summary>中景石柱色调</summary>
    public Color pillarTint = new Color(0.45f, 0.45f, 0.5f, 0.85f);

    [Header("地面")]
    public Image groundImage;
    /// <summary>地面高度（像素）</summary>
    public float groundHeight = 150f;

    [Header("呼吸光效")]
    public float breatheSpeed = 1.2f;
    public float breatheMinAlpha = 0.82f;
    public float breatheMaxAlpha = 1.0f;

    Canvas _canvas;

    void Awake()
    {
        EnsureCanvas();
        EnsureFarBackground();
        EnsureMidground();
        EnsureGround();
    }

    void Start()
    {
        StartCoroutine(BreatheEffect());
    }

    void EnsureCanvas()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            var cgo = new GameObject("BattleCanvas", typeof(RectTransform));
            cgo.layer = 5;
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
    }

    void EnsureFarBackground()
    {
        if (farBgImage != null) return;

        var go = new GameObject("Battle_BG_Far", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsFirstSibling(); // 放到 Canvas 第一个，确保在最底层

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        farBgImage = go.AddComponent<Image>();
        farBgImage.raycastTarget = false; // 不阻挡 UI 输入
        farBgImage.preserveAspect = false;

        var sprite = Resources.Load<Sprite>(farBgPath);
        if (sprite != null)
        {
            farBgImage.sprite = sprite;
            farBgImage.color = farBgTint;
        }
        else
        {
            // 尝试回退到 Dungeon/ruins
            sprite = Resources.Load<Sprite>("Backgrounds/Dungeon/ruins");
            if (sprite != null)
            {
                farBgImage.sprite = sprite;
                farBgImage.color = new Color(0.55f, 0.55f, 0.6f, 1f);
                Debug.LogWarning($"[BattleBackground] 找不到远景: {farBgPath}，回退到 Dungeon/ruins");
            }
            else
            {
                farBgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                Debug.LogWarning($"[BattleBackground] 所有远景资源缺失，使用纯色回退");
            }
        }
    }

    void EnsureMidground()
    {
        if (leftPillarImage == null)
        {
            var go = new GameObject("Battle_Pillar_Left", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(180, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(180, 0);

            leftPillarImage = go.AddComponent<Image>();
            leftPillarImage.raycastTarget = false;
            leftPillarImage.preserveAspect = true;
            leftPillarImage.sprite = SafeLoadSprite(pillarPath);
            leftPillarImage.color = pillarTint;
        }

        if (rightPillarImage == null)
        {
            var go = new GameObject("Battle_Pillar_Right", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(180, 0);
            rt.offsetMin = new Vector2(-180, 0);
            rt.offsetMax = new Vector2(0, 0);

            rightPillarImage = go.AddComponent<Image>();
            rightPillarImage.raycastTarget = false;
            rightPillarImage.preserveAspect = true;
            rightPillarImage.sprite = SafeLoadSprite(pillarPath);
            rightPillarImage.color = pillarTint;
        }
    }

    void EnsureGround()
    {
        if (groundImage != null) return;

        var go = new GameObject("Battle_Ground", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(_canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, groundHeight);
        rt.anchoredPosition = Vector2.zero;

        groundImage = go.AddComponent<Image>();
        groundImage.raycastTarget = false;
        groundImage.sprite = CreateGroundGradientSprite();
        groundImage.color = Color.white;
    }

    /// <summary>
    /// 创建地面渐变 Sprite：从深色到透明，模拟远处地面消失在黑暗中
    /// </summary>
    Sprite CreateGroundGradientSprite()
    {
        int w = 256, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            // 使用非线性曲线让渐变更自然：底部最快变暗
            float fade = Mathf.Pow(t, 1.5f);
            Color c = Color.Lerp(
                new Color(0.04f, 0.04f, 0.06f, 1f),     // 底部：深暗色地面
                new Color(0.01f, 0.01f, 0.02f, 0f),      // 顶部：完全透明融入黑暗
                fade
            );
            for (int x = 0; x < w; x++)
            {
                pixels[y * w + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
    }

    /// <summary>
    /// 呼吸光效：远景透明度在 min/max 之间缓慢波动，营造烛光摇曳感
    /// </summary>
    IEnumerator BreatheEffect()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * breatheSpeed, 1f);
            float alpha = Mathf.Lerp(breatheMinAlpha, breatheMaxAlpha, t);

            if (farBgImage != null && farBgImage.sprite != null)
            {
                Color c = farBgImage.color;
                c.a = alpha;
                farBgImage.color = c;
            }

            // 中景也在小范围内微微呼吸
            if (leftPillarImage != null && leftPillarImage.sprite != null)
            {
                Color c = leftPillarImage.color;
                c.a = Mathf.Lerp(0.75f, 0.9f, t);
                leftPillarImage.color = c;
            }
            if (rightPillarImage != null && rightPillarImage.sprite != null)
            {
                Color c = rightPillarImage.color;
                c.a = Mathf.Lerp(0.75f, 0.9f, t);
                rightPillarImage.color = c;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 安全加载 Sprite，资源缺失时返回纯色回退贴图
    /// </summary>
    Sprite SafeLoadSprite(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
        {
            Debug.LogWarning($"[BattleBackground] 找不到 Sprite: {path}，使用纯色回退");
            return CreateSolidSprite(new Color(0.2f, 0.2f, 0.25f, 1f));
        }
        return sp;
    }

    /// <summary>
    /// 创建纯色回退贴图
    /// </summary>
    Sprite CreateSolidSprite(Color color)
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }
}
