using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPauseMenu : MonoBehaviour
{
    public GameObject panel;
    private GameObject settingsPanel;

    public static UIPauseMenu Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (panel == null) EnsurePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel != null && panel.activeSelf) Close();
            else if (!IsOtherPanelOpen()) Open();
        }
    }

    bool IsOtherPanelOpen()
    {
        if (UIInventoryController.Instance != null &&
            UIInventoryController.Instance.panel != null &&
            UIInventoryController.Instance.panel.activeSelf) return true;
        if (UIFormationController.Instance != null &&
            UIFormationController.Instance.panel != null &&
            UIFormationController.Instance.panel.activeSelf) return true;
        if (UIRecruitController.Instance != null &&
            UIRecruitController.Instance.panel != null &&
            UIRecruitController.Instance.panel.activeSelf) return true;
        return false;
    }

    void EnsurePanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("PauseCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var root = new GameObject("PauseMenu", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 400);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        var title = new GameObject("Title", typeof(RectTransform));
        title.layer = 5;
        title.transform.SetParent(root.transform, false);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(200, 30);
        titleRt.anchoredPosition = new Vector2(0, -12);
        title.AddComponent<CanvasRenderer>();
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "系统菜单";
        titleTmp.fontSize = 20;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        float y = -55;
        MakeTextButton(root, "SaveBtn", y, "💾 保存游戏", new Color(0.2f, 0.35f, 0.2f), () => OnSave());
        y -= 46;
        MakeTextButton(root, "LoadBtn", y, "📂 读取存档", new Color(0.25f, 0.25f, 0.3f), () => OnLoad());
        y -= 46;
        MakeTextButton(root, "SettingsBtn", y, "⚙️ 设置", new Color(0.3f, 0.3f, 0.2f), () => ToggleSettings());
        y -= 46;
        MakeTextButton(root, "RecruitBtn", y, "🏪 招募", new Color(0.25f, 0.3f, 0.35f), () => { Close(); OpenRecruit(); });
        y -= 46;
        MakeTextButton(root, "MenuBtn", y, "🏠 返回主菜单", new Color(0.4f, 0.2f, 0.2f), () => OnReturnToMenu());
        y -= 46;
        MakeTextButton(root, "CloseBtn", y, "✕ 关闭", new Color(0.25f, 0.25f, 0.25f), Close);

        // 设置面板 (内嵌，初始隐藏)
        var sp = new GameObject("SettingsPanel", typeof(RectTransform));
        sp.layer = 5;
        sp.transform.SetParent(root.transform, false);
        var spRt = sp.GetComponent<RectTransform>();
        spRt.anchorMin = new Vector2(0, 0);
        spRt.anchorMax = new Vector2(1, 0.5f);
        spRt.offsetMin = new Vector2(10, 10);
        spRt.offsetMax = new Vector2(-10, -5);
        var spImg = sp.AddComponent<Image>();
        spImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        settingsPanel = sp;

        var bgmLabel = MakeLabel(sp, "BGM_Label", new Vector2(20, -15), "BGM 音量", 14, Color.white);
        var bgmSlider = MakeSlider(sp, "BGM_Slider", new Vector2(20, -38), AudioManager.Instance.bgmVolume, v => {
            if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(v);
        });
        var sfxLabel = MakeLabel(sp, "SFX_Label", new Vector2(20, -68), "SFX 音量", 14, Color.white);
        var sfxSlider = MakeSlider(sp, "SFX_Slider", new Vector2(20, -91), AudioManager.Instance.sfxVolume, v => {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
        });

        settingsPanel.SetActive(false);
        panel.SetActive(false);
    }

    void MakeTextButton(GameObject parent, string name, float y, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(220, 36);
        rt.anchoredPosition = new Vector2(0, y);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = color;

        var lbl = new GameObject("Label", typeof(RectTransform));
        lbl.layer = 5;
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        UIFonts.Apply(tmp);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
    }

    TextMeshProUGUI MakeLabel(GameObject parent, string name, Vector2 pos, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(0, 20);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color;
        UIFonts.Apply(tmp);
        return tmp;
    }

    Slider MakeSlider(GameObject parent, string name, Vector2 pos, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(-40, 16);
        rt.anchoredPosition = pos;

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1;
        slider.value = value;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(8, 0); faRt.offsetMax = new Vector2(-8, 0);

        var fillRect = new GameObject("Fill", typeof(RectTransform));
        fillRect.transform.SetParent(fillArea.transform, false);
        var frRt = fillRect.GetComponent<RectTransform>();
        frRt.anchorMin = Vector2.zero; frRt.anchorMax = Vector2.one;
        frRt.offsetMin = Vector2.zero; frRt.offsetMax = Vector2.zero;
        fillRect.AddComponent<CanvasRenderer>();
        var fillImg = fillRect.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 0.3f);
        slider.fillRect = frRt;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8, 0); haRt.offsetMax = new Vector2(-8, 0);

        var handleRect = new GameObject("Handle", typeof(RectTransform));
        handleRect.transform.SetParent(handleArea.transform, false);
        var hRt = handleRect.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(16, 16);
        handleRect.AddComponent<CanvasRenderer>();
        var hImg = handleRect.AddComponent<Image>();
        hImg.color = Color.white;
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;

        slider.onValueChanged.AddListener(onChange);
        return slider;
    }

    void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    void OnSave()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.SaveGame();
        BattleLog.Add("游戏已保存");
    }

    void OnLoad()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.LoadGame())
        {
            Close();
            SceneManager.LoadScene("WorldMap");
        }
    }

    void OnReturnToMenu()
    {
        Close();
        SceneManager.LoadScene("MainMenu");
    }

    void OpenRecruit()
    {
        var rc = FindObjectOfType<UIRecruitController>();
        if (rc != null) rc.Open();
    }

    public void Open()
    {
        if (panel == null) EnsurePanel();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
