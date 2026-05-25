using TMPro;
using UnityEngine;

public static class UIFonts
{
    public static TMP_FontAsset chineseFont;

    public static void Apply(TextMeshProUGUI tmp)
    {
        if (tmp != null && chineseFont != null)
            tmp.font = chineseFont;
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
