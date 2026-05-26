# Project Chimera — 子Agent启动提示词（修正版 v2）

> **📌 历史记录** — 本文档是开发过程中使用的 AI Agent 提示词，记录了各系统的实现方式。描述的是实现前的"当前状态"，而非当前项目现状。最后审查日期：2026-05-26。

> 使用方式：将所选提示词完整粘贴给独立的 AI 对话（子 Agent），该 Agent 将专注完成对应模块。
> 每个提示词包含：模块目标、当前状态、技术要求、范围边界、交付物、一致性约束、验证步骤。
>
> ⚠️ 跨提示冲突注意：Prompt 1（战斗公式）和 Prompt 5（音效/VFX）都会修改 `BattleManager.DealDamage`。**不要同时执行这两个 Prompt**，否则会产生合并冲突。请依次执行，或执行前先提交。

---

## Prompt 1：战斗公式系统 (Combat Formula Pipeline)

```
你是一个 Unity C# 游戏开发专家，正在为 Project Chimera（Darkest Dungeon 风格的 2D 回合制战术 RPG）实现战斗公式系统。

## 模块目标
为普攻和技能伤害实现完整的概率公式管线（命中/闪避/暴击/伤害波动），替换当前"必定命中"的硬编码逻辑。

## 当前状态
- `BattleManager.DoBasicAttack()` 直接选目标、直接扣血，无命中/闪避/暴击判定
- `DealDamageCommand` 有 `baseDamage` + STR/AGI scaling，但 ACC、DOD、SPD 属性已定义在 `DerivedAttributes.cs` 中且 `UnitData` 已有 `ACC`/`DOD`/`SPD` 属性，但从未用于公式
- `CombatSystem.cs` 不存在——公式散布在多个方法中
- `StressManager.GetStatModifier(unit)` 已提供压力对属性的影响
- ⚠️ **项目全部代码无 namespace**（所有类全局可见），不要引入 namespace

## 技术要求

1. **创建 `Assets/Scripts/Combat/CombatSystem.cs`** 作为所有公式的唯一入口，纯静态类，包含：

   - `int CalculateDamage(UnitData attacker, UnitData target, SkillData skill)`:
     - 基础 = skill.baseDamage + attacker.weaponAttack
     - STR/AGI scaling = attacker.GetEffectiveSTR() * skill.strScaling + attacker.AGI * skill.agiScaling
     - **伤害波动 = ±15% 在 CalculateDamage 内部完成**（不拆成独立方法）
     - 目标防御减免 = target.DEF_Effective * 0.5（线性减免）
     - 标记增伤 = target.GetIncomingDamageMultiplier()
     - 压力修正 = attacker.GetDamageModifier()
     - 如果 `IsCrit()` == true → 暴击倍率 1.5x
     - 最终 = Mathf.Max(1, Mathf.RoundToInt(raw))

   - `bool IsHit(UnitData attacker, UnitData target)`:
     - baseHitChance = 0.90
     - attackerACC = attacker.ACC（`GetFinalStat(StatType.ACC)`）
     - targetDOD = target.DOD（`GetFinalStat(StatType.DOD)`）
     - finalChance = Mathf.Clamp(0.90f + (attackerACC - targetDOD) * 0.01f, 0.05f, 0.98f)
     - return Random.value < finalChance

   - `bool IsCrit(UnitData attacker, UnitData target)`:
     - baseCrit = 0.05
     - attackerCRT = 从 `GetFinalStat(StatType.CRT)` 获取（需新增 CRT——见下）
     - finalCrit = Mathf.Clamp(0.05f + attackerCRT * 0.01f, 0.01f, 0.50f)
     - return Random.value < finalCrit

2. **扩展 `StatType.cs`** 添加 `CRT`。在 `DerivedAttributes.cs` 中添加 CRT 计算：
   - baseCRT = 0
   - 每 10 点 AGI 提供 1 CRT
   - 装备/特质额外加成

3. **修改 `BattleManager.DoBasicAttack()`**:
   - 先 `IsHit(attacker, target)` → 未命中时 BattleLog 记录 "未命中！" 并 return
   - 命中后 `IsCrit()` → 暴击时 BattleLog 记录 "暴击！"
   - `CalculateDamage(...)` 替换手动计算
   - 结果仍走 `DealDamage(attacker, target, damage)` 管线

4. **修改 `DealDamageCommand.Execute()`**:
   - 单体技能：调用 `CombatSystem.IsHit` + `IsCrit` 判定
   - AoE 技能：**跳过命中判定**（全体必中），但仍做暴击判定
   - 使用 `CombatSystem.CalculateDamage` 替换手动计算

5. **修改 `UnitData.cs`**:
   - 确认 `statToTarget` 字典中 `ACC→AttributeTarget.ACC`、`DOD→AttributeTarget.DOD`、`SPD→AttributeTarget.SPD` 映射完整
   - 添加 `CRT→AttributeTarget.CRT`（如果 `AttributeTarget` 没有 CRT 则添加）

## 范围边界
- **在范围内**: 命中/闪避/暴击/伤害波动公式、战斗日志输出、压力对属性的影响纳入公式
- **不在范围内**: 动画/VFX/音频/ScreenShake、状态效果的命中判定（如眩晕概率）、治疗暴击
- **不在范围内**: 修改现有技能的伤害数值

## 交付物
1. `Assets/Scripts/Combat/CombatSystem.cs`
2. `StatType.cs` + `DerivedAttributes.cs` 的修改（CRT）
3. `BattleManager.cs` 中 `DoBasicAttack` 的修改
4. `Commands/Command.cs` 中 `DealDamageCommand.Execute()` 的修改
5. `UnitData.cs` 中 statToTarget 映射的补充
6. 在 `CombatSystem.cs` 顶部用注释写明所有公式

## 一致性约束
- 所有日志文本中文（"未命中！"、"暴击！"、"闪避！"）
- **不使用 namespace**（项目无 namespace）
- `CombatSystem` 是纯静态类，无 MonoBehaviour
- 不要改动 `StressManager`、`TurnManager`、`GameManager`
- 伤害波动在 `CalculateDamage` 方法内部完成

## 验证步骤
1. 进入战斗，使用普攻，确认 BattleLog 出现"未命中！"或暴击消息
2. 多次测试同一场战斗，确认命中率大约在 85-95% 之间
3. 确认 AoE 技能不单独判定命中
```

---

## Prompt 2：特质系统实装 (Quirk Gameplay Integration)

```
你是一个 Unity C# 游戏开发专家，正在为 Project Chimera 实现特质系统——让 Quirk 数据在实际战斗中产生效果。

## 模块目标
将现有的 `Quirk` 数据模型接入战斗系统的各个环节，使角色特质在战斗开始、回合开始、受击、击杀、压力、治疗等时机触发实际效果。

## 当前状态
- `Data/Quirk.cs` 定义了 `Quirk`（id, name, isPositive, isLocked）和 `QuirkEffect`（stat, amount, isPercent）
- `UnitData.quirks` 是 `List<Quirk>`；`GetQuirkFlat`/`GetQuirkPercent` 已实现并被 `GetFinalStat` 调用
- 特质显示在 `UIUnitInfoController` 中（只读展示）
- **任何 Quirk 的动态行为为零**——没有战斗内触发、没有事件驱动的效果
- `BattleSetup.cs` 中 `data.quirks` 已同步到 `UnitData.quirks`
- ⚠️ **项目全部代码无 namespace**

## 技术要求

1. **在 `Quirk.cs` 中添加触发数据**（**复用 `QuirkEffect`，不要新增平行字段**）:
   - `public QuirkTriggerType triggerType = QuirkTriggerType.BattleStart`
   - `public float procChance = 1.0f`（触发概率）
   - `public int duration`（持续回合数，0=即时生效；-1=整场战斗）
   - 效果的数值和属性仍用 `effects` 列表中的 `QuirkEffect`

2. **创建 `Assets/Scripts/Stress/QuirkTriggerSystem.cs`**:
   - 纯静态类
   - `enum QuirkTriggerType { BattleStart, TurnStart, OnHit, OnTakeDamage, OnKill, OnStress, OnHeal, OnDeathsDoor }`
   - 提供多个类型安全的重载，而非 `object context`:
     - `CheckTriggers(UnitData unit, QuirkTriggerType trigger)`
     - `CheckTriggers(UnitData unit, QuirkTriggerType trigger, UnitData sourceUnit)` （用于 OnHit/OnKill）
     - `CheckTriggers(UnitData unit, QuirkTriggerType trigger, int numericValue)` （用于 OnStress/OnHeal 传伤害量）
   - 遍历 `unit.quirks`，检查 `quirk.triggerType == trigger`，按 `procChance` 判定触发
   - 触发后按 `QuirkEffect` 列表执行效果（stat 修改通过 `modifiers` 添加 `AttributeModifier`，HP 恢复通过 `HealHP`，压力通过 `stress` 直接修改）

3. **在 `UnitData.cs` 中添加辅助方法**:
   - `Dictionary<string, int> quirkCooldowns` — 每回合 -1，为 0 时可再次触发
   - `bool HasQuirk(string id)`
   - `void HealHPPercent(float percent)` — `HealHP(Mathf.RoundToInt(MaxHp * percent))`，用于"嗜血""顽强"等百分比效果
   - 注意：`HealHP()` 已存在（接受 int 参数），只需加百分比版本

4. **创建 10 个预制特质**（注册在 `QuirkTriggerSystem` 的 `static` 构造函数或字典中）:

   | 特质名 | 类型 | 触发 | 效果 | 概率/冷却 |
   |--------|------|------|------|----------|
   | 战意昂扬 | 正面 | BattleStart | +5 STR 整场 | 100% |
   | 铁壁 | 正面 | OnHit | +3 DEF 2回合 | 30% |
   | 嗜血 | 正面 | OnKill | 恢复 10% MaxHP | 100% |
   | 冷静 | 正面 | OnStress | 额外 -5 压力 | 20% |
   | 顽强 | 正面 | OnDeathsDoor | 恢复 50% HP（仅一次） | 40% |
   | 懦弱 | 负面 | BattleStart | -3 STR 整场 | 100% |
   | 脆弱 | 负面 | OnTakeDamage | +2 压力 | 40% |
   | 不详预感 | 负面 | TurnStart | +5 压力 | 25% |
   | 自虐 | 负面 | OnHeal | +5 压力 | 30% |
   | 易伤 | 负面 | OnTakeDamage | 伤害额外 +20%（即时） | 100% |

5. **插入触发点**:
   - `BattleManager.Start()` 或 `BattleSetup` 完成后：`BattleStart`
   - `TurnManager.StartNextTurn()` 中跳过了眩晕/死亡的单位后：`TurnStart`
   - `BattleManager.DealDamage()` 中：`OnTakeDamage`（target）+ `OnHit`（attacker）
   - `BattleManager.DealDamage()` 中敌人死亡时：`OnKill`（attacker）
   - `StressManager.AddStress()` 中：`OnStress`
   - `HealCommand.Execute()` 中：`OnHeal`

## 范围边界
- **在范围内**: 特质触发系统、10 个预制特质、触发点插入、冷却机制、百分比治疗辅助
- **不在范围内**: 特质获取/洗练/锁定/删除的 UI、特质池随机分配、是否显示触发概率在界面上
- **不在范围内**: 分配默认特质给玩家角色

## 交付物
1. `Assets/Scripts/Stress/QuirkTriggerSystem.cs`
2. `Data/Quirk.cs` 的修改（添加 triggerType/procChance/duration）
3. `UnitData.cs` 中添加 `HealHPPercent()` + `HasQuirk()` + `quirkCooldowns`
4. `BattleManager.cs` 中插入的触发点
5. `TurnManager.cs` 中插入的触发点
6. `StressManager.cs` 中插入的触发点
7. `Commands/Command.cs` 中 `HealCommand` 的触发点

## 一致性约束
- `BattleLog` 格式：`[特质] {unitName} 的 [{quirkName}] 触发了！效果: ...`
- 保持 `Quirk` 和 `QuirkEffect` 的 `[System.Serializable]`
- 冷却状态不需要持久化（每场战斗重置）
- 触发点需容错：如果 `QuirkTriggerSystem` 未初始化，静默跳过

## 验证步骤
1. 给一个玩家角色在 `BattleSetup` 或测试数据中添加 "战意昂扬" 特质
2. 进入战斗，确认 BattleLog 显示特质触发、STR 面板提升
3. 添加 "不详预感" 特质，确认每回合开始 +5 压力
```

---

## Prompt 3：商店与经济系统 (Shop & Economy)

```
你是一个 Unity C# 游戏开发专家，正在为 Project Chimera 实现商店与经济系统。

## 模块目标
创建商店 UI 和交易系统：商品列表、购买/出售、金币管理、商品刷新。**同时将现有装备纳入 ItemDatabase 体系**，使装备可作为商品流通。

## 当前状态
- `GameManager.gold` 已存在（int，初始 100），`inventory` 是 `List<ItemStack>`
- `ItemDatabase` 已初始化，运行时只有 `health_potion` 和 `stress_herb` 两个内置物品
- `ItemDefinition` 已有 `weaponTemplate`/`armorTemplate` 字段，但装备**未被注册为物品**
- `Equipment.cs` 定义 `Weapon`/`Armor`/`StatMod`，装备通过 `MakeTestWeapon()` 在 GameManager 中硬编码创建
- ⚠️ **关键先决条件：需要先在 ItemDatabase 中注册装备物品**，否则商店无货可卖

## 技术要求（共 7 步）

### 第一阶段：装备物品化

1. **在 `ItemDatabase.InitBuiltins()` 中添加 4 件可购买装备**:
   ```csharp
   // 铁剑 (weapon)
   var ironSword = ScriptableObject.CreateInstance<ItemDefinition>();
   ironSword.itemId = "iron_sword"; ironSword.itemName = "铁剑";
   ironSword.category = ItemCategory.Weapon; ironSword.maxStack = 1;
   ironSword.buyPrice = 120; ironSword.sellPrice = 60;
   ironSword.description = "一把普通的铁剑\nSTR+3";
   ironSword.weaponTemplate = new Weapon { id = "iron_sword", equipmentName = "铁剑",
       mods = new List<StatMod>{ new StatMod{stat=StatType.STR, amount=3} } };
   _items["iron_sword"] = ironSword;

   // 皮甲 (armor)
   var leatherArmor = ScriptableObject.CreateInstance<ItemDefinition>();
   leatherArmor.itemId = "leather_armor"; leatherArmor.itemName = "皮甲";
   leatherArmor.category = ItemCategory.Armor; leatherArmor.maxStack = 1;
   leatherArmor.buyPrice = 100; leatherArmor.sellPrice = 50;
   leatherArmor.armorTemplate = new Armor { id = "leather_armor", equipmentName = "皮甲",
       mods = new List<StatMod>{ new StatMod{stat=StatType.DEF, amount=2} } };
   _items["leather_armor"] = leatherArmor;

   // 生命戒指 (accessory-like, +MaxHP)
   var lifeRing = ScriptableObject.CreateInstance<ItemDefinition>();
   lifeRing.itemId = "life_ring"; lifeRing.itemName = "生命戒指";
   lifeRing.category = ItemCategory.Weapon; lifeRing.maxStack = 1;
   lifeRing.buyPrice = 200; lifeRing.sellPrice = 100;
   lifeRing.description = "蕴含生命能量的戒指\nMaxHP+15";
   lifeRing.weaponTemplate = new Weapon { id = "life_ring", equipmentName = "生命戒指",
       mods = new List<StatMod>{ new StatMod{stat=StatType.MaxHP, amount=15} } };
   _items["life_ring"] = lifeRing;

   // 速度靴
   var speedBoots = ScriptableObject.CreateInstance<ItemDefinition>();
   speedBoots.itemId = "speed_boots"; speedBoots.itemName = "速度之靴";
   speedBoots.category = ItemCategory.Armor; speedBoots.maxStack = 1;
   speedBoots.buyPrice = 180; speedBoots.sellPrice = 90;
   speedBoots.armorTemplate = new Armor { id = "speed_boots", equipmentName = "速度之靴",
       mods = new List<StatMod>{ new StatMod{stat=StatType.SPD, amount=3} } };
   _items["speed_boots"] = speedBoots;
   ```

2. **在 `ItemDefinition.cs` 中添加**:
   - `public int buyPrice = 50`
   - `public int sellPrice = 25`

### 第二阶段：商店系统

3. **创建 `Data/ShopData.cs`**:
   - `public List<string> shopItemIds` — 当前出售的物品 ID 列表
   - `public void RefreshShop()` — 从 `ItemDatabase.GetAll()` 中随机选 6 件：
     - 至少包含 health_potion 和 stress_herb
     - 随机抽 4 件（优先选 category=Weapon 或 Armor 的）
   - 每次返回世界地图时调用 `RefreshShop()`

4. **创建 `Assets/Scripts/UI/UIShopController.cs`**:
   - 遵循 `UIInventoryController` 的 `EnsurePanel()` 运行时 UI 创建模式
   - `GameManager.Start()` 中自动创建（`FindObjectOfType` 检查）
   - B 键打开（仅 `GameState.WorldMap`），Escape 关闭
   - 布局：左侧商品列表（可滚动，带 ScrollRect + Mask）、右侧详情+按钮
   - 每个商品显示：名称、价格（金色）、持有量("已有: X")
   - 详情面板：名称、描述、价格、持有量、"购买"按钮、"出售"按钮
   - 购买：检查金币 → 扣除 → `GameManager.AddItem`
   - 出售：检查库存 → `GameManager.RemoveItem` → 加金币
   - 右下角显示 `💰 {gold}`（引用 `GameManager.gold`，无需轮询）
   - 如果商品列表为空（`shopItemIds.Count == 0`），显示 "今日无货" 文本
   - 如果 `ItemDatabase.Get(id)` 返回 null，跳过该商品项

5. **修改 `GameManager.cs`**:
   - 添加 `public ShopData shopData`
   - `ReturnToWorldMap()` 中调用 `shopData.RefreshShop()`
   - `SaveGame()` / `LoadGame()` 中加入 `shopData`

### 第三阶段：定价一致性

6. **装备定价规则**（统一在 ItemDefinition 层面，不需要 `Equipment.GetBuyPrice()`）:
   - `buyPrice` 在 `InitBuiltins()` 中硬编码（见上面第 1 步）
   - 存档不存价格，价格由物品 ID 在 `ItemDatabase` 中固定

7. **调整内置消耗品定价**：
   - health_potion: buy=80, sell=40
   - stress_herb: buy=120, sell=60

## 范围边界
- **在范围内**: 装备物品化（4 件装备注册到 ItemDatabase）、商店面板、购买/出售、ShopData 持久化、商品刷新
- **不在范围内**: 随机词缀装备、装备强化/分解、讨价还价、商店等级
- **不在范围内**: 怪物掉落商店（目前是战斗结算中的直接掉落）

## 交付物
1. `Data/ItemDefinition.cs` 的修改（buyPrice/sellPrice）
2. `Data/ItemDatabase.cs` 的修改（InitBuiltins 添加 4 件装备 + 设置价格）
3. `Data/ShopData.cs`
4. `Scripts/UI/UIShopController.cs`
5. `Core/GameManager.cs` 的修改（shopData + 自动创建 UIShopController + 存档）

## 一致性约束
- UI 配色：背景 `0.1f,0.1f,0.1f,0.95f`，购买按钮 `0.2f,0.6f,0.2f`，出售按钮 `0.5f,0.3f,0.2f`
- 所有文本中文（"购买"、"出售"、"金币不足"、"今日无货"）
- B 键只在 WorldMap 响应

## 验证步骤
1. 在世界地图按 B 打开商店，确认看到 6 件商品（含 2 种药水 + 4 件随机）
2. 购买一件商品，确认金币减少、背包增加
3. 出售一件商品，确认金币增加、背包减少
4. 存档后重载，确认商店商品不变
```

---

## Prompt 4：地牢地图与遭遇流程 (Dungeon Map & Encounter Flow)

```
你是一个 Unity C# 游戏开发专家，正在为 Project Chimera 实现地牢地图系统——替代当前"点击战斗→直接战斗"的简单流程，改为选择路线、多节点探索的类桌游地牢地图。

## 模块目标
创建多节点地牢地图，包含战斗/休息/精英/首领节点，玩家选择路径推进，到达首领后结算。

⚠️ **范围削减**：当前版本只做**Phase 1（敌人模板系统 + DungeonSession 基础）+ Phase 2（地图 UI + 核心节点）**。不做法国/宝箱/Exit 节点（标记为 FIXME 即可）。

## 当前状态
- `GameManager.StartBattle()` 用 `Random.Range(0,2)` 选两个硬编码模板之一
- `GameManager.ReturnToWorldMap()` 从战斗返回世界地图
- 无地牢概念，每次战斗独立
- ⚠️ **项目全部代码无 namespace**

## 技术要求

### Phase 1：敌人模板系统 + DungeonSession 基础

1. **在 `GameManager.cs` 中添加敌人模板字典**:
   - `Dictionary<int, List<UnitBattleData>> enemyTemplates`（在 `Start()` 中填充）
   - 将现有的两个模板分别注册为 id=1（哥布林军团）和 id=2（狼群+混编）
   - 添加一个 id=3 简单 Boss 模板（4 个哥布林战士，属性 x1.5，等级 4）
   - `StartBattle()` 改为 `StartBattleWithTemplate(int templateId, bool fromDungeon)`
   - 保留 `StartBattle()` 作为快捷入口（内部调 `StartBattleWithTemplate(0, false)`，0=随机）

2. **创建 `Core/DungeonSession.cs`**:
   - `public int currentFloor = 1`
   - `public bool isInDungeon`
   - `public void EnterDungeon()` — 设置 `isInDungeon = true`，生成第一层地图
   - `public void NextFloor()` — 清空节点，`currentFloor++`，生成新地图
   - `public void ExitDungeon()` — `isInDungeon = false`，保留所有物品/金币/经验

3. **中退出地牢规则**：战斗中 R 键已支持撤退（撤退后返回世界地图，`isInDungeon` 保持 true）。如果玩家希望完全退出地牢，可在世界地图上按 Esc 选择"退出地牢"（`DungeonSession.ExitDungeon()`，保留所有收益）。

### Phase 2：地图生成与 UI

4. **创建 `MapGen/DungeonMapData.cs`**:
   - `enum DungeonNodeType { Battle, Rest, Elite, Boss }`（去掉 Treasure、Exit 以减少范围）
   - `class DungeonNode { int row, col; DungeonNodeType type; bool visited; bool accessible; List<int> nextNodeIndices; int templateId; }`
   - `class DungeonMap { int currentFloor; List<DungeonNode> nodes; DungeonNode currentNode; }`

5. **创建 `MapGen/DungeonGenerator.cs`**:
   - `static DungeonMap GenerateMap(int floor)`
   - 3 行：第 1 行 = 2-3 个 Battle 节点，第 2 行 = 混入 1 个 Rest + 1 个 Elite，第 3 行 = 1 个 Boss + 1 个 Battle
   - 每个节点连接到下一行的 1-2 个节点（随机连线，确保至少一条完整路径）
   - 使用种子 `floor * 1000 + System.DateTime.Now.DayOfYear` 保证当天可重复

6. **创建 `Assets/Scripts/UI/UIDungeonMapController.cs`**:
   - `EnsurePanel()` 模式，全屏面板（不覆盖 BattleScene，而是作为独立场景/叠加 UI）
   - **不使用 emoji**（字体不支持），用 `Image` 组件的 color 区分节点：
     - Battle = `Image.color = new Color(0.8f, 0.2f, 0.2f)` + 白色 "⚔" TextMeshPro
     - Rest = `Image.color = new Color(0.2f, 0.4f, 0.8f)` + 白色 "▼" TextMeshPro
     - Elite = `Image.color = new Color(0.6f, 0.2f, 0.6f)` + 白色 "★" TextMeshPro
     - Boss = `Image.color = new Color(0.9f, 0.6f, 0.1f)` + 白色 "♛" TextMeshPro
   - 节点之间的路径用 `Image` 旋转拉伸为灰色细线
   - 玩家点击 accessible 节点 → 标记 visited → 执行对应逻辑
   - Battle 节点 → `GameManager.StartBattleWithTemplate(templateId, true)`
   - Rest 节点 → 弹出恢复面板（全队 HealHP(30%) + ReduceStress(20)），点击"继续"后回到地图
   - Elite 节点 → 调用 `GameManager.StartBattleWithTemplate`，使用 id=1 但参数标记为精英（属性 x1.3）
   - Boss 节点 → 调用 id=3 模板
   - 打完 Boss → 弹出"地牢通关！"面板，`DungeonSession.ExitDungeon()`

7. **修改 `GameManager.cs`**:
   - `ReturnToWorldMap()` 中：如果 `dungeonSession.isInDungeon && !battleResult.Contains("失败")`，加载地牢地图场景/界面而非世界地图
   - 如果战斗失败（团灭），`DungeonSession.ExitDungeon()` 并返回世界地图（失去战斗收益）

## 范围边界
- **在范围内**: 敌人模板 ID 系统、DungeonSession、3 行地牢地图生成、Battle/Rest/Elite/Boss 节点 UI 和功能、楼层推进
- **不在范围内**: Treasure/Exit 节点、Boss 独特技能和动画、陷阱节点、剧情文本、地图编辑

## 交付物
1. `Core/GameManager.cs` 的修改（enemyTemplates 字典 + StartBattleWithTemplate）
2. `Core/DungeonSession.cs`
3. `MapGen/DungeonMapData.cs`
4. `MapGen/DungeonGenerator.cs`
5. `UI/UIDungeonMapController.cs`
6. `Battle/BattleSetup.cs` 的修改（使用 templateId 敌方数据）

## 一致性约束
- 节点符号使用 TextMeshPro 文本字符（⚔▼★♛），配合 `Image` 背景色
- 路径使用旋转拉伸的 `Image`（宽 2px，灰色 `0.4f,0.4f,0.4f`）
- 所有新增方法在无 dungeonSession 时正常工作（向前兼容）

## 验证步骤
1. 从世界地图"进入地牢"（方案：在 `MainMenu` 或世界地图按 D 键 `DungeonSession.EnterDungeon()`）
2. 确认看到 3 行节点地图，路径连线正确
3. 点击一个 Battle 节点，进入战斗，胜利后返回地牢地图
4. 点击 Rest 节点，确认 HP/压力恢复
5. 击败 Boss 节点，确认返回世界地图
```

---

## Prompt 5：音效与视觉反馈系统 (Audio & VFX)

```
你是一个 Unity C# 游戏开发专家，正在为 Project Chimera 实现音效与视觉反馈系统。

## 模块目标
创建 AudioManager 和 VFX 管理器，为战斗中的攻击、命中、暴击、治疗、压力、死亡之门等事件提供音频和视觉反馈。

## 当前状态
- `BattleManager` 有受击动画协程（`HitFeedbackEffect` — 跳跃 + 下落）
- `DamagePopup` 预制体 + 脚本已存在
- `BattleLog.Add()` 输出文本日志
- **无音频系统**、**无视觉特效**
- ⚠️ **项目全部代码无 namespace**
- ⚠️ 如果 Prompt 1（战斗公式）已执行，`DealDamage` 被修改过；集成音效时注意不要覆盖已有逻辑

## 技术要求

1. **创建 `Assets/Scripts/Core/AudioManager.cs`**:
   - `MonoBehaviour` 单例（`DontDestroyOnLoad`），`public static AudioManager Instance`
   - `AudioSource bgmSource` — 循环
   - `AudioSource sfxSource` — 播放一次
   - `Dictionary<string, AudioClip> clips` — 从 `Resources/Audio/` 文件夹加载所有音频文件（`Resources.LoadAll<AudioClip>("Audio")`）
   - `void PlayBGM(string key, float volume = 0.4f)`：
     - 如果 `clips` 中无此 key，`Debug.Log($"[Audio] BGM: {key}")` 并 return
     - 否则 `bgmSource.clip = clips[key]; bgmSource.volume = volume; bgmSource.Play()`
   - `void PlaySFX(string key, float volume = 0.8f)`：
     - 如果无此 key，`Debug.Log` 并 return
     - 否则 `sfxSource.PlayOneShot(clips[key], volume)`

2. **创建 `Assets/Scripts/Core/AudioKeys.cs`**（静态常量类）:
   ```csharp
   public static class AudioKeys
   {
       // SFX
       public const string SWORD_HIT = "sfx_sword_hit";
       public const string ARROW_HIT = "sfx_arrow_hit";
       public const string MAGIC_HIT = "sfx_magic_hit";
       public const string CRIT = "sfx_crit";
       public const string MISS = "sfx_miss";
       public const string HEAL = "sfx_heal";
       public const string SHIELD = "sfx_shield";
       public const string STRESS = "sfx_stress";
       public const string BREAKDOWN = "sfx_breakdown";
       public const string VIRTUE = "sfx_virtue";
       public const string DEATHS_DOOR = "sfx_deaths_door";
       public const string VICTORY = "sfx_victory";
       public const string DEFEAT = "sfx_defeat";
       public const string RETREAT = "sfx_retreat";
       public const string LEVEL_UP = "sfx_level_up";
       // BGM
       public const string BGM_BATTLE = "bgm_battle";
       public const string BGM_BOSS = "bgm_boss";
       public const string BGM_MAP = "bgm_map";
       public const string BGM_MENU = "bgm_menu";
   }
   ```

3. **创建 `Assets/Scripts/VFX/VFXManager.cs`**:
   - **纯静态类**（非 MonoBehaviour），所有方法 `public static`
   - `static CoroutineRunner _runner` — 用一个隐藏的 `MonoBehaviour` GameObject 来跑协程（或直接在 `BattleManager` 中调 `StartCoroutine`）
   - `static void ScreenShake(float intensity = 0.2f, float duration = 0.15f)`：
     - 保存 `Camera.main.transform.position`
     - 每帧随机偏移 `originalPos + Random.insideUnitSphere * intensity`（z 轴归零）
     - duration 后恢复
   - `static void FlashDamage(UnitData target)`：
     - `var sr = target.GetComponent<SpriteRenderer>()`
     - `sr.color = Color.white`，0.1 秒后恢复 `originalColors[target]`（需从 BattleManager 传入或自行缓存）
   - `static void FlashHeal(UnitData target)`：同上，颜色改为 Color.green，0.15 秒
   - `static void PlayHitEffect(Vector3 position)`：
     - 创建一个 `GameObject("HitEffect")`，添加 `SpriteRenderer`
     - 创建一个 `1x1` 白色 **Texture2D**（`new Texture2D(1,1)`，setpixel(0,0,Color.white)），赋值给 SpriteRenderer
     - 颜色 `Color.yellow`，缩放 `Vector3(1,1,1)*0.5`，1 秒内渐隐 + 缩放到 0 后 Destroy
     - 这是**纯代码生成的 placeholder**，不依赖任何外部资源

4. **在 `BattleManager.cs` 中集成**:
   - `DealDamage()` 中：
     - 命中时：`AudioManager.Instance?.PlaySFX(AudioKeys.SWORD_HIT)` + `VFXManager.ScreenShake(0.15f)` + `VFXManager.FlashDamage(target)`
     - 暴击时：额外 `AudioManager.Instance?.PlaySFX(AudioKeys.CRIT)` + `VFXManager.ScreenShake(0.3f, 0.25f)`
     - 未命中时：`AudioManager.Instance?.PlaySFX(AudioKeys.MISS)`
   - 治疗时（`HealCommand.Execute` 中）：`AudioManager.Instance?.PlaySFX(AudioKeys.HEAL)` + `VFXManager.FlashHeal(target)`
   - 护盾时（`ShieldCommand.Execute` 中）：`AudioManager.Instance?.PlaySFX(AudioKeys.SHIELD)`
   - 压力增加时：`AudioManager.Instance?.PlaySFX(AudioKeys.STRESS)`
   - 死亡之门时：`AudioManager.Instance?.PlaySFX(AudioKeys.DEATHS_DOOR)`
   - 胜利时：`AudioManager.Instance?.PlaySFX(AudioKeys.VICTORY)` → 2 秒后 `AudioManager.Instance?.PlayBGM(AudioKeys.BGM_MAP)`
   - 失败时：`AudioManager.Instance?.PlaySFX(AudioKeys.DEFEAT)`

5. **在 `TurnManager.cs` 中集成**:
   - 回合开始时如果 `isPlayerTurn`：`AudioManager.Instance?.PlaySFX("sfx_ui_tick", 0.5f)`

6. **在 `GameManager.Start()` 中初始化**:
   - `if (FindObjectOfType<AudioManager>() == null)` 时创建
   - 初始播放 `AudioManager.Instance?.PlayBGM(AudioKeys.BGM_MAP)`

## 范围边界
- **在范围内**: AudioManager 架构、VFXManager（ScreenShake + Flash + HitEffect）、关键事件插入、纯代码 HitEffect 占位
- **不在范围内**: 实际的 .wav/.ogg 文件、粒子系统、Spine 动画事件、音量设置 UI、3D 音频

## 交付物
1. `Assets/Scripts/Core/AudioManager.cs`
2. `Assets/Scripts/Core/AudioKeys.cs`
3. `Assets/Scripts/VFX/VFXManager.cs`
4. `BattleManager.cs` 中 DealDamage 等处的音效/特效集成
5. `TurnManager.cs` 中回合提示音
6. `GameManager.cs` 中的初始化

## 一致性约束
- 所有 `AudioManager` 调用加 `?.` 空安全（`AudioManager.Instance?.PlaySFX(...)`）
- Resources 中无音频文件时不报错，只输出 `[Audio] 播放: {key}`
- ScreenShake 只移动 Camera，不影响 ScreenSpaceOverlay UI
- VFXManager 为纯静态类，协程通过隐藏的 `CoroutineRunner` MonoBehaviour 驱动
- 音量默认值：BGM=0.4f, SFX=0.8f

## 验证步骤
1. 进入战斗，确认控制台输出 `[Audio] 播放: bgm_battle`（无实际音频文件时）
2. 普攻确认输出 `[Audio] 播放: sfx_sword_hit`
3. 暴击确认 `[Audio] 播放: sfx_crit` + 屏幕抖动可见
4. 治疗确认 `[Audio] 播放: sfx_heal` + 角色闪绿
```
