# Task 03: 战斗系统稳定性修复与自动化测试流程

## 功能描述

全面修复《Project Chimera》战斗系统中剩余的 P1/P2 级运行时崩溃风险，并实现一个编辑器内的自动化战斗测试框架。该框架能够在不依赖玩家输入的情况下，自动模拟多场完整战斗（从开场到胜利/失败/撤退），输出详细的测试报告和性能数据，确保战斗核心逻辑在各种边缘情况下稳定运行。

## 技术栈要求

- Unity 2022.3 LTS
- C#（运行时脚本 + 编辑器脚本）
- Unity Editor 测试框架（可选，但推荐用自定义编辑器窗口实现）
- 项目现有系统：BattleManager、TurnManager、EnemyAIEngine、CombatSystem、Command

## 输入数据规范

### 测试配置（ScriptableObject 或 JSON）
```csharp
[System.Serializable]
public class BattleTestCase
{
    public string testName;           // 测试用例名称
    public int playerCount;           // 玩家数量（1-4）
    public int enemyCount;            // 敌人数量（1-4）
    public string[] playerClasses;    // 玩家职业（如 "warrior", "ranger"）
    public string[] enemyTypes;       // 敌人类型（如 "goblin", "boss"）
    public int maxTurns;              // 最大回合数（防止死循环）
    public bool autoPlay;             // 是否自动选择技能（true = AI代打玩家）
    public AIDifficulty difficulty;   // AI难度
}
```

### 预设测试用例（至少覆盖以下场景）

1. **基础战斗**：2玩家 vs 2敌人，正常打到胜利
2. **极限人数**：4玩家 vs 4敌人，高强度战斗
3. **单人生存**：1玩家 vs 3敌人，测试死亡之门逻辑
4. **全灭测试**：1玩家 vs 4敌人，预期玩家全灭触发 GameOver
5. **撤退测试**：战斗中主动撤退，验证撤退逻辑
6. **空敌人列表**：战斗开始时敌人列表为空，验证立即胜利
7. **玩家全死**：玩家开场 HP=0，验证战斗结束
8. **长时间战斗**：设置 maxTurns=50，验证不会无限循环

## 实现步骤

### 1. 修复剩余崩溃风险

扫描并修复以下文件中的潜在崩溃点（使用 try-catch 或判空保护）：

**`BattleManager.cs`**
- `ShowRewardPanel()` / `ShowGameOverPanel()`：确保 `FindObjectOfType<Canvas>()` 后所有分支都安全
- `CreateTargetArrow()`：确保 `targetArrowCanvas` 和 `targetArrow` 在 null 时自动创建
- `HighlightTarget()` / `ClearHighlight()`：确保 `unit` 参数不为 null
- 所有 `foreach (var unit in playerUnits/enemyUnits)` 循环：确保遍历过程中列表不被异常修改

**`EnemyAIEngine.cs`**
- `ExecuteTurn()` 入口：验证 `bm.playerUnits` 和 `bm.enemyUnits` 都不为 null 且非空
- `SelectBestTarget()`：如果所有评分后 bestTarget 仍为 null，返回 players.FirstOrDefault() 而非直接访问
- `TryEmergencyHeal()` / `TryShield()`：确保 `allies` 列表非空

**`TurnManager.cs`**
- `EnemyTurn()`：在调用 `EnemyAIEngine.ExecuteTurn()` 前，确认当前回合单位仍然存活（HP > 0）
- `NextTurn()`：如果 `turnOrder` 为空，自动调用 `RebuildTurnOrder()`，若仍为空则结束战斗

**`CombatSystem.cs`**
- `DealDamage()`：如果 `target` 为 null 或已死亡，直接返回
- `ApplyStatus()`：如果 `target` 为 null，直接返回
- `IsHit()`：如果 `attacker` 或 `defender` 为 null，返回 false

**`UnitData.cs`**
- `GetSkeleton()`：如果 `GetComponentInChildren` 失败，记录 Warning 并返回 null，不要抛异常
- `Play()` / `SetIdle()`：如果动画名为空或 Skeleton 为 null，静默跳过
- `Die()`：如果 `isDead` 已为 true，防止重复调用

**`Command.cs`**
- `DealDamageCommand.Execute()`：在扣除 HP 前确认 `target` 不为 null
- `HealCommand.Execute()`：确认 `target` 和 `ctx.selectedTarget` 不为 null
- `ApplyStatusCommand.Execute()`：确认 `target` 不为 null

### 2. 创建自动化测试框架 `BattleTestRunner.cs`

在 `Assets/Editor/` 下创建编辑器窗口（Menu: Window > Battle Test Runner）：

1. **测试初始化**：
   - 创建临时场景或清空当前场景
   - 自动生成 BattleManager、TurnManager、Canvas、Camera、EventSystem
   - 根据测试用例生成玩家和敌人单位

2. **自动战斗循环**：
   - 每帧调用 `TurnManager.Update()` 或手动推进回合
   - 玩家回合：自动选择第一个可用技能，随机选择合法目标
   - 敌人回合：由 `EnemyAIEngine` 自动决策（现有逻辑）
   - 物品使用：关闭（简化测试）

3. **战斗结束检测**：
   - 监听 `BattleEvents.OnBattleWon` 和 `OnBattleLost`
   - 达到 `maxTurns` 时强制结束，标记为 "Timeout"

4. **日志记录**：
   - 记录每一回合的行动摘要（谁做了什么，伤害多少）
   - 记录任何异常（Exception）和错误日志
   - 记录战斗总时长、总回合数

### 3. 生成测试报告

测试完成后生成文本报告 `BattleTestReport_<timestamp>.txt`：

```
========== 战斗自动化测试报告 ==========
测试时间: 2026-06-10 17:30:00
测试用例数: 8
通过: 7
失败: 1
超时: 0

[PASS] 基础战斗 (2v2) - 12回合, 耗时1.2s, 结果: Victory
[PASS] 极限人数 (4v4) - 28回合, 耗时3.5s, 结果: Victory
[PASS] 单人生存 (1v3) - 8回合, 耗时0.9s, 结果: Defeat
[FAIL] 空敌人列表 - Exception: NullReferenceException at BattleManager.cs:861
[PASS] ...

========== 异常详情 ==========
[空敌人列表] NullReferenceException: Object reference not set to instance...
   at BattleManager.DoBasicAttack() line 861
```

### 4. 性能监控（可选增强）

- 记录每回合的 GC Alloc 大小
- 记录单场战斗的总内存增长
- 如果单场战斗超过 5MB 内存增长，标记为 "内存泄漏风险"

## 输入输出规范

### 输入
- 测试用例数组（硬编码在脚本中或从 JSON 读取）
- 场景引用：`BattleManager` prefab、`universalCharacterPrefab`

### 输出
- Console 日志（实时）
- `BattleTestReport_<timestamp>.txt`（详细报告）
- 测试通过的用例在编辑器窗口中显示绿色，失败的显示红色

## 错误处理要求

1. **测试隔离**：每个测试用例在独立的场景中运行，一个测试失败不影响下一个
2. **超时保护**：每个测试用例最多运行 `maxTurns` 回合或 30 秒实际时间，超时自动标记失败
3. **异常捕获**：测试循环用 try-catch 包裹，任何异常记录详细信息后继续下一个测试
4. **资源清理**：测试结束后销毁所有临时创建的对象，恢复场景原状
5. **编辑器安全**：测试运行期间禁用 Inspector 刷新，防止 Editor 卡顿

## 验收标准

- [ ] 编辑器窗口能正常打开并显示所有预设测试用例
- [ ] 点击 "Run All Tests" 后自动顺序执行所有测试用例
- [ ] 每个测试用例在 30 秒内完成（或超时标记）
- [ ] 测试报告中清晰标记通过/失败/超时，并附异常堆栈
- [ ] 所有 P1 级崩溃点（如空列表访问、null 引用）都已添加防御性代码
- [ ] 连续运行 10 次完整测试套件，Unity 编辑器不崩溃
- [ ] 代码注释使用中文
