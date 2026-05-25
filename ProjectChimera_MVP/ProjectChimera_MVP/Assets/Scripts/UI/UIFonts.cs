using TMPro;
using UnityEngine;

public static class UIFonts
{
    static TMP_FontAsset _chinese;

    public static TMP_FontAsset Chinese
    {
        get
        {
            if (_chinese == null)
                Load();
            return _chinese;
        }
    }

    static void Load()
    {
        _chinese = Resources.Load<TMP_FontAsset>("Unifont_Universal_SDF");
        if (_chinese != null)
        {
            Debug.Log("[UIFonts] 已加载 SDF 字体: Unifont_Universal_SDF");
            return;
        }

        var font = Resources.Load<Font>("unifont-17.0.04");
        if (font != null)
        {
            _chinese = TMP_FontAsset.CreateFontAsset(font, 14, 2, AtlasPopulationMode.Dynamic, true);
            Debug.Log("[UIFonts] 运行时创建动态字体: unifont-17.0.04");
        }
        else
        {
            Debug.LogWarning("[UIFonts] 未找到中文字体文件！");
        }
    }

    public static void Apply(TextMeshProUGUI tmp)
    {
        if (tmp != null && Chinese != null)
            tmp.font = Chinese;
    }

    public static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        Apply(tmp);
        return tmp;
    }
}

    public static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        Apply(tmp);
        return tmp;
    }
}
