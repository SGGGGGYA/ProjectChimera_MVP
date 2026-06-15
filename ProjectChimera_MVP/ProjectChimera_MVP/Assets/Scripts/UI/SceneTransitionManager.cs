using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏场景过渡管理器：黑色淡入淡出
/// 在菜单->地图、地图->地牢、地牢->战斗、战斗返回时触发
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("过渡遮罩")]
    public Image fadeImage;
    public float fadeInDuration = 0.4f;
    public float fadeOutDuration = 0.4f;

    bool _isTransitioning;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureFadeOverlay();
    }

    void EnsureFadeOverlay()
    {
        if (fadeImage != null) return;

        var canvasGo = new GameObject("TransitionCanvas", typeof(RectTransform));
        canvasGo.layer = 5;
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 确保在最上层

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        var go = new GameObject("FadeOverlay", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(canvasGo.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = go.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;
    }

    /// <summary>
    /// 执行带过渡的场景切换
    /// </summary>
    public void TransitionTo(System.Action onMidTransition)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(onMidTransition));
    }

    IEnumerator TransitionRoutine(System.Action onMidTransition)
    {
        _isTransitioning = true;

        // 禁用输入
        SetInputEnabled(false);

        // 淡入（黑屏）
        yield return StartCoroutine(FadeTo(1f, fadeInDuration));

        // 执行切换逻辑
        onMidTransition?.Invoke();

        // 等待一帧让场景加载
        yield return null;
        // 再等一帧，确保场景中所有 Awake/Start 跑完
        yield return null;

        // 淡出
        yield return StartCoroutine(FadeTo(0f, fadeOutDuration));

        // 强制归零（防御性兜底）
        ForceClearOverlay();

        // 恢复输入
        SetInputEnabled(true);
        _isTransitioning = false;
    }

    /// <summary>
    /// 强制把遮罩清成全透明，防止残留黑屏
    /// </summary>
    public void ForceClearOverlay()
    {
        if (fadeImage == null) return;
        var c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false;
    }

    void OnEnable()
    {
        // 监听场景加载，加载完成后兜底清一次遮罩
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 场景加载后延迟一帧再清，避免在加载中途被覆盖
        if (gameObject.activeInHierarchy)
            StartCoroutine(DelayClearOverlay());
    }

    IEnumerator DelayClearOverlay()
    {
        yield return null;
        ForceClearOverlay();
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = new Color(0, 0, 0, a);
            fadeImage.raycastTarget = targetAlpha > 0.01f;
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
        fadeImage.raycastTarget = targetAlpha > 0.01f;
    }

    void SetInputEnabled(bool enabled)
    {
        // 禁用/启用 EventSystem 的输入模块
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (inputModule != null)
                inputModule.enabled = enabled;
        }
    }

    /// <summary>
    /// 是否正在过渡中
    /// </summary>
    public bool IsTransitioning => _isTransitioning;
}
