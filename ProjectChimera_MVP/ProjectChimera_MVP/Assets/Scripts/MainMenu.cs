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
        // 有存档才显示"继续游戏"按钮
        bool hasSave = SaveManager.HasSave();
        if (continueButtonObj != null)
            continueButtonObj.SetActive(hasSave);

        // 注：按钮已通过 Inspector 绑定，不在此重复 AddListener
    }

    public void OnClickNewGame()
    {
        Debug.Log("[主菜单] play 按钮被点击了！正在尝试跳转...");
        SaveManager.DeleteSave();
        EnsureGameManager();
        SceneManager.LoadScene("WorldMap");
    }

    public void OnClickContinue()
    {
        Debug.Log("[主菜单] continue 按钮被点击了！正在尝试读档...");
        EnsureGameManager();
        GameManager.Instance.LoadGame();
        SceneManager.LoadScene("WorldMap");
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
        }
    }
}
