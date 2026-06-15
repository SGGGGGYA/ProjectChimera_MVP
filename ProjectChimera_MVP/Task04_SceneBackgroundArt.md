# Task 04: 场景背景与主菜单美术集成

## 功能描述

将《暗黑地牢1》的场景背景、主菜单美术、地图节点图标、营地/休息事件图等资源导入 Unity 项目《Project Chimera》，替换当前所有由代码生成的纯色/占位背景。涵盖：主菜单背景、世界地图背景、地牢探索背景、战斗场景背景、营地休息画面、以及各种场景过渡效果。

## 技术栈要求

- Unity 2022.3 LTS
- C#（运行时脚本）
- Unity UI（uGUI）— Image、RawImage、Canvas
- SpriteRenderer（2D场景背景）
- 场景管理（SceneManager、DontDestroyOnLoad）

## 输入数据规范

### 源资源路径（暗黑地牢1安装目录）
```
E:\xlyp\DD.v26186\campaign\town\             # 城镇场景背景
E:\xlyp\DD.v26186\campaign\town\sky\        # 天空背景
E:\xlyp\DD.v26186\fe_flow\                   # 主菜单、UI背景
E:\xlyp\DD.v26186\fx\titles\                 # 标题画面
E:\xlyp\DD.v26186\props\shared\              # 营地火堆等共享道具
E:\xlyp\DD.v26186\monsters\<name>\           # 怪物战斗背景（如有）
E:\xlyp\DD.v26186\overlays\                  # 火炬、UI装饰
```

### 关键资源清单

**主菜单与UI背景（fe_flow/）**
- `title_bg.png` — 主菜单背景（暗黑地牢标志性的庄园夜景）
- `title_house.png` — 主菜单前景房屋
- `start_button.png` — 开始游戏按钮
- `settings.button.png` — 设置按钮
- `demo_splash.png` — 启动画面
- `dlc.background.png` / `ugc.background.png` — 面板背景

**城镇/世界地图背景（campaign/town/）**
- `town_bg.png` — 城镇全景背景
- `sky/sky01.png` / `sky02.png` — 天空层（可滚动视差）
- `ruins.png` — 废墟背景（用于特定地牢类型）

**特效与氛围（fx/ 和 overlays/）**
- `torch.sprite.png` / `torch.sprite.atlas` — 火炬动画（Spine）
- `titles/titles.sprite.png` — 标题特效
- `campfire_flame.png` — 营地火堆

**共享道具（props/shared/）**
- `campfire.png` — 营地火堆静态图

### 目标路径（Unity项目）
```
Assets/Resources/Backgrounds/Menu/          # 主菜单背景
Assets/Resources/Backgrounds/Town/          # 城镇/世界地图背景
Assets/Resources/Backgrounds/Dungeon/       # 地牢背景
Assets/Resources/Backgrounds/Camp/          # 营地背景
Assets/Resources/Backgrounds/Battle/        # 战斗背景
Assets/Resources/Effects/Torch/             # 火炬特效
```

## 实现步骤

### 1. 资源批量复制与分类

创建编辑器工具 `DD1BackgroundImporter.cs`（Menu: Tools > DD1 Background Importer）：

1. 扫描并复制：
   - `fe_flow/title_bg.png` + `title_house.png` → `Assets/Resources/Backgrounds/Menu/`
   - `fe_flow/start_button.png` + `settings.button.png` → `Assets/Resources/UI/Menu/`
   - `campaign/town/town_bg.png` → `Assets/Resources/Backgrounds/Town/`
   - `campaign/town/sky/sky01.png` + `sky02.png` → `Assets/Resources/Backgrounds/Town/`
   - `campaign/town/ruins.png` → `Assets/Resources/Backgrounds/Dungeon/`
   - `props/shared/campfire.png` → `Assets/Resources/Backgrounds/Camp/`
   - `fx/torch/torch.sprite.*` → `Assets/Resources/Effects/Torch/`

2. 所有图片导入设置：
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single（背景图）/ Multiple（如有图集）
   - Pixels Per Unit: 100
   - Compression: None（保持像素清晰）

### 2. 主菜单场景背景替换

修改 `Assets/Scripts/UI/MainMenu.cs` 或创建 `MenuBackgroundController.cs`：

1. 在 MainMenu Canvas 下添加背景层结构：
   ```
   MenuCanvas (ScreenSpaceOverlay)
   └── BackgroundLayer (GameObject)
       ├── BG_Image (Image) — 显示 title_bg.png，铺满全屏
       ├── House_Image (Image) — 显示 title_house.png，底部居中
       └── Torch_Effect (Spine or Particle) — 火炬闪烁效果
   ```

2. 使用 `Image` 组件的 `preserveAspect = false`，设置 `anchorMin=(0,0), anchorMax=(1,1)` 实现全屏拉伸
3. 添加简单的视差效果：鼠标移动时，`House_Image` 以 0.1 系数跟随偏移，`BG_Image` 以 0.05 系数跟随
4. 暗黑地牢风格的暗角效果：在 `BackgroundLayer` 上叠加一个黑色 radial gradient 的 Image，alpha=0.3

### 3. 世界地图（六边形地图）背景替换

修改 `Assets/Scripts/HexGrid/HexWorldMap.cs`：

1. 当前地图是纯黑色背景 + 纯色六边形格子
2. 在场景中添加远景背景 SpriteRenderer（sortingOrder = -10）：
   - 使用 `town_bg.png` 或 `sky01.png` 作为远景
   - 背景跟随相机移动（或固定不动）
3. 修改六边形格子的默认颜色：
   - 当前 `normalColor` 是代码中的 Color 值
   - 改为使用带纹理的 Sprite（从 `fe_flow/` 或 `panels/` 找合适的底纹），或保持半透明深色让背景可见
4. 添加云层/雾气装饰：
   - 使用 `sky02.png` 作为云层，设置半透明并缓慢水平移动（自动滚动）

### 4. 地牢探索背景替换

修改 `Assets/Scripts/Core/DungeonSession.cs` 和 `Assets/Scripts/UI/UIDungeonMapController.cs`：

1. 根据地牢类型（废墟、森林、沼泽等）切换不同背景：
   ```csharp
   public enum DungeonType { Ruins, Warrens, Weald, Cove, Courtyard }
   ```
2. 当前地牢UI是纯黑色背景，替换为：
   - `ruins.png` 用于废墟地牢
   - 其他类型暂时复用 `ruins.png` 或保持深色底
3. 在地牢节点之间移动时，背景保持不变（不切换）
4. 进入战斗时，背景平滑过渡到战斗背景

### 5. 战斗场景背景替换

修改 `Assets/Scripts/Battle/BattleSetup.cs`：

1. 当前战斗场景是纯黑背景，添加战斗背景系统：
   ```csharp
   public class BattleBackgroundController : MonoBehaviour
   {
       public Image backgroundImage;           // 远景背景
       public Image midgroundImage;            // 中景（如石柱、树木）
       public ParticleSystem ambientParticles; // 环境粒子（灰尘、雪花等）
       
       public void SetupBackground(DungeonType type)
       {
           // 根据 dungeon 类型加载对应背景 Sprite
       }
   }
   ```

2. 背景分层：
   - **远景层**：`ruins.png` 或 `town_bg.png` 的裁剪版， darkest 风格（暗色调）
   - **中景层**：使用 `side_decor.png` 或 `panel_transition.png` 作为两侧石柱/装饰
   - **地面层**：底部深色渐变，让角色站立区域更明显
3. 背景在战斗中保持静态，但可以有轻微的呼吸光效（通过修改 Image.color 的 alpha 实现）

### 6. 营地休息画面

修改 `Assets/Scripts/UI/UICampController.cs`（或创建该控制器）：

1. 营地场景显示：
   - 背景：`campfire.png` 或 `campfire_flame.png`
   - 中央显示篝火动画（使用 Spine 的 torch 特效或简单的帧动画）
   - 两侧显示休息中的英雄（使用 idle 动画）
2. UI 面板使用 `panel_banner.png` 作为底板

### 7. 场景过渡效果

创建 `SceneTransitionManager.cs`：

1. 实现类似暗黑地牢的 "墨迹/书本翻页" 过渡效果（简化版）：
   - 使用一个全屏黑色 Image
   - 切换场景时，黑色 Image 的 alpha 从 0 -> 1（0.5秒），加载新场景后再从 1 -> 0
2. 在以下节点触发过渡：
   - 主菜单 -> 世界地图
   - 世界地图 -> 地牢
   - 地牢 -> 战斗
   - 战斗 -> 地牢/世界地图

## 输入输出规范

### 输入
- 源目录：`E:\xlyp\DD.v26186\campaign\`、`fe_flow\`、`props\`、`fx\`
- 运行时：通过 `Resources.Load<Sprite>(path)` 加载
- 地牢类型参数：`DungeonType` enum

### 输出
- 导入的 PNG / Spine 资源文件
- 动态生成的背景 GameObject 和 UI 元素
- 场景过渡时的黑屏遮罩

## 错误处理要求

1. **资源缺失**：如果某个背景 Sprite 加载失败，使用纯黑色背景作为回退，记录 Warning
2. **分辨率适配**：所有背景 Image 使用 `Anchor Stretch`（`anchorMin=(0,0), anchorMax=(1,1)`），确保在不同分辨率下铺满全屏
3. **性能保护**：背景图使用 `Texture2D` 而非 `RenderTexture`，避免每帧重新渲染；如果背景图尺寸过大（>2048），在导入时自动缩放
4. **过渡安全**：场景过渡期间禁用所有输入，防止玩家在过渡中点击导致状态混乱
5. **内存管理**：切换场景时，旧的背景 Sprite 引用释放，允许 Resources.UnloadUnusedAssets()

## 验收标准

- [ ] 编辑器工具能一键导入所有背景美术资源到正确目录
- [ ] 主菜单显示暗黑地牢风格的庄园夜景背景，而非纯黑/纯色
- [ ] 世界地图（六边形地图）有远景天空背景，六边形格子半透明可见
- [ ] 进入战斗时，背景显示地牢风格的远景+中景+地面三层结构
- [ ] 营地休息画面显示篝火背景
- [ ] 场景切换时有黑色淡入淡出过渡效果
- [ ] 所有背景在 1920x1080 和 1280x720 分辨率下正确铺满，无拉伸变形或黑边
- [ ] 如果某个背景资源缺失，显示黑色背景但不崩溃
- [ ] 战斗中背景不会影响角色 Spine 动画的渲染（sortingOrder 正确）
- [ ] 代码注释使用中文
