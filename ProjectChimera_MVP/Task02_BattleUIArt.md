# Task 02: 战斗UI美术全面替换与状态效果图标系统

## 功能描述

将《暗黑地牢1》的 UI 美术资源全面导入 Unity 项目《Project Chimera》，替换当前战斗中所有由代码动态生成的纯色占位 UI 元素。包括：HP血条、压力条、选中光圈、目标箭头、状态效果图标、技能栏背景、战斗日志背景等。同时实现一个状态效果（Buff/Debuff）图标浮动显示系统。

## 技术栈要求

- Unity 2022.3 LTS
- C#（运行时脚本 + 编辑器辅助）
- Unity UI（uGUI）— Canvas、Image、RectTransform
- Spine Unity Runtime（用于部分特效）
- TextMeshPro（用于文字显示）

## 输入数据规范

### 源资源路径（暗黑地牢1安装目录）
```
E:\xlyp\DD.v26186\overlays\          # UI状态图标、目标标记、火炬等
E:\xlyp\DD.v26186\panels\            # UI面板背景（角色面板、地图面板等）
E:\xlyp\DD.v26186\heroes\<name>\     # 英雄技能图标、装备图标、肖像
```

### 关键资源清单

**状态效果图标（overlays/）**
- `tray_bleed.png` — 流血
- `tray_stun.png` — 眩晕
- `tray_tag.png` — 标记
- `tray_guard.png` — 保护
- `tray_buff.png` / `tray_buff_plus.png` — 增益
- `tray_debuff.png` — 减益
- `tray_afflicted.png` — 崩溃/折磨
- `tray_virtued.png` — 美德
- `target_1.png` ~ `target_4.png` — 目标编号
- `selected_1.png` ~ `selected_4.png` — 选中标记

**面板背景（panels/）**
- `panel_hero.png` — 角色信息面板
- `panel_monster.png` — 怪物信息面板
- `panel_banner.png` — 顶部横幅
- `retreat_button.png` — 撤退按钮
- `side_decor.png` — 侧边装饰

**英雄资源（heroes/<name>/）**
- `*_portrait_roster.png` — 英雄肖像（用于HUD头像）
- `*.ability.*.png` — 技能图标
- `icons_equip/eqp_weapon_*.png` — 武器图标
- `icons_equip/eqp_armour_*.png` — 护甲图标

### 目标路径（Unity项目）
```
Assets/Resources/UI/Icons/Status/       # 状态效果图标
Assets/Resources/UI/Icons/Skills/       # 技能图标
Assets/Resources/UI/Icons/Equip/        # 装备图标
Assets/Resources/UI/Panels/             # 面板背景
Assets/Resources/UI/Portraits/          # 英雄肖像
Assets/Resources/UI/Overlays/           # 目标箭头、选中标记
```

## 实现步骤

### 1. 资源批量复制与重命名

创建编辑器工具 `DD1UIArtImporter.cs`（Menu: Tools > DD1 UI Art Importer）：

1. 扫描 `overlays/` 目录，按前缀分类：
   - `tray_*.png` → `Assets/Resources/UI/Icons/Status/`
   - `target_*.png` / `selected_*.png` → `Assets/Resources/UI/Overlays/`
   - `panel_*.png` / `retreat_button.png` / `side_decor.png` → `Assets/Resources/UI/Panels/`
2. 扫描 `heroes/<name>/` 下的 `*_portrait_roster.png` → `Assets/Resources/UI/Portraits/`
3. 扫描 `heroes/<name>/` 下的 `*.ability.*.png` → `Assets/Resources/UI/Icons/Skills/`
4. 扫描 `heroes/<name>/icons_equip/` → `Assets/Resources/UI/Icons/Equip/`

所有图片复制时保持文件名小写，统一添加前缀避免冲突（如 `dd1_tray_bleed.png`）。

### 2. 替换代码生成的 HPBar 和 StressBar

当前 `BattleSetup.cs` 中 `CreateHPBar()` 和 `CreateStressBar()` 使用代码生成的纯色方块。修改为：

1. 创建 `HPBarDD1.cs` 和 `StressBarDD1.cs` 组件（或修改现有的 `HPBarFollower.cs` / `StressBarFollower.cs`）
2. 使用 `dd1_panel_hero.png` 或 `dd1_panel_monster.png` 作为血条背景底板
3. 血条填充使用 Unity UI 的 `Image.fillAmount`（保留现有逻辑），但填充色使用图片而非纯色
4. 压力条使用 `dd1_panel_banner.png` 作为背景
5. 在血条左侧添加英雄肖像 `Image`（圆形遮罩），从 `Resources/UI/Portraits/` 动态加载

### 3. 替换选中光圈和目标箭头

修改 `BattleManager.cs`：

1. `CreateTargetArrow()`：
   - 当前使用代码生成的 `TextMeshProUGUI` 显示 "▼"
   - 替换为 `dd1_target_1.png` ~ `dd1_target_4.png` 的 Sprite 序列
   - 根据目标在队伍中的位置（rank 0-3）显示对应编号图标
2. `UpdateTargetHighlight()` 中的 `selectCircle`：
   - 当前使用代码生成的 SpriteRenderer 纯色圆
   - 替换为 `dd1_selected_1.png` ~ `dd1_selected_4.png` 的 Sprite
   - 使用不同颜色区分玩家（黄色）和敌人（青色）——通过 UI Image 的 color 属性叠加

### 4. 实现状态效果图标系统 `StatusIconManager.cs`

在 `Assets/Scripts/Battle/` 下创建新组件，挂载到 BattleManager 或 Canvas 上：

1. **数据结构**：
   ```csharp
   [System.Serializable]
   public class StatusIconEntry
   {
       public StatusType statusType;
       public Sprite iconSprite;
       public Color tintColor;
   }
   ```

2. **动态生成图标**：
   - 监听 `UnitData.OnStatusApplied` 和 `OnStatusRemoved` 事件（如不存在则添加事件）
   - 当单位获得状态时，在其头顶（World Space Canvas 或 Screen Space Overlay Canvas）生成一个图标 GameObject
   - 图标横向排列，最多显示 4 个，超出时显示 "+N" 文本

3. **图标位置跟随**：
   - 使用 `RectTransform` 的 `anchoredPosition` 跟随单位的世界坐标
   - 通过 `Camera.main.WorldToScreenPoint()` 转换坐标
   - 图标堆叠在血条上方约 20 像素处

4. **状态效果映射表**：
   | StatusType | 图标文件名 | 叠加色 |
   |---|---|---|
   | Bleed | `dd1_tray_bleed.png` | 红色 |
   | Stun | `dd1_tray_stun.png` | 紫色 |
   | Mark | `dd1_tray_tag.png` | 橙色 |
   | Taunt | `dd1_tray_guard.png` | 蓝色 |
   | Protected | `dd1_tray_guard.png` | 青色 |
   | Berserk | `dd1_tray_buff.png` | 深红 |
   | Enlightened | `dd1_tray_virtued.png` | 金色 |

### 5. 替换技能栏背景

修改 `UISkillBarController.cs`：

1. 技能按钮当前是纯色方块 + TMP 文字
2. 添加背景底板 `dd1_panel_banner.png` 或 `dd1_side_decor.png`
3. 技能图标从 `Resources/UI/Icons/Skills/` 动态加载
4. 冷却中的技能显示灰色遮罩 + 回合数文字

## 输入输出规范

### 输入
- 源目录：`E:\xlyp\DD.v26186\overlays\`、`panels\`、`heroes\`
- 目标目录：`Assets/Resources/UI/...`
- 运行时：通过 `Resources.Load<Sprite>(path)` 加载

### 输出
- 导入的 PNG 资源文件
- 动态生成的 UI GameObject（状态图标、血条、目标箭头）
- Console 日志：记录资源加载状态

## 错误处理要求

1. **资源缺失**：如果 `Resources.Load<Sprite>()` 返回 null，显示一个默认的灰色方块，并记录 Warning，不要崩溃
2. **Canvas 缺失**：如果场景中找不到 Canvas，自动创建一个 `ScreenSpaceOverlay` Canvas（参考现有 `ShowFloatingText` 逻辑）
3. **Camera.main 缺失**：所有需要 `WorldToScreenPoint` 的地方必须判空
4. **状态图标过多**：如果单位身上的状态超过 4 个，第 5 个及以后折叠为 "+N" 文本图标
5. **内存管理**：状态图标在战斗结束或单位死亡时自动销毁，避免内存泄漏

## 验收标准

- [ ] 编辑器工具能一键导入所有 UI 美术资源到正确目录
- [ ] 战斗中血条和压力条显示为带背景图片的样式，而非纯色方块
- [ ] 选中目标时显示 DD1 风格的目标箭头（带编号 1-4）
- [ ] 单位获得状态效果（如 Bleed、Stun）时，头顶出现对应图标
- [ ] 状态图标在状态消失时自动移除
- [ ] 技能栏按钮有背景底板和技能图标
- [ ] 所有 UI 元素在 1920x1080 和 1280x720 分辨率下显示正常（锚点设置正确）
- [ ] 战斗中如果某个图片资源缺失，显示占位图但不崩溃
- [ ] 代码注释使用中文
