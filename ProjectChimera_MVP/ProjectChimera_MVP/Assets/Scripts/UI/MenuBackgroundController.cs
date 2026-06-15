using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单背景控制器：视差背景 + 暗角效果
/// </summary>
public class MenuBackgroundController : MonoBehaviour
{
    [Header("背景图片")]
    public Image bgImage;      // title_bg.png 全屏
    public Image houseImage;   // title_house.png 底部居中

    [Header("视差系数")]
    public float bgParallaxFactor = 0.05f;
    public float houseParallaxFactor = 0.1f;

    [Header("暗角")]
    public Image vignetteImage;
    public Color vignetteColor = new Color(0, 0, 0, 0.3f);

    Vector2 _screenCenter;
    RectTransform _bgRect;
    RectTransform _houseRect;

    void Awake()
    {
        EnsureBackground();
        EnsureVignette();
    }

    void Start()
    {
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (bgImage != null) _bgRect = bgImage.GetComponent<RectTransform>();
        if (houseImage != null) _houseRect = houseImage.GetComponent<RectTransform>();
    }

    void Update()
    {
        ApplyParallax();
    }

    void ApplyParallax()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 offset = mousePos - _screenCenter;

        if (_bgRect != null)
        {
            _bgRect.anchoredPosition = offset * bgParallaxFactor;
        }
        if (_houseRect != null)
        {
            _houseRect.anchoredPosition = offset * houseParallaxFactor;
        }
    }

    void EnsureBackground()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("MenuCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        // BG 全屏背景（最底层）
        if (bgImage == null)
        {
            var bgGo = new GameObject("BG_Image", typeof(RectTransform));
            bgGo.layer = 5;
            bgGo.transform.SetParent(canvas.transform, false);
            bgGo.transform.SetAsFirstSibling();

            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            bgImage = bgGo.AddComponent<Image>();
            bgImage.sprite = SafeLoadSprite("Backgrounds/Menu/title_bg");
            bgImage.color = Color.white;
        }

        // House 底部居中
        if (houseImage == null)
        {
            var houseGo = new GameObject("House_Image", typeof(RectTransform));
            houseGo.layer = 5;
            houseGo.transform.SetParent(canvas.transform, false);

            var rt = houseGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(1200, 600);
            rt.anchoredPosition = new Vector2(0, -50);

            houseImage = houseGo.AddComponent<Image>();
            houseImage.sprite = SafeLoadSprite("Backgrounds/Menu/title_house");
            houseImage.color = Color.white;
        }
    }

    void EnsureVignette()
    {
        if (vignetteImage != null) return;

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var vigGo = new GameObject("Vignette", typeof(RectTransform));
        vigGo.layer = 5;
        vigGo.transform.SetParent(canvas.transform, false);
        vigGo.transform.SetAsLastSibling();

        var rt = vigGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        vignetteImage = vigGo.AddComponent<Image>();
        vignetteImage.sprite = CreateRadialGradientSprite();
        vignetteImage.color = vignetteColor;
        vignetteImage.raycastTarget = false;
    }

    Sprite CreateRadialGradientSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                // 径向渐变：中心透明，边缘黑色
                float alpha = t * t;
                pixels[y * size + x] = new Color(0, 0, 0, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite SafeLoadSprite(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
        {
            Debug.LogWarning($"[MenuBackground] 找不到 Sprite: {path}，使用纯黑回退");
            return CreateSolidSprite(Color.black);
        }
        return sp;
    }

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
