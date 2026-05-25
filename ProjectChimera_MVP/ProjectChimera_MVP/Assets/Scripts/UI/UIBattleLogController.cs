using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBattleLogController : MonoBehaviour
{
    [Header("引用")]
    public ScrollRect scrollRect;
    public TextMeshProUGUI logText;
    public int maxLines = 20;

    [Header("最小化")]
    public GameObject expandedArea;
    public Button toggleButton;
    public TextMeshProUGUI toggleLabel;

    bool isMinimized;

    void Awake()
    {
        BattleLog.OnNewEntry += Refresh;
    }

    void OnDestroy()
    {
        BattleLog.OnNewEntry -= Refresh;
    }

    void Start()
    {
        if (toggleButton == null)
            CreateToggleButton();

        toggleButton.onClick.AddListener(ToggleMinimize);

        if (expandedArea == null && scrollRect != null)
            expandedArea = scrollRect.gameObject;

        if (toggleLabel == null && toggleButton != null)
            toggleLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();

        Refresh();
    }

    void CreateToggleButton()
    {
        var go = new GameObject("MinimizeBtn", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(transform.parent ?? transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(30, 24);
        rt.anchoredPosition = new Vector2(-2, -2);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var label = new GameObject("Label", typeof(RectTransform));
        label.layer = 5;
        label.transform.SetParent(go.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        label.AddComponent<CanvasRenderer>();
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "—";
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        toggleButton = go.AddComponent<Button>();
        toggleLabel = tmp;
    }

    void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        if (expandedArea != null)
            expandedArea.SetActive(!isMinimized);
        if (toggleLabel != null)
            toggleLabel.text = isMinimized ? "日志" : "—";
    }

    void Refresh()
    {
        if (logText == null || scrollRect == null) return;

        var entries = BattleLog.Entries;
        int start = Mathf.Max(0, entries.Count - maxLines);
        string html = "";
        for (int i = start; i < entries.Count; i++)
        {
            string line = entries[i];
            string color = "white";
            if (line.Contains("击败") || line.Contains("胜利"))
                color = "yellow";
            else if (line.Contains("失败"))
                color = "red";
            else if (line.Contains("升级"))
                color = "green";
            else if (line.Contains("技能") || line.Contains("释放"))
                color = "cyan";
            html += $"<color={color}>{line}</color>\n";
        }
        logText.text = html;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
