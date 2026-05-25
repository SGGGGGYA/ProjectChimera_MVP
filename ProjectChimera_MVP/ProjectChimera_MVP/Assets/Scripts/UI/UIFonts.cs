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
                _chinese = Resources.Load<TMP_FontAsset>("Unifont_Universal_SDF");
            return _chinese;
        }
    }

    public static TMP_FontAsset chineseFont
    {
        set { _chinese = value; }
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
