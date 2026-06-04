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
    public GameObject continueButtonObj;  // 用来控制显隐
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
        GameManager.LoadSceneClean("WorldMap");
    }

    public void OnClickContinue()
    {
        AudioManager.Instance?.PlaySFX(AudioKeys.SFX_UI_CLICK);
        Log.Info("[主菜单] continue 按钮被点击了！正在尝试读档...");
        EnsureGameManager();
        GameManager.Instance.LoadGame();
        GameManager.LoadSceneClean("WorldMap");
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
