using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICampController : MonoBehaviour
{
    public static UICampController Instance { get; private set; }
    public GameObject panel;

    TextMeshProUGUI titleTmp;
    TextMeshProUGUI infoTmp;
    TextMeshProUGUI goldTmp;
    Button campBtn;
    TextMeshProUGUI btnTmp;
    List<UnitBattleData> playerData;

    /// <summary>篝火背景 Image 引用，用于闪烁动画</summary>
    Image campfireBgImage;
    /// <summary>篝火闪烁动画协程</summary>
    Coroutine campfireFlickerRoutine;

    const int CAMP_COST = 25;
    const int HP_RESTORE_PCT = 50;
    const int STRESS_REDUCE = 30;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        StopCampfireFlicker();
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (panel == null) EnsurePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel != null && panel.activeSelf) Close();
            else Open();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    void EnsurePanel()
    {
        if (panel != null) return;

        UIFonts.EnsureEventSystem();

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("CampCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        var root = new GameObject("CampPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 480);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // 营地背景图 campfire.png
        var campBg = new GameObject("CampBg", typeof(RectTransform));
        campBg.layer = 5;
        campBg.transform.SetParent(root.transform, false);
        var campRt = campBg.GetComponent<RectTransform>();
        campRt.anchorMin = Vector2.zero; campRt.anchorMax = Vector2.one;
        campRt.offsetMin = Vector2.zero; campRt.offsetMax = Vector2.zero;
        campfireBgImage = campBg.AddComponent<Image>();
        var campfireSprite = Resources.Load<Sprite>("Backgrounds/Camp/campfire");
        if (campfireSprite != null)
        {
            campfireBgImage.sprite = campfireSprite;
            campfireBgImage.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            campfireBgImage.type = Image.Type.Simple;
        }
        else
        {
            campfireBgImage.color = new Color(0.08f, 0.06f, 0.04f, 0.95f);
            Debug.LogWarning("[UICamp] 找不到 campfire.png，使用纯色背景回退");
        }

        // 半透明遮罩层，确保UI可读
        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.6f);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5;
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(360, 30);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleGo.AddComponent<CanvasRenderer>();
        titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "扎营休整";
        titleTmp.fontSize = 20;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.6f, 0.8f, 0.6f);
        UIFonts.Apply(titleTmp);

        var infoGo = new GameObject("Info", typeof(RectTransform));
        infoGo.layer = 5;
        infoGo.transform.SetParent(root.transform, false);
        var infoRt = infoGo.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 1);
        infoRt.anchorMax = new Vector2(1, 1);
        infoRt.pivot = new Vector2(0.5f, 1);
        infoRt.sizeDelta = new Vector2(360, 300);
        infoRt.anchoredPosition = new Vector2(0, -45);
        infoGo.AddComponent<CanvasRenderer>();
        infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
        infoTmp.fontSize = 13;
        infoTmp.alignment = TextAlignmentOptions.TopLeft;
        infoTmp.color = Color.white;
        UIFonts.Apply(infoTmp);

        var goldGo = new GameObject("GoldLabel", typeof(RectTransform));
        goldGo.layer = 5;
        goldGo.transform.SetParent(root.transform, false);
        var goldRt = goldGo.GetComponent<RectTransform>();
        goldRt.anchorMin = new Vector2(0, 0);
        goldRt.anchorMax = new Vector2(1, 0);
        goldRt.pivot = new Vector2(0.5f, 0);
        goldRt.sizeDelta = new Vector2(360, 24);
        goldRt.anchoredPosition = new Vector2(0, 80);
        goldGo.AddComponent<CanvasRenderer>();
        goldTmp = goldGo.AddComponent<TextMeshProUGUI>();
        goldTmp.fontSize = 14;
        goldTmp.alignment = TextAlignmentOptions.Center;
        goldTmp.color = Color.yellow;
        UIFonts.Apply(goldTmp);

        var btnGo = new GameObject("CampBtn", typeof(RectTransform));
        btnGo.layer = 5;
        btnGo.transform.SetParent(root.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0);
        btnRt.anchorMax = new Vector2(0.5f, 0);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(200, 44);
        btnRt.anchoredPosition = new Vector2(0, 30);
        btnGo.AddComponent<CanvasRenderer>();
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.5f, 0.25f);
        var btnLbl = new GameObject("Label", typeof(RectTransform));
        btnLbl.layer = 5;
        btnLbl.transform.SetParent(btnGo.transform, false);
        var lblRt = btnLbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        btnLbl.AddComponent<CanvasRenderer>();
        btnTmp = btnLbl.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "扎营 (25金币)";
        btnTmp.fontSize = 16;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        UIFonts.Apply(btnTmp);
        campBtn = btnGo.AddComponent<Button>();
        campBtn.onClick.AddListener(() =>
        {
            DoCamp();
            AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
        });

        var closeBtn = new GameObject("CloseBtn", typeof(RectTransform));
        closeBtn.layer = 5;
        closeBtn.transform.SetParent(root.transform, false);
        var cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(1, 1);
        cRt.sizeDelta = new Vector2(40, 40);
        cRt.anchoredPosition = new Vector2(-4, -4);
        closeBtn.AddComponent<CanvasRenderer>();
        var cImg = closeBtn.AddComponent<Image>();
        cImg.color = new Color(0.4f, 0.15f, 0.15f);
        var cLbl = new GameObject("X", typeof(RectTransform));
        cLbl.layer = 5;
        cLbl.transform.SetParent(closeBtn.transform, false);
        var cLblRt = cLbl.GetComponent<RectTransform>();
        cLblRt.anchorMin = Vector2.zero; cLblRt.anchorMax = Vector2.one;
        cLblRt.offsetMin = Vector2.zero; cLblRt.offsetMax = Vector2.zero;
        cLbl.AddComponent<CanvasRenderer>();
        var cTmp = cLbl.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.fontSize = 16;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;
        UIFonts.Apply(cTmp);
        closeBtn.AddComponent<Button>().onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLOSE); Close(); });

        panel = root;
        panel.SetActive(false);
    }

    public void Open()
    {
        EnsurePanel();
        var gm = GameManager.Instance;
        if (gm == null) return;
        playerData = gm.playerTeamData;
        Refresh();
        panel.SetActive(true);
        StartCampfireFlicker();
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_OPEN);
    }

    public void Close()
    {
        StopCampfireFlicker();
        if (panel != null) panel.SetActive(false);
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLOSE);
    }

    /// <summary>
    /// 启动篝火闪烁动画：模拟火光摇曳效果
    /// </summary>
    void StartCampfireFlicker()
    {
        StopCampfireFlicker();
        if (campfireBgImage != null && campfireBgImage.sprite != null)
        {
            campfireFlickerRoutine = StartCoroutine(CampfireFlickerRoutine());
        }
    }

    /// <summary>
    /// 停止篝火闪烁动画
    /// </summary>
    void StopCampfireFlicker()
    {
        if (campfireFlickerRoutine != null)
        {
            StopCoroutine(campfireFlickerRoutine);
            campfireFlickerRoutine = null;
        }
    }

    /// <summary>
    /// 篝火闪烁协程：随机微调亮度和饱和度，模拟火光摇曳
    /// </summary>
    IEnumerator CampfireFlickerRoutine()
    {
        float baseBrightness = 0.8f;
        float baseSaturation = 0.8f;
        float currentBrightness = baseBrightness;
        float currentSaturation = baseSaturation;
        float targetBrightness = baseBrightness;
        float targetSaturation = baseSaturation;

        while (campfireBgImage != null)
        {
            // 随机设置目标亮度和饱和度
            targetBrightness = baseBrightness + Random.Range(-0.12f, 0.12f);
            targetSaturation = baseSaturation + Random.Range(-0.08f, 0.08f);

            float elapsed = 0f;
            float flickerDuration = Random.Range(0.08f, 0.3f);
            float startBright = currentBrightness;
            float startSat = currentSaturation;

            while (elapsed < flickerDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flickerDuration;
                currentBrightness = Mathf.Lerp(startBright, targetBrightness, t);
                currentSaturation = Mathf.Lerp(startSat, targetSaturation, t);
                campfireBgImage.color = new Color(currentBrightness, currentSaturation * currentBrightness, (currentSaturation - 0.1f) * currentBrightness, 0.9f);
                yield return null;
            }

            // 偶尔的"爆燃"效果
            if (Random.value < 0.1f)
            {
                currentBrightness = baseBrightness + 0.15f;
                campfireBgImage.color = new Color(currentBrightness, currentBrightness * 0.9f, currentBrightness * 0.6f, 0.92f);
                yield return new WaitForSeconds(0.05f);
            }

            yield return null;
        }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null || playerData == null) return;

        string info = "";
        bool allFull = true;
        bool allCalm = true;
        foreach (var u in playerData)
        {
            int missingHP = u.maxHp - u.currentHP;
            if (missingHP > 0) allFull = false;
            if (u.stress > 0) allCalm = false;

            string hpStr = $"<color={(missingHP > 0 ? "#ff8888" : "#88ff88")}>{u.currentHP}/{u.maxHp}</color>";
            string stressStr = $"<color={(u.stress > 0 ? "#ffaa44" : "#88ff88")}>{u.stress}</color>";
            info += $"{u.unitName}  HP {hpStr}  压力 {stressStr}\n";
        }

        info += $"\n<color=#aaaaaa>扎营效果: 恢复{HP_RESTORE_PCT}%已损HP, 减压{STRESS_REDUCE}</color>";
        infoTmp.text = info;

        goldTmp.text = $"金币: <color=yellow>{gm.gold}</color>";

        bool canCamp = gm.gold >= CAMP_COST && !(allFull && allCalm);
        campBtn.interactable = canCamp;
        btnTmp.text = canCamp ? $"扎营 ({CAMP_COST}金币)" : (allFull && allCalm ? "无需休整" : "金币不足");
        btnTmp.color = canCamp ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    void DoCamp()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.gold < CAMP_COST) return;

        gm.gold -= CAMP_COST;
        foreach (var u in playerData)
        {
            int heal = Mathf.RoundToInt((u.maxHp - u.currentHP) * HP_RESTORE_PCT / 100f);
            u.currentHP = Mathf.Min(u.currentHP + heal, u.maxHp);
            u.stress = Mathf.Max(u.stress - STRESS_REDUCE, 0);
        }
        Refresh();
    }
}
