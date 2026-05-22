# Project Chimera — Git 工作流规范

## 分支结构

```
master           稳定可玩版，只合入经过 review + 测试的代码
develop          集成分支，三个窗口的代码先在这里汇合
  ├─ battle     战斗系统（Bug修复、AI、平衡）
  ├─ content    内容开发（职业、敌人、掉落、装备）
  └─ systems    核心架构（重构、数值框架、场景系统）
```

## 分支职责

| 分支 | 负责人 | 内容 |
|:---|:---|:---|
| `master` | 代码审查者 | 仅从 `develop` 合入，需 review + 跑通战斗 |
| `develop` | 代码审查者 | 三个窗口的代码在此集成，解决冲突 |
| `battle` | 窗口1 | 战斗场景、BattleManager、TurnManager、AI |
| `content` | 窗口3 | 新职业/敌人、掉落、装备、技能数据 |
| `systems` | 窗口2 | Core/Data/Attributes/Commands、场景框架 |

## 日常工作流

### 每个窗口
```
git checkout <分支>
# 写代码...
git add <文件>
git commit -m "说明"
```

### 代码审查者合并到 develop
```
git checkout develop
git merge <分支>
# 解决冲突、review、修复
git push
```

### 发布到 master
```
git checkout master
git merge develop
# 确保能跑通一场完整战斗
```

## 提交规范

格式：`<类型> <模块> — <说明>`

```
fix   battle — 修复目标选择高亮位置错误
feat  core   — 实现三层属性结构
refac skill  — 迁移所有技能到命令队列
```

## 关键规则

1. 不要直接往 `master` 提交代码
2. 合入 `develop` 前至少确认编译通过
3. 合入 `master` 前需要跑通一次完整战斗循环
4. 冲突在 `develop` 上解决，不要在主题分支里解
