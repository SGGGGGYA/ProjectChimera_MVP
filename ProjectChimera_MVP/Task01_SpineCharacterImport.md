# Task 01: 暗黑地牢1 Spine角色动画批量导入与集成系统

## 功能描述

开发一个自动化导入系统，将《暗黑地牢1》中的 Spine 骨骼动画资源批量导入 Unity 项目《Project Chimera》，并自动集成到现有的角色战斗系统中。需要处理英雄（如修女、十字军、强盗等）和怪物的 idle/combat/attack 动画，替换当前项目中的纯色占位符角色。

## 技术栈要求

- Unity 2022.3 LTS
- C#（编辑器脚本 + 运行时脚本）
- Spine Unity Runtime（项目中已集成）
- 文件系统操作（System.IO）

## 输入数据规范

### 源资源路径（暗黑地牢1安装目录）
```
E:\xlyp\DD.v26186\heroes\<hero_name>\anim\          # 基础动画
E:\xlyp\DD.v26186\heroes\<hero_name>\<skin_X>\anim\ # 皮肤变体（A/B/C/D）
E:\xlyp\DD.v26186\monsters\<monster_name>\anim\      # 怪物动画
```

每个动画组包含3个文件：
- `<name>.atlas` — Spine 图集描述
- `<name>.skel` — Spine 骨骼二进制数据
- `<name>.png` — 图集纹理

### 目标路径（Unity项目）
```
Assets/SpineCharacters/DD1/<hero_name>/
```

## 实现步骤

### 1. 创建编辑器导入工具 `DD1SpineImporter.cs`

在 `Assets/Editor/` 下创建编辑器窗口，功能包括：

1. **扫描源目录**：递归扫描 `E:\xlyp\DD.v26186\heroes\` 和 `monsters\`，收集所有 `.atlas`/`.skel`/`.png` 组合
2. **文件预处理**：
   - 将 `.atlas` 复制并重命名为 `.atlas.txt`
   - 将 `.skel` 复制并重命名为 `.skel.bytes`
   - 复制 `.png` 纹理（保持原文件名）
3. **自动创建 Spine Asset**：
   - 使用 Spine 编辑器的 API 或反射调用 `SpineEditorUtilities.ImportSkeletonData()`
   - 生成 `<name>_SkeletonData.asset` 和 `<name>_Atlas.asset`
4. **映射表生成**：自动生成 JSON 映射表 `dd1_character_map.json`，记录角色名 -> SkeletonDataAsset 路径的对应关系

### 2. 修改运行时角色加载逻辑

修改 `Assets/Scripts/Battle/BattleSetup.cs` 中的角色创建流程：

1. 在 `CharacterDataEntry` 结构旁新增 `DD1CharacterLoader` 辅助类
2. 修改 `BattleSetup.SpawnUnit()` 方法：
   - 优先从 `characterDataEntries` 查找（保留现有逻辑）
   - 如果未找到，从 `Resources/dd1_character_map.json` 查找对应的 DD1 资源
   - 如果找到 DD1 资源，动态加载 `SkeletonDataAsset` 并赋值给 `SkeletonAnimation`
3. 确保 `SkeletonAnimation.Initialize(true)` 被正确调用

### 3. 动画状态映射

暗黑地牢1的动画命名与项目代码中的命名需要映射：

| 项目代码调用 | DD1 动画文件名 |
|---|---|
| `attackAnimName = "attack"` | `<name>.sprite.attack_*.skel` |
| `idleAnimName = "idle"` | `<name>.sprite.idle.skel` |
| `damagedAnimName = "damaged"` | `<name>.sprite.combat.skel`（或 defend） |
| `deathAnimName = "death"` | 无专用死亡动画，使用 idle 或 combat |

在 `DD1CharacterLoader` 中实现智能匹配：
- 扫描角色的所有动画文件
- 按优先级匹配：精确匹配 > 前缀匹配 > 回退到 idle

### 4. 阵营调色与镜像

保留现有逻辑：
- 玩家阵营：`skeleton.R=1, G=1, B=1, A=1`（正常色）
- 敌人阵营：`skeleton.R=1, G=0.6, B=0.6, A=1`（微红）
- 敌人水平翻转：`transform.localScale = new Vector3(-1, 1, 1)`

## 输入输出规范

### 输入
- 源目录路径：`E:\xlyp\DD.v26186\`
- 角色类型过滤： heroes / monsters / all
- 目标导入目录：`Assets/SpineCharacters/DD1/`

### 输出
- 导入的 Spine 资源文件（`.atlas.txt`, `.skel.bytes`, `.png`, `.asset`）
- `dd1_character_map.json`：映射表
- Console 日志：记录每个角色的导入状态（成功/失败/原因）

### 映射表格式（JSON）
```json
{
  "heroes": {
    "vestal": {
      "skins": ["vestal_A", "vestal_B", "vestal_C", "vestal_D"],
      "default_skin": "vestal_A",
      "animations": {
        "idle": "heroes/vestal/vestal_A/anim/vestal.sprite.idle_SkeletonData",
        "combat": "heroes/vestal/vestal_A/anim/vestal.sprite.combat_SkeletonData",
        "attack_mace": "heroes/vestal/vestal_A/anim/vestal.sprite.attack_mace_SkeletonData"
      }
    }
  },
  "monsters": {}
}
```

## 错误处理要求

1. **源文件缺失**：如果 `.atlas`/`.skel`/`.png` 三件套不完整，跳过该角色并记录 Warning
2. **Spine 版本不兼容**：如果 `.skel` 无法解析，记录 Error 并跳过，不要崩溃
3. **重复导入**：如果目标目录已存在同名文件，提供 "覆盖/跳过/重命名" 选项
4. **路径不存在**：如果源目录路径错误，显示对话框提示用户
5. **导入失败保护**：使用 try-catch 包裹整个导入流程，任何异常都不应让 Unity 编辑器崩溃

## 验收标准

- [ ] 编辑器窗口能正常打开（Menu: Tools > DD1 Spine Importer）
- [ ] 成功导入至少1个英雄（如修女 vestal）的所有基础动画（idle/combat/attack）
- [ ] 导入的资源能在 Unity Inspector 中正确预览 Spine 动画
- [ ] 战斗场景中，使用 DD1 资源的角色能正常播放 idle 和 attack 动画
- [ ] `dd1_character_map.json` 文件正确生成且可被运行时读取
- [ ] 导入过程中出现任何错误，Unity 都不崩溃，且 Console 有清晰的错误日志
- [ ] 代码注释使用中文，关键函数有 XML 文档注释
