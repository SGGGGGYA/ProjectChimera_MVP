using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 双层血条控制器
/// - 绿血（fillImage）：瞬间变化，立即反映当前血量
/// - 红血（trailImage）：缓慢追赶绿血，制造受伤尾迹效果
/// - 自动跟随挂载在角色头顶
/// </summary>
public class HPBarFollower : MonoBehaviour
{
    [Header("UI 引用 — 在编辑器中拖拽绑定")]
    public Image fillImage;       // 绿色 Fill（当前血量，瞬间变化）
    public Image trailImage;      // 红色 Fill_Trail（尾迹，缓慢追赶）

    [Header("动画参数")]
    public float lerpSpeed = 5f;  // 红血追赶绿血的速度，越大越快

    [Header("位置跟随")]
    public Vector3 offset = new Vector3(0, 1.5f, 0); // 角色头顶偏移

    private Transform target;         // 要跟随的角色 Transform

    // ==============================
    //  公共方法 — 供外部战斗管理器调用
    // ==============================

    /// <summary>绑定要跟随的角色</summary>
    public void SetTarget(Transform t)
    {
        target = t;
    }

    bool isDeathsDoor;
    float pulseTimer;

    public void SetDeathsDoor(bool active)
    {
        isDeathsDoor = active;
        if (!active && fillImage != null)
            fillImage.color = Color.green;
    }

    /// <summary>
    /// 更新血量（战斗管理器扣血/回血时调用）
    /// </summary>
    /// <param name="currentHP">当前血量</param>
    /// <param name="maxHP">最大血量</param>
    public void UpdateHP(int currentHP, int maxHP)
    {
        Log.Info($"[血条更新] {gameObject.name}, HP: {currentHP}/{maxHP}");

        // 计算血量百分比（强转 float，防止整型截断）
        float fillAmount = Mathf.Clamp01((float)currentHP / maxHP);
        Log.Info($"[血条更新] {gameObject.name}, fillAmount={fillAmount}");

        // 绿血：瞬间变化，零延迟
        if (fillImage != null)
            fillImage.fillAmount = fillAmount;
        else
            Log.Warn($"[血条更新] {gameObject.name} 的 fillImage 为 null！请在预制体中绑定");
    }

    // ==============================
    //  生命周期
    // ==============================

    void Update()
    {
        // 1. 跟随角色：将世界坐标转为屏幕坐标（因为血条在 Canvas 下）
        if (target != null)
        {
            transform.position = Camera.main.WorldToScreenPoint(target.position + offset);
        }

        // 2. 红血尾迹：平滑追赶绿血
        if (trailImage != null && fillImage != null)
        {
            // 如果红血明显大于绿血（说明刚受了伤），缓慢向左缩减
            if (trailImage.fillAmount > fillImage.fillAmount + 0.001f)
            {
                trailImage.fillAmount = Mathf.Lerp(
                    trailImage.fillAmount,
                    fillImage.fillAmount,
                    Time.deltaTime * lerpSpeed
                );
            }
            else
            {
                // 加血或已经追上：直接对齐，避免闪烁
                trailImage.fillAmount = fillImage.fillAmount;
            }
        }

        // 3. 死亡之门闪烁效果
        if (isDeathsDoor && fillImage != null)
        {
            pulseTimer += Time.deltaTime * 4f;
            float alpha = 0.3f + Mathf.Abs(Mathf.Sin(pulseTimer)) * 0.7f;
            fillImage.color = new Color(1f, 0.2f, 0.2f, alpha);
        }
    }
}
