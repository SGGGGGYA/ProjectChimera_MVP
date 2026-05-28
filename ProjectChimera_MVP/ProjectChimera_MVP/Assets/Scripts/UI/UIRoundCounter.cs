using TMPro;
using UnityEngine;

public class UIRoundCounter : MonoBehaviour
{
    private TextMeshProUGUI label;
    private int lastRound;

    void Start()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("BattleCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
            cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        UIFonts.EnsureEventSystem();

        var go = new GameObject("RoundCounter", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(300, 36);
        rt.anchoredPosition = new Vector2(0, -6);

        go.AddComponent<CanvasRenderer>();
        label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = 20;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        UIFonts.Apply(label);

        lastRound = -1;
        Refresh();
    }

    void Update()
    {
        if (TurnManager.Instance == null) return;
        if (TurnManager.Instance.roundCount != lastRound)
            Refresh();
    }

    void Refresh()
    {
        if (label == null || TurnManager.Instance == null) return;
        lastRound = TurnManager.Instance.roundCount;
        label.text = $"===== 第 {lastRound} 轮 =====";
    }
}
