using UnityEngine;

/// <summary>
/// 点击监听器 — 挂在单位预制体根节点上
/// 支持合法性判断 + 悬停预选高亮
/// </summary>
public class UnitClickDetector : MonoBehaviour
{
    public static event System.Action<UnitData> OnUnitClicked;

    private UnitData unitData;
    private BattleManager battleManager;

    void Awake()
    {
        unitData = GetComponent<UnitData>();
        if (unitData == null)
            Log.Warn($"[UnitClickDetector] {gameObject.name} 上找不到 UnitData 组件");
    }

    void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
    }

    void OnMouseDown()
    {
        if (GridManager.IsUIPanelOpen()) return;
        FireClickEvent();
    }

    void Update()
    {
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
        if (battleManager != null && battleManager.IsInTargetingMode() && battleManager.IsValidTarget(unitData))
        {
            unitData.SetHighlightState(UnitData.HighlightState.Highlighted);
        }
    }

    void OnMouseExit()
    {
        if (unitData == null) return;

        // 退出悬停 → 恢复为当前应有的状态
        if (battleManager != null && battleManager.IsInTargetingMode())
        {
            if (battleManager.IsValidTarget(unitData))
                unitData.SetHighlightState(UnitData.HighlightState.Normal);
        }
    }

    // ==================== 内部 ====================

    void FireClickEvent()
    {
        if (unitData == null) return;

        // 检查合法性（选目标阶段非法点击不触发）
        if (battleManager != null && battleManager.IsInTargetingMode())
        {
            if (!battleManager.IsValidTarget(unitData))
                return;
        }

        Log.Info($"[点击] 选中 {unitData.unitName}");
        OnUnitClicked?.Invoke(unitData);
    }
}
