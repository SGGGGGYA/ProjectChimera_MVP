using UnityEngine;
using ProjectChimera.Core;

/// <summary>
/// 点击监听器 — 挂在单位预制体根节点上
/// 支持合法性判断 + 悬停预选高亮
///
/// 解耦（与 TurnManager 走相同模式）：
///   - 不再直接持有 BattleManager 引用
///   - 通过 IBattleContext 接口调用 targeting 相关能力
///   - 通过 <see cref="BattleContextProvider"/> 解析当前激活的上下文
///     （Provider 内部已封装了 FindObjectOfType 的兜底，本组件对 BattleManager 类零依赖）
///   - 任何模块只要实现 IBattleContext 即可复用本组件（便于以后做编辑模式/回放）
/// </summary>
public class UnitClickDetector : MonoBehaviour
{
    public static event System.Action<UnitData> OnUnitClicked;

    private UnitData unitData;
    private IBattleContext battleContext;
    private Collider2D ownCollider;
    private float lastClickTime = -1f;  // 防止 OnMouseDown 和 Update 重复触发

    void Awake()
    {
        unitData = GetComponent<UnitData>();
        if (unitData == null)
            Log.Warn($"[UnitClickDetector] {gameObject.name} 上找不到 UnitData 组件");
        ownCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        EnsureContext();
    }

    void OnEnable()
    {
        // 重新查找 BattleManager（按接口查找），处理战斗切换/重开场景
        EnsureContext();
    }

    void EnsureContext()
    {
        if (battleContext != null && !(battleContext is NullBattleContext)) return;
        // BattleSetup 在装配 BattleManager 时已注入 Provider；这里只做兜底查找。
        // Provider 内部已封装 NullBattleContext 保护，本组件不再写 FindObjectOfType<BattleManager>。
        battleContext = BattleContextProvider.Current;
    }

    void OnMouseDown()
    {
        if (GridManager.IsUIPanelOpen()) return;
        // 有 Collider2D 时优先用 OnMouseDown（性能更好），不重复走 Update 路径
        if (ownCollider == null) return;
        FireClickEvent();
    }

    void Update()
    {
        // 没 Collider2D 的单位靠 Update 里的射线检测拾取
        if (ownCollider != null) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (GridManager.IsUIPanelOpen()) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);
        if (hit != null && hit.gameObject == gameObject)
            FireClickEvent();
    }

    // ==================== 悬停高亮 ====================

    void OnMouseEnter()
    {
        if (unitData == null) return;

        // Targeting 模式下悬停合法目标 → 预选高亮
        if (battleContext != null && battleContext.IsInTargetingMode() && battleContext.IsValidTarget(unitData))
        {
            unitData.SetHighlightState(UnitData.HighlightState.Highlighted);
        }
    }

    void OnMouseExit()
    {
        if (unitData == null) return;

        // 退出悬停 → 恢复为当前应有的状态
        if (battleContext != null && battleContext.IsInTargetingMode())
        {
            if (battleContext.IsValidTarget(unitData))
                unitData.SetHighlightState(UnitData.HighlightState.Normal);
        }
    }

    // ==================== 内部 ====================

    void FireClickEvent()
    {
        if (unitData == null) return;

        // 防止同帧双触发（OnMouseDown + Update 边界场景）
        if (Time.time - lastClickTime < 0.05f) return;
        lastClickTime = Time.time;

        // 检查合法性（选目标阶段非法点击不触发）
        if (battleContext != null && battleContext.IsInTargetingMode())
        {
            if (!battleContext.IsValidTarget(unitData))
                return;
        }

        Log.Info($"[点击] 选中 {unitData.unitName}");
        OnUnitClicked?.Invoke(unitData);
    }
}
