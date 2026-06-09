using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主菜单 — uGUI 版，所有 UI 元素在场景中拖拽绑定
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("UI 引用")]
    public Button newGameButton;
    public Button continueButton;
    public Button debugBattleButton;     // 调试用：直接进战斗场景
    public GameObject continueButtonObj;  // 用来控制显隐
    public GameObject debugBattleButtonObj;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI versionText;

    void Start()
    {
        EnsureAudioManager();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioKeys.BGM_MENU);

        // 有存档才显示"继续游戏"按钮
        bool hasSave = SaveManager.HasSave();
        if (continueButtonObj != null)
            continueButtonObj.SetActive(hasSave);

        // 调试战斗按钮：仅当 BattleScene 在 Build Settings 中时才显示
        if (debugBattleButtonObj != null)
        {
            bool battleAvailable = Application.CanStreamedLevelBeLoaded("BattleScene");
            debugBattleButtonObj.SetActive(battleAvailable);
        }

        // 按钮事件绑定（防止 Inspector 漏配）
        if (newGameButton != null) { newGameButton.onClick.RemoveAllListeners(); newGameButton.onClick.AddListener(OnClickNewGame); }
        if (continueButton != null) { continueButton.onClick.RemoveAllListeners(); continueButton.onClick.AddListener(OnClickContinue); }
        if (debugBattleButton != null) { debugBattleButton.onClick.RemoveAllListeners(); debugBattleButton.onClick.AddListener(OnClickDebugBattle); }

#if UNITY_EDITOR
        // Editor 多场景 Play 模式卸掉非主菜单场景
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != "MainMenu")
                SceneManager.UnloadSceneAsync(s);
        }
#endif
    }

    public void OnClickNewGame()
    {
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
        Log.Info("[主菜单] play 按钮被点击了！正在尝试跳转...");
        SaveManager.DeleteSave();
        EnsureGameManager();
        SafeLoadScene("WorldMap");
    }

    public void OnClickContinue()
    {
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
        Log.Info("[主菜单] continue 按钮被点击了！正在尝试读档...");
        EnsureGameManager();
        if (GameManager.Instance.LoadGame())
            SafeLoadScene("WorldMap");
        else
            Log.Warn("[主菜单] 读档失败，强制进入新游戏");
    }

    /// <summary>调试按钮：跳过 WorldMap 直接进战斗场景（用于快速测试战斗/动画）</summary>
    public void OnClickDebugBattle()
    {
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
        Log.Info("[主菜单] 调试战斗按钮被点击");
        EnsureGameManager();
        GameManager.Instance.StartBattle();
    }

    /// <summary>安全加载场景：若目标不在 Build Settings 则回退到 MainMenu</summary>
    void SafeLoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            GameManager.LoadSceneClean(sceneName);
        }
        else
        {
            Log.Error($"[主菜单] 场景 '{sceneName}' 未在 Build Settings 中！请 File > Build Settings 添加。");
        }
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
        }
    }

    void EnsureAudioManager()
    {
        if (AudioManager.Instance == null)
        {
            GameObject amGO = new GameObject("AudioManager");
            amGO.AddComponent<AudioManager>();
        }
    }
}
