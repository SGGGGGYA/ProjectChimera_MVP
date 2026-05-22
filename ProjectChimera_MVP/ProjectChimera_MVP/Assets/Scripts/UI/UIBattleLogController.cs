using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗日志 uGUI 控制器 — 挂在 ScrollRect 上
/// </summary>
public class UIBattleLogController : MonoBehaviour
{
    [Header("引用")]
    public ScrollRect scrollRect;           // 整个 ScrollRect
    public TextMeshProUGUI logText;         // Content 下的 TMP 文本
    public int maxLines = 20;

    void Awake()
    {
        // 订阅日志更新
        BattleLog.OnNewEntry += Refresh;
    }

    void OnDestroy()
    {
        BattleLog.OnNewEntry -= Refresh;
    }

    void Start()
    {
        Refresh(); // 初始显示
    }

    void Refresh()
    {
        if (logText == null || scrollRect == null) return;

        // 收集最新的 N 条
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

        // 自动滚到底部
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
