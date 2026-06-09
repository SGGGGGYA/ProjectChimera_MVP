# A1：文档 vs 实现偏差对照表

> **生成日期**：2026-06-09
> **覆盖范围**：`e:\游\暗黑地牢英雄代码实现.md`（数值框架）
> **基础**：原"勘误表"列出 12 条已确认偏差，本报告**新增 13 条**并修正勘误表中 2 条数据陈旧
> **更新**：2026-06-09 — 新增"修复记录"段，已完成 P0 #1~#5 的代码层修复

---

## 严重度图例

- 🔴 **致命**：实际游戏行为与设计完全不同，会导致核心机制失效
- 🟠 **重要**：核心数值偏离，会显著影响游戏体验
- 🟡 **中等**：细节不一致，暂不致命但有隐患
- 🟢 **微差**：命名/格式差异，不影响功能

---

## 修正现有勘误表（2 条陈旧）

| # | 字段 | 文档/勘误表说 | **实际代码**（2026-06-09） | 严重度 |
|---|---|---|---|---|
| C-1 | 命中基础值 | 90%（文档/勘误表均） | **`0.75f`**（`CombatSystem.cs` `BASE_HIT_CHANCE`） | 🟠 |
| C-2 | 命中 ACC 系数 | 1%/点（勘误表） | **`0.003f`/点**（`HIT_ACC_SCALE`，约 0.3%/点） | 🟠 |

> ⚠️ 勘误表中的 `Mathf.Clamp(0.90f + (ACC - DOD) * 0.01f, 0.05f, 0.98f)` 是**旧版本**的代码。
> 当前代码用的是 `0.75 + ... * 0.003`，钳制到 `[0.10, 0.95]`。这个改动**没有回写到勘误表**。

---

## 新增偏差（13 条）

### 🔴 致命（5 条）

| # | 字段 | 文档说 | **代码实际** | 涉及文件 |
|---|---|---|---|---|
| 1 | **SPD 计算** | 应由 AGI 派生 | **`GetFinalStat(SPD) = 0`**（`GetClassBaseByType` 和 `GetPrimaryByType` 都不处理 SPD；`ClassDefinition` 没有 `baseSPD` 字段） | `UnitData.cs:132-156` / `ClassDefinition.cs` |
| 2 | **ACC 计算** | 应由 AGI 派生 | **`GetFinalStat(ACC) = 0`**（同上原因） | 同上 |
| 3 | **DOD 计算** | 应由 AGI 派生 | **`GetFinalStat(DOD) = 0`** | 同上 |
| 4 | **CRT 计算** | 应由 AGI 派生 | **`GetFinalStat(CRT) = 0`** | 同上 |
| 5 | **`DerivedAttributes` 完全未集成** | 应该是 `UnitData` 的属性管线 | 整个结构体只有定义 + 构造函数两行，**全工程零引用**（仅出现 2 次，都在自身文件） | `DerivedAttributes.cs:5,14` |

**实际后果**：
- 全员命中率 = `Clamp(0.75 + 0*0.003, 0.10, 0.95) = 0.75`（基础值）
- 全员暴击率 = `Clamp(0.03 + 0*0.005, 0.01, 0.70) = 0.03`（基础值）
- 全员闪避 = 0（命中公式无下拉）
- 全员速度 = 0（TurnManager 用 SPD 排序时全平手 → 走随机）

**测试现状**：`CombatSystemAccuracyTests` 用 `unit.modifiers.Add(new AttributeModifier(Add, ACC, 100, "test"))` **手动喂入**，绕过了 GetBaseStat 路径。这是测试工作区，不是真修复。

**修复建议**（推荐一行修复）：
```csharp
// ClassDefinition.cs 添加 4 个字段
public int baseSPD, baseACC, baseDOD, baseCRT;
// 或更好：在 GetClassBaseByType 中加 default return 20
// 或最好：把 DerivedAttributes 接入 GetFinalStat
```

---

### 🟠 重要（5 条）

| # | 字段 | 文档说 | **代码实际** | 涉及文件 |
|---|---|---|---|---|
| 6 | **平 A 伤害** | 含力量系数、武器系数 | **`BattleManager.cs:885` 用 `new SkillData { baseDamage=0, strScaling=0, agiScaling=0 }`** → 平A = `weaponAttack - def`，无任何 STR/AGI 缩放 | `BattleManager.cs:885` |
| 7 | **`UnitData.CalculateDamage` 是死代码** | — | 60+ 行实例方法 (`UnitData.cs:661-720+`)，**全工程零调用**（仅 7 行出现，都是定义本身） | `UnitData.cs:661` |
| 8 | **AI 角色识别** | 数据驱动 | 用 **`unitName.Contains("萨满")`** / `Contains("精英")` / `Contains("弓手")` 5 处字符串匹配 | `EnemyAIEngine.cs:68, 178-184` |
| 9 | **AI 难度识别** | 数据驱动 | 同上，用 `Contains("精英")` 判断难度 | `EnemyAIEngine.cs:68` |
| 10 | **音效 key 选择** | 数据驱动 | `BattleManager.cs:836-840` 用 `unitName.Contains` 选击中音效 | `BattleManager.cs:836-840` |

**实际后果**：
- 战士拿匕首 = 匕首的物攻（没有 STR 修正，体验很怪）
- 把"哥布林弓手"重命名成"哥布林神射手"→ AI 突然不知道它是弓手
- 在 ClassDefinition 里把 `id` 改成 `archer_2` 不影响行为，但改 `unitName` 就崩

---

### 🟡 中等（3 条）

| # | 字段 | 文档说 | **代码实际** | 涉及文件 |
|---|---|---|---|---|
| 11 | **装备价格来源** | 应从 JSON 数据表读取 | **`Equipment.GetBuyPrice()` 内联硬编码**（50 + Σ `rng.Next(30, 81)` per mod，用 id 哈希做种子） | `Equipment.cs:36-47` |
| 12 | **职业定义来源** | 应从 `.info.darkest` 自定义格式加载 | **全硬编码**在 `GameManager.InitClassDefinitions()`（约 360 行） | `GameManager.cs:189-551` |
| 13 | **`UnitBattleData` vs `UnitData` 双数据源** | 未明确（应一源） | 两份结构：持久数据用 `UnitBattleData`（不带 `classData` 引用），战斗时克隆到 `UnitData`（带 `classData`）。**属性字段名/类型不一致**：`UnitBattleData.DEF` vs `UnitData.baseDefense`；`UnitBattleData.maxHp` vs `UnitData.MaxHp`（实际是属性） | `GameManager.cs:15-50` / `UnitData.cs:15` |

---

## 设计文档**已实现但实现方式不一样**的项（不是 bug，是简化）

| 设计承诺 | 实现方式 | 评价 |
|---|---|---|
| 暴击无视 50% DEF | `CombatSystem.CalculateDamage` 内已实现 | ✓ 正确 |
| 命中溢出转暴击 | `IsCrit` 内 `overflow * 0.30` 已实现 | ✓ 正确 |
| 未命中+2 压力 | `DealDamageCommand` + `BattleManager.DoBasicAttack` 已实现 | ✓ 正确 |
| 暴击减压（攻击方 -5） | **已实装**在 `StressConfig.onCriticalDeal = -5` | ✓ 正确（勘误表说"未实现"是旧的） |
| 经验曲线 | `100 * 2^(level-1)` 上限 5 级 | 简化版可接受 |
| 等级上限 | 5 级（文档说 10） | 简化版可接受 |

---

## 总览：哪些字段 100% 没问题 / 哪些永远返回 0

| 字段 | 文档公式 | 实际返回 | 状态 |
|---|---|---|---|
| VIT | `primary.VIT + classBase.VIT` | `primary.VIT + classData.baseVIT` | ✅ |
| STR | `primary.STR + classBase.STR` | `primary.STR + classData.baseSTR` | ✅ |
| AGI | `primary.AGI + classBase.AGI` | `primary.AGI + classData.baseAGI` | ✅ |
| INT | `primary.INT + classBase.INT` | `primary.INT + classData.baseINT` | ✅ |
| DEF | `primary.DEF + classBase.DEF` | **`0 + classData.baseDEF`**（primary 无 DEF 字段） | ⚠️ partial |
| MaxHP | `VIT*5 + baseHp` | `primary.VIT*5 + classData.baseHp` | ✅ |
| **SPD** | `AGI*2 + classBase.SPD` | `AGI*2 + classData.baseSPD` | ✅ **已修复** |
| **ACC** | `5 + AGI*5 + classBase.ACC` | `5 + AGI*5 + classData.baseACC` | ✅ **已修复** |
| **DOD** | `AGI*2 + classBase.DOD` | `AGI*2 + classData.baseDOD` | ✅ **已修复** |
| **CRT** | `round(AGI*1.5) + classBase.CRT` | `round(AGI*1.5) + classData.baseCRT` | ✅ **已修复** |

---

## ✅ 修复记录（2026-06-09 完成 P0 #1~#5）

### 改动文件
- `Assets/Scripts/Data/ClassDefinition.cs` — 新增 `baseSPD/baseACC/baseDOD/baseCRT` 4 个字段
- `Assets/Scripts/Attributes/DerivedAttributes.cs` — 构造函数扩为 7 参数，把 ACC 公式从 `20 + AGI*5` 改为 `5 + AGI*5 + classBaseACC`（让 class 真正影响命中）
- `Assets/Scripts/UnitData.cs` — `GetBaseStat` 改用 `DerivedAttributes` 统一计算；`GetClassBaseByType` 补全 4 个 StatType 分支；删除已无人调用的 `GetPrimaryByType`
- `Assets/Editor/Tests/EditMode/StatCalculationTests.cs` — 改写 2 个锁定旧 Bug 的测试，新增 4 个回归测试
- `Assets/Editor/Tests/EditMode/NamingMomentTests.cs` — 改写 1 个命名怪癖测试（之前断言 `ACC=0`，现在断言正确值 35）

### 修复后行为
- AGI=5 默认角色：SPD=10, ACC=30, DOD=10, CRT=8
- 高 AGI=20：SPD=40, ACC=105, DOD=40, CRT=30
- `DerivedAttributes` 零引用问题同步解决（`UnitData.GetBaseStat` 是它唯一调用点）
- `CombatSystem.IsHit/IsCrit` 立即恢复设计意图，全员命中 75% + 暴击 3% 的旧假象消失

### 验证
- EditMode 单元测试需在 Unity 中通过 Test Runner 重跑（CLI 命令见 A2 报告末尾）

---

## 修复优先级建议

| 优先级 | 项 | 工时估算 | 价值 |
|---|---|---|---|
| **P0** | #1-#5（SPD/ACC/DOD/CRT 全废） | 1-2h | 极大，让"敏捷型"职业真的有意义 |
| **P1** | #6（平 A 缩放） | 30min | 战士/狂战士手感修正 |
| **P1** | #7（删除死代码 UnitData.CalculateDamage） | 15min | 减少维护负担 |
| **P2** | #8-#10（AI/音效字符串改枚举） | 2-3h | 防止重命名连锁 bug |
| **P3** | #11-#13 | 6h+ | 重构级，看优先级 |
