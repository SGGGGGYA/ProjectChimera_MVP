using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDebugConsole : MonoBehaviour
{
    public static UIDebugConsole Instance { get; private set; }

    private GameObject panel;
    private TextMeshProUGUI logText;
    private TMP_InputField filterInput;
    private GameObject contentRoot;
    private bool showDebug = true;
    private bool showInfo = true;
    private bool showWarn = true;
    private bool showError = true;
    private bool autoScroll = true;

    private readonly List<Log.Entry> filtered = new List<Log.Entry>();
    private readonly StringBuilder sb = new StringBuilder(4096);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (panel != null && panel.activeSelf) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (panel == null) BuildPanel();
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    void BuildPanel()
    {
        panel = new GameObject("DebugConsole", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        panel.layer = 5;
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        panel.transform.SetParent(transform, false);
        UIFonts.EnsureEventSystem();

        var bgImg = panel.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.85f);
        bgImg.raycastTarget = true;

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float y = -10;

        // title
        CreateLabel(panel, "Title", "DEBUG CONSOLE  [F12]", 22, Color.white, new Vector2(10, y), TextAlignmentOptions.Left);
        y -= 30;

        // filter row: level toggles + search input
        var filterRow = new GameObject("FilterRow", typeof(RectTransform));
        filterRow.layer = 5;
        filterRow.transform.SetParent(panel.transform, false);
        var frRt = filterRow.GetComponent<RectTransform>();
        frRt.anchorMin = new Vector2(0, 1);
        frRt.anchorMax = new Vector2(1, 1);
        frRt.pivot = new Vector2(0, 1);
        frRt.sizeDelta = new Vector2(0, 26);
        frRt.anchoredPosition = new Vector2(10, y);
        y -= 30;

        float fx = 0;
        MakeToggleButton(filterRow, ref fx, 0, "DBG", new Color(0.5f, 0.5f, 0.5f), () => { showDebug = !showDebug; Refresh(); });
        MakeToggleButton(filterRow, ref fx, 1, "INFO", new Color(0.3f, 0.6f, 1f), () => { showInfo = !showInfo; Refresh(); });
        MakeToggleButton(filterRow, ref fx, 2, "WARN", new Color(1f, 0.8f, 0.2f), () => { showWarn = !showWarn; Refresh(); });
        MakeToggleButton(filterRow, ref fx, 3, "ERR", new Color(1f, 0.3f, 0.3f), () => { showError = !showError; Refresh(); });

        // auto-scroll toggle
        MakeToggleButton(filterRow, ref fx, 4, "AUTO", new Color(0.5f, 0.8f, 0.5f), () => { autoScroll = !autoScroll; Refresh(); });

        // clear button
        float cx = frRt.sizeDelta.x - 70;
        var clearBtn = new GameObject("ClearBtn", typeof(RectTransform));
        clearBtn.layer = 5;
        clearBtn.transform.SetParent(filterRow.transform, false);
        var cRt = clearBtn.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(0, 1);
        cRt.pivot = new Vector2(0, 1);
        cRt.sizeDelta = new Vector2(60, 22);
        cRt.anchoredPosition = new Vector2(cx, 2);
        var cImg = clearBtn.AddComponent<Image>();
        cImg.color = new Color(0.4f, 0.2f, 0.2f);
        var cBtn = clearBtn.AddComponent<Button>();
        var cTmp = CreateLabel(clearBtn, "T", "清空", 14, Color.white, Vector2.zero, TextAlignmentOptions.Center);
        var cRt2 = cTmp.GetComponent<RectTransform>();
        cRt2.anchorMin = Vector2.zero; cRt2.anchorMax = Vector2.one;
        cRt2.offsetMin = Vector2.zero; cRt2.offsetMax = Vector2.zero;
        cBtn.onClick.AddListener(() => { Log.Clear(); Refresh(); });

        // search / filter text input
        var searchGo = new GameObject("SearchInput", typeof(RectTransform));
        searchGo.layer = 5;
        searchGo.transform.SetParent(panel.transform, false);
        var sRt = searchGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0, 1); sRt.anchorMax = new Vector2(1, 1);
        sRt.pivot = new Vector2(0, 1);
        sRt.sizeDelta = new Vector2(-20, 24);
        sRt.anchoredPosition = new Vector2(10, y);
        y -= 28;

        var sBg = searchGo.AddComponent<Image>();
        sBg.color = new Color(0.15f, 0.15f, 0.2f);

        filterInput = searchGo.AddComponent<TMP_InputField>();
        var sLabel = new GameObject("Label", typeof(RectTransform));
        sLabel.layer = 5;
        sLabel.transform.SetParent(searchGo.transform, false);
        var slRt = sLabel.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero; slRt.anchorMax = Vector2.one;
        slRt.offsetMin = new Vector2(4, 2); slRt.offsetMax = new Vector2(-4, -2);
        var sTmp = sLabel.AddComponent<TextMeshProUGUI>();
        sTmp.text = "";
        sTmp.fontSize = 14;
        sTmp.color = Color.white;
        sTmp.alignment = TextAlignmentOptions.Left;
        sTmp.raycastTarget = false;
        UIFonts.Apply(sTmp);
        filterInput.textComponent = sTmp;
        filterInput.text = "";
        filterInput.onValueChanged.AddListener(_ => Refresh());

        // placeholder
        var ph = new GameObject("Placeholder", typeof(RectTransform));
        ph.layer = 5;
        ph.transform.SetParent(searchGo.transform, false);
        var phRt = ph.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(4, 2); phRt.offsetMax = new Vector2(-4, -2);
        var phTmp = ph.AddComponent<TextMeshProUGUI>();
        phTmp.text = "过滤关键字...";
        phTmp.fontSize = 14;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f);
        phTmp.alignment = TextAlignmentOptions.Left;
        phTmp.raycastTarget = false;
        UIFonts.Apply(phTmp);
        filterInput.placeholder = phTmp;

        // scroll view for log entries
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
        scrollGo.layer = 5;
        scrollGo.transform.SetParent(panel.transform, false);
        var svRt = scrollGo.GetComponent<RectTransform>();
        svRt.anchorMin = new Vector2(0, 1);
        svRt.anchorMax = new Vector2(1, 1);
        svRt.pivot = new Vector2(0, 1);
        svRt.sizeDelta = new Vector2(-20, y - 10);
        svRt.anchoredPosition = new Vector2(10, y);

        var sv = scrollGo.AddComponent<ScrollRect>();
        sv.horizontal = false;
        sv.vertical = true;
        sv.elasticity = 0;
        sv.movementType = ScrollRect.MovementType.Clamped;
        sv.scrollSensitivity = 30;

        contentRoot = new GameObject("Content", typeof(RectTransform));
        contentRoot.layer = 5;
        contentRoot.transform.SetParent(scrollGo.transform, false);
        var ctRt = contentRoot.GetComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0, 1);
        ctRt.anchorMax = new Vector2(1, 1);
        ctRt.pivot = new Vector2(0, 1);
        ctRt.sizeDelta = new Vector2(0, 0);
        ctRt.anchoredPosition = Vector2.zero;

        logText = CreateLabel(contentRoot, "LogText", "", 13, Color.white, Vector2.zero, TextAlignmentOptions.TopLeft);
        var ltRt = logText.GetComponent<RectTransform>();
        ltRt.anchorMin = Vector2.zero; ltRt.anchorMax = Vector2.one;
        ltRt.offsetMin = new Vector2(4, 4);
        ltRt.offsetMax = new Vector2(-4, -4);

        sv.content = ctRt;
        sv.viewport = svRt;

        panel.SetActive(false);
    }

    void MakeToggleButton(GameObject parent, ref float x, int idx, string label, Color activeColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn{idx}", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(50, 22);
        rt.anchoredPosition = new Vector2(x, 2);
        x += 54;

        var img = go.AddComponent<Image>();
        img.color = activeColor * 0.6f;
        var btn = go.AddComponent<Button>();
        var tmp = CreateLabel(go, "T", label, 12, Color.white, Vector2.zero, TextAlignmentOptions.Center);
        var tmpRt = tmp.GetComponent<RectTransform>();
        tmpRt.anchorMin = Vector2.zero; tmpRt.anchorMax = Vector2.one;
        tmpRt.offsetMin = Vector2.zero; tmpRt.offsetMax = Vector2.zero;
        btn.onClick.AddListener(onClick);
    }

    TextMeshProUGUI CreateLabel(GameObject parent, string name, string text, int fontSize, Color color, Vector2 pos, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(0, fontSize + 4);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.richText = true;
        UIFonts.Apply(tmp);
        return tmp;
    }

    void Refresh()
    {
        string filter = filterInput != null ? filterInput.text.Trim().ToLower() : "";
        filtered.Clear();
        sb.Clear();

        var entries = Log.Entries;
        int maxShow = autoScroll ? 300 : entries.Count;

        for (int i = Mathf.Max(0, entries.Count - maxShow); i < entries.Count; i++)
        {
            var e = entries[i];
            if (!ShouldShow(e)) continue;
            if (!string.IsNullOrEmpty(filter))
            {
                if (!e.message.ToLower().Contains(filter) && !e.tag.ToLower().Contains(filter))
                    continue;
            }
            filtered.Add(e);
        }

        string levelColor;
        foreach (var e in filtered)
        {
            switch (e.level)
            {
                case Log.Level.Verbose: levelColor = "#888888"; break;
                case Log.Level.Warn: levelColor = "#ffcc44"; break;
                case Log.Level.Error: levelColor = "#ff4444"; break;
                default: levelColor = "#cccccc"; break;
            }
            sb.Append($"<color={levelColor}>[{e.timestamp}][{e.tag}] {e.message}</color>\n");
        }

        if (logText != null)
            logText.text = sb.ToString();

        // auto-resize content
        if (contentRoot != null)
        {
            int lineCount = filtered.Count;
            var ct = contentRoot.GetComponent<RectTransform>();
            ct.sizeDelta = new Vector2(0, lineCount * 18 + 8);
        }
    }

    bool ShouldShow(Log.Entry e)
    {
        switch (e.level)
        {
            case Log.Level.Verbose: return showDebug;
            case Log.Level.Info: return showInfo;
            case Log.Level.Warn: return showWarn;
            case Log.Level.Error: return showError;
            default: return true;
        }
    }
}
