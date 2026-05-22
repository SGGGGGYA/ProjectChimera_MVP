# 《Project Chimera》MVP行动路线图与关键决策记录

**文档版本**：V1.0  
**创建日期**：2026-05-05  
**状态**：开发启动指南  
**关联文档**：
- 技术规格书 V2.0
- 核心数值框架文档 V1.0（已修正）
- 数组设计重构报告 V1.0
- 英雄与职业设计文档 V1.0（架构师评审版）
- 数值体系讨论总结

---

## 关键决策记录（开发前必须确认）

### 决策1：技术栈选择

| 选项 | 优势 | 劣势 | 建议 |
|:---|:---|:---|:---|
| **Unity + Visual Scripting** | 资产商店丰富、可视化编程成熟、文档完善 | 安装包大、授权费用（年收入超10万美元时） | **推荐** |
| **Godot 4.x** | 开源免费、轻量、信号系统优秀 | 资产商店较小、学习资源相对较少 | 备选 |

**最终选择**：Unity + Visual Scripting（内置，无需额外购买PlayMaker）
- **理由**：个人项目无需担心授权费；Visual Scripting已内置在Unity 2021.2+；Asset Store有大量免费2D素材可用于原型。

### 决策2：MVP内容范围（严格限制）

根据架构师评审报告的建议，MVP阶段**只保留2个职业**：
- **战士**（坦克/控制）
- **游侠**（输出/标记）

**首发技能**（各3个，共6个技能）：
| 职业 | 技能1 | 技能2 | 技能3（终极） |
|:---|:---|:---|:---|
| 战士 | 盾牌猛击（伤害+眩晕） | 嘲讽（强制攻击） | 援护（替队友承伤） |
| 游侠 | 精准射击（高伤害） | 标记（增伤Debuff） | 弹幕覆盖（群体伤害） |

**刻意砍掉的内容**（后续版本再加入）：
- 狂战士（自残机制复杂，需额外UI提示）
- 学者（压力双刃剑机制，需完整的压力系统支撑）
- 领袖战斗定位（MVP中领袖仅在剧情中出现，不参战）
- 科技树/文化树（MVP中建筑仅解锁功能，无科技依赖）
- 命名系统（MVP中等级上限降低到5级，命名延后）

### 决策3：数值简化（MVP专用）

MVP阶段使用**简化数值**，避免过度设计：

| 系统 | 完整版 | MVP简化版 |
|:---|:---|:---|
| 等级上限 | 冒险者10级/领袖15级 | 冒险者5级 |
| 经验曲线 | 100 * 等级^1.8 | 线性：100/200/400/800/1600 |
| 属性点 | 3点/级 | 2点/级 |
| 怪癖系统 | 随机获得，正负混合 | 仅正面怪癖，且固定3个 |
| 压力系统 | 完整的美德/折磨判定 | 仅累积，满100后强制回城 |
| 装备系统 | 多部位+随机属性 | 仅武器+防具，固定属性 |
| 地形天赋 | 4种地形各职业不同 | 仅森林/平原2种，固定加成 |

**MVP伤害公式**（进一步简化，去掉防御效率）：
```
最终伤害 = max(1, (技能基础伤害 + 武器攻击力) * 力量系数 - 目标防御)
力量系数 = 1 + (STR - 5) * 0.05
```
防御效率在MVP中暂不实现，用固定DEF减免。

### 决策4：数据结构约定

**单位数据结构**（MVP简化版）：
```csharp
public class UnitData
{
    public string unitId;
    public string unitName;
    public UnitClass unitClass; // Warrior, Ranger
    public int level;
    public int currentExp;
    
    // 核心属性 (Primary)
    public int VIT; // 体质
    public int STR; // 力量
    public int AGI; // 敏捷
    public int INT; // 智力
    
    // 派生属性 (Derived，运行时计算)
    public int MaxHP => VIT * 5 + classBaseHP;
    public int CurrentHP;
    public int SPD => AGI * 2 + classBaseSPD;
    public int ACC => 80 + AGI * 2;
    public int DOD => AGI;
    public int DEF; // 来自装备
    
    // 战斗状态
    public int currentStress;
    public List<string> quirks; // 怪癖ID列表
    public List<string> skills; // 技能ID列表
    public Equipment weapon;
    public Equipment armor;
}
```

**技能命令队列结构**（MVP版）：
```csharp
public class SkillData
{
    public string skillId;
    public string skillName;
    public List<Command> commands; // 命令队列
}

public class Command
{
    public CommandType type; // DealDamage, ApplyStatus, MoveTarget, etc.
    public Dictionary<string, object> parameters;
}
```

---

## 行动路线图

### 第〇步：工具准备（预计1-2天）

**任务清单**：
- [ ] 下载并安装 Unity Hub
- [ ] 安装 Unity 2022.3 LTS（长期支持版，稳定）
- [ ] 创建新项目，选择2D模板
- [ ] 熟悉Unity界面：Scene视图、Game视图、Inspector、Hierarchy
- [ ] **核心任务**：用Visual Scripting创建一个"点击按钮→打印日志"的流程
  - 创建一个UI按钮
  - 添加Visual Script组件
  - 连接"On Click"事件到"Debug Log"节点
  - 运行并验证

**验证标准**：
> 点击按钮后，Console窗口显示"Hello, Project Chimera!"

**学习资源**：
- Unity官方文档：Visual Scripting入门
- B站/YouTube搜索："Unity Visual Scripting 2D游戏"

---

### 第一步：搭建骨架（预计2-3天）

**任务清单**：
- [ ] 创建3个Scene文件：
  - `WorldMap`（世界地图）
  - `BattleScene`（战斗场景）
  - `GameManager`（全局管理器，单例模式）
- [ ] 在WorldMap中：
  - 创建一个6x6的网格（先用Square Sprite代替六边形）
  - 实现点击检测：点击格子，高亮显示
  - 创建一个"小队"图标（圆形Sprite）
  - 实现小队移动：点击格子→小队平滑移动到该位置
- [ ] 在GameManager中：
  - 实现单例模式（确保全局只有一个）
  - 创建`GameState`枚举：WorldMap, Battle, Menu
  - 实现场景切换方法：`LoadScene(string sceneName)`
  - 实现数据传递：用`DontDestroyOnLoad`保持数据

**关键代码/节点参考**：
```csharp
// GameManager.cs（单例模式）
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState currentState;
    public List<UnitData> playerTeam; // 我方小队数据
    public List<UnitData> enemyTeam;  // 敌方数据（战斗时填充）
    
    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    public void StartBattle(List<UnitData> enemies)
    {
        enemyTeam = enemies;
        currentState = GameState.Battle;
        SceneManager.LoadScene("BattleScene");
    }
}
```

**验证标准**：
> 1. 能在WorldMap中点击格子移动小队
> 2. 按B键（测试用）触发战斗，切换到BattleScene
> 3. BattleScene中能读取并显示我方单位名称

---

### 第二步：跑通场景切换（第一个里程碑，预计2-3天）

**这是架构的基石，必须完全跑通。**

**任务清单**：
- [ ] 在WorldMap中创建"遭遇触发"机制：
  - 特定格子标记为"战斗格"
  - 小队移动到战斗格→弹出"遭遇"UI面板
  - 面板显示敌方信息（硬编码的假数据）
- [ ] 实现数据打包：
  - 创建`BattleStartData`结构
  - 将双方单位数据存入GameManager
- [ ] BattleScene初始化：
  - 读取GameManager中的数据
  - 在屏幕上打印"战士 等级3 已加载"
  - 摆放4个我方站位方块、2个敌方站位方块
- [ ] 实现"返回"按钮：
  - 点击后回到WorldMap
  - WorldMap恢复小队位置

**MVP假数据**（硬编码，用于测试）：
```csharp
// 在GameManager中预置
void CreateTestData()
{
    playerTeam = new List<UnitData>
    {
        new UnitData {
            unitName = "战士",
            unitClass = UnitClass.Warrior,
            level = 3,
            VIT = 8, STR = 10, AGI = 5, INT = 3,
            CurrentHP = 80, // MaxHP = 8*5 + 40 = 80
            skills = new List<string>{"shield_bash", "taunt", "guard"}
        },
        new UnitData {
            unitName = "游侠",
            unitClass = UnitClass.Ranger,
            level = 3,
            VIT = 5, STR = 6, AGI = 10, INT = 4,
            CurrentHP = 55, // MaxHP = 5*5 + 30 = 55
            skills = new List<string>{"aimed_shot", "mark", "barrage"}
        }
    };
}
```

**验证标准**：
> 完整流程：WorldMap移动→触发遭遇→看到敌方信息→点击"进入战斗"→BattleScene显示双方单位→点击"返回"→回到WorldMap

---

### 第三步：实现战斗原型（第二个里程碑，预计5-7天）

**这是最难的一步，也是核心玩法的验证。**

#### 3.1 初始化战场（第1-2天）
- [ ] 根据传入数据，在屏幕上摆放站位：
  - 我方4个位置（前排2、后排2），用不同颜色方块表示
  - 敌方2个位置（MVP只放2个敌人），用红色方块
- [ ] 每个单位显示：名称、HP条（简易Slider）
- [ ] 实现"行动条"UI：显示当前轮到谁

#### 3.2 实现行动顺序（第2-3天）
- [ ] 基于SPD计算行动顺序：
  ```
  行动条填充 = SPD * Random.Range(0.8, 1.2)
  每回合累积，达到100时行动
  ```
- [ ] 实现回合循环：
  1. 计算所有单位行动条
  2. 找出最高的单位→标记为"当前行动"
  3. 等待玩家选择技能和目标
  4. 执行技能→更新状态
  5. 检查胜负条件
  6. 返回步骤1

#### 3.3 实现第一个原子命令：DealDamage（第3-4天）
- [ ] 创建Command基类和DealDamageCommand
- [ ] 实现伤害计算（使用MVP简化公式）
- [ ] 实现目标选择：点击敌方单位→高亮→确认
- [ ] 执行后：目标HP减少→更新HP条→播放简单动画（闪烁）

#### 3.4 实现战士的"盾牌猛击"（第4-5天）
- [ ] 命令队列：DealDamage → ApplyStatus(Stun)
- [ ] Stun效果：下回合无法行动
- [ ] 实现状态系统：Buff/Debuff的添加、计时、移除

#### 3.5 实现游侠的"标记射击"（第5-6天）
- [ ] 命令队列：DealDamage → ApplyStatus(Marked)
- [ ] Marked效果：受到的伤害+20%（持续2回合）
- [ ] 验证"标记→引爆"循环：游侠标记→战士猛击→额外伤害

#### 3.6 实现胜利/失败条件（第6-7天）
- [ ] 每回合检查：敌方全灭→胜利；我方全灭→失败
- [ ] 胜利后：弹出结算面板，显示获得的经验
- [ ] 点击"返回地图"→回到WorldMap

**MVP技能数据**（硬编码）：
```csharp
public static class SkillDatabase
{
    public static Dictionary<string, SkillData> Skills = new Dictionary<string, SkillData>
    {
        ["shield_bash"] = new SkillData
        {
            skillName = "盾牌猛击",
            commands = new List<Command>
            {
                new Command {
                    type = CommandType.DealDamage,
                    parameters = new Dictionary<string, object> {
                        ["baseDamage"] = 15,
                        ["attrCoeff"] = 1.0, // STR系数
                        ["damageType"] = DamageType.Physical
                    }
                },
                new Command {
                    type = CommandType.ApplyStatus,
                    parameters = new Dictionary<string, object> {
                        ["statusId"] = "stun",
                        ["duration"] = 1
                    }
                }
            }
        },
        ["aimed_shot"] = new SkillData
        {
            skillName = "精准射击",
            commands = new List<Command>
            {
                new Command {
                    type = CommandType.DealDamage,
                    parameters = new Dictionary<string, object> {
                        ["baseDamage"] = 20,
                        ["attrCoeff"] = 1.2, // AGI系数
                        ["damageType"] = DamageType.Physical
                    }
                }
            }
        },
        ["mark"] = new SkillData
        {
            skillName = "标记",
            commands = new List<Command>
            {
                new Command {
                    type = CommandType.ApplyStatus,
                    parameters = new Dictionary<string, object> {
                        ["statusId"] = "marked",
                        ["duration"] = 2
                    }
                }
            }
        }
    };
}
```

**验证标准**：
> 1. 能完成一场完整的战斗：行动顺序→选择技能→选择目标→执行→下一回合
> 2. 战士的盾牌猛击能造成伤害并眩晕敌人
> 3. 游侠的标记能让后续伤害增加
> 4. 一方全灭后正确触发结算

---

### 第四步：验收与反思（预计1天）

**当你亲手操作了一次从移动到战斗到结算的完整循环后，回答以下问题：**

#### 体验问题
1. **战士和游侠的配合感觉有趣吗？**
   - 标记→猛击的循环是否让你感到"策略感"？
   - 还是感觉拖沓、多余？

2. **标记→引爆的循环在实际操作中是否流畅？**
   - 需要点击多少次？是否直观？
   - UI是否清晰展示了"已标记"状态？

3. **战斗节奏如何？**
   - 一场战斗持续多久？（目标：3-5分钟）
   - 是否感到无聊或过于紧张？

#### 技术问题
4. **命令队列系统在实现时遇到了什么困难？**
   - 目标失效处理（如目标已死亡）是否顺畅？
   - 命令顺序是否严格按队列执行？

5. **Visual Scripting是否够用？**
   - 哪些逻辑必须用代码写？
   - 是否考虑全部用C#重写？

#### 数值问题
6. **哪些数值需要立即调整？**
   - 伤害是否太高/太低？（目标：3-4下击杀同级敌人）
   - 速度差异是否明显影响行动顺序？
   - HP是否太少/太多？

**记录格式**：
```
## MVP验收记录 - [日期]

### 体验反馈
- [ ] 战士+游侠配合：有趣/一般/无聊
- [ ] 标记循环流畅度：流畅/一般/卡顿
- [ ] 战斗节奏：太快/合适/太慢

### 技术问题
- 遇到的Bug：...
- 需要重构的部分：...

### 数值调整
- 伤害：当前XX，建议改为XX，原因...
- HP：当前XX，建议改为XX，原因...
- 速度：当前XX，建议改为XX，原因...

### 下一步决策
- [ ] 继续完善MVP（添加压力系统）
- [ ] 直接开始V0.5（添加更多职业/怪物）
- [ ] 重构架构（如命令队列系统需要重新设计）
```

---

## 风险预警与应对

| 风险 | 可能性 | 应对方案 |
|:---|:---|:---|
| Visual Scripting无法满足复杂逻辑 | 中 | 准备Plan B：关键逻辑用C#脚本写，Visual Scripting只做简单事件触发 |
| 场景切换时数据丢失 | 高 | 严格测试GameManager的`DontDestroyOnLoad`，每次切换后检查数据完整性 |
| 战斗系统过于复杂，7天做不完 | 高 | 进一步简化：先只做"点击敌方→造成伤害"，跳过行动条和命令队列 |
| 没有美术素材，原型太丑影响判断 | 中 | 使用Unity Asset Store免费2D素材包，或纯色方块+文字标签 |
| 数值不平衡导致战斗无趣 | 中 | MVP阶段随时调整硬编码数值，验收阶段重点测试 |

---

## 附录：快速参考

### 项目文件结构建议
```
Assets/
├── Scenes/
│   ├── WorldMap.unity
│   ├── BattleScene.unity
│   └── GameManager.unity
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   └── SceneLoader.cs
│   ├── Data/
│   │   ├── UnitData.cs
│   │   ├── SkillData.cs
│   │   └── Command.cs
│   ├── Battle/
│   │   ├── BattleManager.cs
│   │   ├── TurnManager.cs
│   │   └── CommandExecutor.cs
│   └── Map/
│       ├── GridManager.cs
│       └── PlayerSquad.cs
├── Resources/
│   └── Prefabs/
│       ├── UnitPrefab.prefab
│       └── SkillButton.prefab
└── VisualScripts/
    └── UIEvents.asset
```

### 关键数值速查（MVP版）

| 参数 | 值 | 说明 |
|:---|:---|:---|
| 战士基础HP | 40 + VIT*5 | 1级约65HP |
| 游侠基础HP | 30 + VIT*5 | 1级约55HP |
| 哥布林HP | 40 | 1级标准敌人 |
| 哥布林DEF | 5 | 减免约5点伤害 |
| 战士盾牌猛击 | 15 + STR*1.0 | 1级约25伤害 |
| 游侠精准射击 | 20 + AGI*1.2 | 1级约32伤害 |
| 标记增伤 | +20% | 乘法叠加 |
| 眩晕持续 | 1回合 | 跳过下次行动 |
| 升级经验 | 100/200/400/800/1600 | 线性增长 |

---

*本文档为开发启动指南。所有时间节点为预估，实际进度取决于每日投入时间。建议每天至少投入2小时，预计总耗时2-3周完成MVP原型。*
