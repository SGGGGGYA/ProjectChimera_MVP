using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Spine.Unity.Editor;

/// <summary>
/// 暗黑地牢1 Spine 角色批量导入工具
/// 
/// 功能：
///   1. 扫描 DD1 素材目录，将 .atlas/.skel/.png 组合复制到项目
///   2. 检测 Spine 骨骼二进制版本，判断是否与当前 Spine-Unity 3.6 Runtime 兼容
///   3. 兼容时触发 Spine 自动导入（创建 SkeletonDataAsset）
///   4. 不兼容时提取 PNG 精灵作为回退方案
///   5. 自动生成 Resources/dd1_character_map.json 映射表
///
/// 已知问题：
///   DD1 的 .skel 文件使用旧版 Spine 二进制格式（版本号约 2.1.27），
///   与项目中的 Spine-Unity 3.6 Runtime 不兼容，直接导入会抛出
///   IndexOutOfRangeException。本工具会在检测到不兼容时自动走精灵
///   回退流程，将图集 PNG 复制为 Unity Sprite 供战斗系统使用。
///
/// 动画状态映射规则（见 InferAnimationState）：
///   - attack  ← *.sprite.attack_*.skel（匹配任意带 attack_ 前缀的动画）
///   - idle    ← *.sprite.idle.skel
///   - damaged ← *.sprite.combat.skel（combat 对应受损/战斗状态）
///   - defend  ← *.sprite.defend.skel
///   - walk    ← *.sprite.walk.skel
/// </summary>
public static class DD1SpineImporter
{
    // ==================== 配置常量 ====================

    private const string DD1Path = @"E:\xlyp\DD.v26186";
    private const string OutputRoot = "Assets/SpineCharacters/DD1";
    private const string JsonMapPath = "Assets/Resources/dd1_character_map.json";

    /// <summary>
    /// Spine 3.6 Runtime 支持的二进制版本范围
    /// 3.6 只能正确读取 3.6.xx 格式的 skel，3.7+ 或 2.x 都不兼容
    /// </summary>
    private const string SupportedVersionPrefix = "3.6";

    // ==================== 菜单入口 ====================

    /// <summary>
    /// 菜单入口：Tools > DD1 Spine Importer（全量导入）
    /// </summary>
    [MenuItem("Tools/DD1 Spine Importer")]
    static void ImportAll()
    {
        try
        {
            if (!Directory.Exists(DD1Path))
            {
                EditorUtility.DisplayDialog("错误", $"找不到 DD1 素材目录:\n{DD1Path}", "确定");
                return;
            }

            // 确保输出根目录存在
            if (!Directory.Exists(OutputRoot))
                Directory.CreateDirectory(OutputRoot);

            var allEntries = new List<CharacterEntry>();
            int totalImported = 0;
            int spriteFallbackCount = 0;
            int skippedCount = 0;

            // 扫描 heroes 和 monsters
            string[] categories = { "heroes", "monsters" };
            int totalDirs = 0;
            foreach (string cat in categories)
            {
                string catPath = Path.Combine(DD1Path, cat);
                if (Directory.Exists(catPath))
                    totalDirs += Directory.GetDirectories(catPath).Length;
            }

            int processedCount = 0;

            foreach (string category in categories)
            {
                string categoryPath = Path.Combine(DD1Path, category);
                if (!Directory.Exists(categoryPath))
                {
                    Debug.LogWarning($"[DD1Importer] 目录不存在，跳过: {categoryPath}");
                    continue;
                }

                string[] characterDirs = Directory.GetDirectories(categoryPath);

                foreach (string charDir in characterDirs)
                {
                    string charName = Path.GetFileName(charDir);
                    processedCount++;

                    float progress = (float)processedCount / totalDirs;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "DD1 Spine Importer",
                        $"导入 [{category}] {charName}  ({processedCount}/{totalDirs})",
                        progress))
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.Log("[DD1Importer] 用户取消了导入");
                        goto afterImport;
                    }

                    try
                    {
                        var entry = ImportCharacter(category, charName, charDir,
                            out bool usedSpriteFallback, out int skipped);

                        if (entry != null)
                        {
                            allEntries.Add(entry);
                            totalImported++;
                            if (usedSpriteFallback) spriteFallbackCount++;
                        }

                        skippedCount += skipped;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DD1Importer] 导入角色 {charName} 时发生异常: {ex.Message}");
                        skippedCount++;
                    }
                }
            }

        afterImport:

            // 刷新 AssetDatabase，让 Unity 识别新文件
            EditorUtility.DisplayProgressBar("DD1 Spine Importer", "刷新 AssetDatabase...", 0.90f);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            // 对兼容的文件触发 Spine 自动导入
            if (allEntries.Any(e => !e.usedSpriteFallback))
            {
                EditorUtility.DisplayProgressBar("DD1 Spine Importer", "触发 Spine 自动导入...", 0.93f);
                TriggerSpineImport(allEntries.Where(e => !e.usedSpriteFallback).ToList());
                AssetDatabase.Refresh();
            }

            // 生成 JSON 映射表
            EditorUtility.DisplayProgressBar("DD1 Spine Importer", "生成映射表...", 0.97f);
            GenerateJsonMap(allEntries);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();

            string summary = $"DD1 Spine 导入完成！\n\n" +
                             $"成功导入角色数: {totalImported}\n" +
                             $"  - Spine 骨骼导入: {totalImported - spriteFallbackCount}\n" +
                             $"  - 精灵回退方案: {spriteFallbackCount}\n" +
                             $"跳过角色数: {skippedCount}\n\n" +
                             $"映射表已生成: {JsonMapPath}";

            EditorUtility.DisplayDialog("完成", summary, "确定");
            Debug.Log($"[DD1Importer] {summary.Replace("\n\n", " | ").Replace("\n", " | ")}");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[DD1Importer] 导入流程发生严重异常: {ex}");
            EditorUtility.DisplayDialog("错误", $"导入失败:\n{ex.Message}", "确定");
        }
    }

    /// <summary>
    /// 菜单入口：Tools > DD1 Spine Importer (仅 Hero)
    /// </summary>
    [MenuItem("Tools/DD1 Spine Importer (Heroes Only)")]
    static void ImportHeroesOnly()
    {
        ImportCategoryOnly("heroes");
    }

    /// <summary>
    /// 菜单入口：Tools > DD1 Spine Importer (仅 Monster)
    /// </summary>
    [MenuItem("Tools/DD1 Spine Importer (Monsters Only)")]
    static void ImportMonstersOnly()
    {
        ImportCategoryOnly("monsters");
    }

    static void ImportCategoryOnly(string category)
    {
        try
        {
            string categoryPath = Path.Combine(DD1Path, category);
            if (!Directory.Exists(categoryPath))
            {
                EditorUtility.DisplayDialog("错误", $"找不到目录: {categoryPath}", "确定");
                return;
            }

            if (!Directory.Exists(OutputRoot))
                Directory.CreateDirectory(OutputRoot);

            var allEntries = new List<CharacterEntry>();
            int totalImported = 0;
            int spriteFallbackCount = 0;

            string[] characterDirs = Directory.GetDirectories(categoryPath);

            for (int i = 0; i < characterDirs.Length; i++)
            {
                string charDir = characterDirs[i];
                string charName = Path.GetFileName(charDir);

                float progress = (float)i / characterDirs.Length;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "DD1 Spine Importer",
                    $"导入 [{category}] {charName}  ({i + 1}/{characterDirs.Length})",
                    progress))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                try
                {
                    var entry = ImportCharacter(category, charName, charDir, out bool fb, out _);
                    if (entry != null)
                    {
                        allEntries.Add(entry);
                        totalImported++;
                        if (fb) spriteFallbackCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DD1Importer] 导入角色 {charName} 时发生异常: {ex.Message}");
                }
            }

            EditorUtility.DisplayProgressBar("DD1 Spine Importer", "刷新与生成映射...", 0.9f);
            AssetDatabase.Refresh();
            GenerateJsonMap(allEntries);
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("完成",
                $"导入完成: {totalImported} 个角色\n(Spine: {totalImported - spriteFallbackCount}, 精灵: {spriteFallbackCount})",
                "确定");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[DD1Importer] 异常: {ex}");
        }
    }

    // ==================== 角色导入主逻辑 ====================

    /// <summary>
    /// 导入单个角色的所有动画组合
    /// </summary>
    /// <param name="usedSpriteFallback">是否因为版本不兼容而走了精灵回退</param>
    /// <param name="skippedCount">跳过的动画组数量</param>
    static CharacterEntry ImportCharacter(string category, string charName, string charDir,
        out bool usedSpriteFallback, out int skippedCount)
    {
        usedSpriteFallback = false;
        skippedCount = 0;

        // 查找 anim 目录
        string animDir = Path.Combine(charDir, "anim");
        if (!Directory.Exists(animDir))
        {
            // 某些怪物可能直接在根目录有素材
            animDir = charDir;
        }

        string[] atlasFiles = Directory.GetFiles(animDir, "*.atlas", SearchOption.TopDirectoryOnly);
        if (atlasFiles.Length == 0)
        {
            Debug.LogWarning($"[DD1Importer] {charName} 未找到任何 .atlas 文件，跳过");
            skippedCount = -1;
            return null;
        }

        // 创建角色输出目录
        string charOutputDir = Path.Combine(OutputRoot, charName);
        if (!Directory.Exists(charOutputDir))
            Directory.CreateDirectory(charOutputDir);

        var entry = new CharacterEntry
        {
            unitName = charName,
            category = category,
            animations = new Dictionary<string, string>(),
            spritePaths = new Dictionary<string, string>(),
            usedSpriteFallback = false
        };

        int importedCount = 0;

        foreach (string atlasPath in atlasFiles)
        {
            string baseName = Path.GetFileNameWithoutExtension(atlasPath);
            string skelPath = Path.Combine(animDir, baseName + ".skel");

            if (!File.Exists(skelPath))
            {
                Debug.LogWarning($"[DD1Importer] {charName}/{baseName} 缺少 .skel 文件，跳过");
                skippedCount++;
                continue;
            }

            // 查找对应的 .png 纹理
            string pngPath = FindPngForAtlas(atlasPath, baseName, charDir);
            if (string.IsNullOrEmpty(pngPath))
            {
                Debug.LogWarning($"[DD1Importer] {charName}/{baseName} 找不到对应的 .png 纹理，跳过");
                skippedCount++;
                continue;
            }

            // 检测 skel 版本兼容性
            bool versionCompatible = CheckSkeletonVersion(skelPath);
            string animState = InferAnimationState(baseName);

            try
            {
                if (versionCompatible)
                {
                    // === 路径 A：Spine 骨骼导入 ===
                    string destAtlas = Path.Combine(charOutputDir, baseName + ".atlas.txt");
                    string destSkel = Path.Combine(charOutputDir, baseName + ".skel.bytes");
                    string destPng = Path.Combine(charOutputDir, Path.GetFileName(pngPath));

                    File.Copy(atlasPath, destAtlas, true);
                    File.Copy(skelPath, destSkel, true);
                    File.Copy(pngPath, destPng, true);

                    // 映射到预期的 SkeletonDataAsset 路径
                    string skeletonAssetPath = $"{OutputRoot}/{charName}/{baseName}_SkeletonData.asset";
                    entry.animations[animState] = skeletonAssetPath;
                    entry.spritePaths[animState] = $"{OutputRoot}/{charName}/{Path.GetFileName(pngPath)}";

                    importedCount++;
                }
                else
                {
                    // === 路径 B：精灵回退（复制 PNG 作为 Sprite） ===
                    string destPngName = $"{charName}.sprite.{animState}.png";
                    string destPng = Path.Combine(charOutputDir, destPngName);

                    // 也复制 atlas(不含 .skel) 供参考
                    string destAtlas = Path.Combine(charOutputDir, baseName + ".atlas.txt");
                    File.Copy(atlasPath, destAtlas, true);
                    File.Copy(pngPath, destPng, true);

                    // 记录精灵路径（非 SkeletonDataAsset）
                    string spriteAssetPath = $"{OutputRoot}/{charName}/{destPngName}";
                    entry.spritePaths[animState] = spriteAssetPath;
                    entry.animations[animState] = spriteAssetPath; // 回退时 animation path 也是 sprite

                    entry.usedSpriteFallback = true;
                    usedSpriteFallback = true;
                    importedCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DD1Importer] 复制 {charName}/{baseName} 时失败: {ex.Message}");
                skippedCount++;
            }
        }

        if (importedCount == 0)
        {
            Debug.LogWarning($"[DD1Importer] {charName} 没有任何有效动画组被导入");
            return null;
        }

        string mode = entry.usedSpriteFallback ? "精灵回退" : "Spine骨骼";
        Debug.Log($"[DD1Importer] {charName} ({mode}) 成功导入 {importedCount} 组资源");
        return entry;
    }

    // ==================== Spine 版本检测 ====================

    /// <summary>
    /// 检测 .skel 文件的 Spine 二进制版本是否与当前 Runtime 3.6 兼容
    /// 读取二进制头部：hash(varint+string) → version(varint+string)
    /// 如果版本以 "3.6" 开头则兼容，否则不兼容。
    /// </summary>
    static bool CheckSkeletonVersion(string skelPath)
    {
        try
        {
            using (var fs = new FileStream(skelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // 使用 Spine 内置方法读取版本字符串
                string version = Spine.SkeletonBinary.GetVersionString(fs);
                if (string.IsNullOrEmpty(version))
                {
                    Debug.LogWarning($"[DD1Importer] {Path.GetFileName(skelPath)} 无法读取版本信息，视为不兼容");
                    return false;
                }

                bool compatible = version.StartsWith(SupportedVersionPrefix);
                if (!compatible)
                {
                    Debug.Log($"[DD1Importer] {Path.GetFileName(skelPath)} 版本 {version}，" +
                              $"与 3.6 Runtime 不兼容，将使用精灵回退");
                }
                return compatible;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DD1Importer] 读取 {Path.GetFileName(skelPath)} 版本失败: {ex.Message}，视为不兼容");
            return false;
        }
    }

    // ==================== PNG 查找 ====================

    /// <summary>
    /// 查找 atlas 对应的 png 纹理文件
    /// 优先级: 同目录 → 变体目录(_A优先) → atlas文件内容引用 → 全局递归搜索
    /// </summary>
    static string FindPngForAtlas(string atlasPath, string baseName, string charDir)
    {
        string atlasDir = Path.GetDirectoryName(atlasPath);
        string expectedPng = Path.Combine(atlasDir, baseName + ".png");

        // 1. 优先在同目录查找
        if (File.Exists(expectedPng))
            return expectedPng;

        // 2. 在角色根目录及其子目录中递归查找同名 png
        string pngName = baseName + ".png";
        string[] foundPngs = Directory.GetFiles(charDir, pngName, SearchOption.AllDirectories);
        if (foundPngs.Length > 0)
        {
            // 优先选择变体目录中带 "_A" 的路径（默认皮肤）
            string preferred = foundPngs.FirstOrDefault(p =>
                p.Contains("_A" + Path.DirectorySeparatorChar) ||
                p.Contains("_A" + Path.AltDirectorySeparatorChar)) ?? foundPngs[0];
            return preferred;
        }

        // 3. 尝试读取 atlas 文件内容，查找其中引用的 png 名称
        try
        {
            string atlasContent = File.ReadAllText(atlasPath);
            // atlas 第一行通常就是 PNG 文件名
            string firstLine = atlasContent.Split(new[] { '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();

            if (!string.IsNullOrEmpty(firstLine) && firstLine.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                // 在同目录查找
                string atlasPngPath = Path.Combine(atlasDir, firstLine);
                if (File.Exists(atlasPngPath))
                    return atlasPngPath;

                // 在角色根目录递归查找
                string[] recursiveFound = Directory.GetFiles(charDir, firstLine, SearchOption.AllDirectories);
                if (recursiveFound.Length > 0)
                    return recursiveFound[0];
            }
        }
        catch { /* 读取失败忽略 */ }

        // 4. 在角色目录中查找所有 .png 文件（最后手段）
        try
        {
            string[] allPngs = Directory.GetFiles(charDir, "*.png", SearchOption.AllDirectories);
            // 按文件名相似度排序：优先选择包含基础名称的
            var candidates = allPngs
                .Where(p => Path.GetFileNameWithoutExtension(p).Contains(
                    baseName.Substring(Math.Max(0, baseName.LastIndexOf('.') + 1))))
                .OrderBy(p => p.Length)
                .ToList();

            if (candidates.Count > 0)
            {
                // 优先 _A 变体
                string pref = candidates.FirstOrDefault(p =>
                    p.Contains("_A" + Path.DirectorySeparatorChar) || p.Contains("_A/")) ?? candidates[0];
                Debug.Log($"[DD1Importer] 模糊匹配 PNG: {Path.GetFileName(pref)} -> {baseName}");
                return pref;
            }
        }
        catch { }

        return null;
    }

    // ==================== 动画状态推断 ====================

    /// <summary>
    /// 从文件名推断动画状态名称
    /// 
    /// 映射规则:
    ///   idle                → "idle"
    ///   combat              → "combat"    (同时映射为 damaged)
    ///   attack_sword / attack_heal / ...  → "attack"
    ///   defend              → "defend"
    ///   walk                → "walk"
    ///   heroic              → "heroic"
    ///   camp                → "camp"
    ///   afflicted           → "afflicted"
    ///   investigate         → "investigate"
    ///   dead                → "dead"
    /// </summary>
    static string InferAnimationState(string baseName)
    {
        // baseName 格式: "crusader.sprite.attack_sword" 或 "brigand_cutthroat.sprite.combat"
        // 提取最后一段作为动画标识

        int lastDot = baseName.LastIndexOf('.');
        string suffix = lastDot >= 0
            ? baseName.Substring(lastDot + 1).ToLowerInvariant()
            : baseName.ToLowerInvariant();

        // 将 attack_* 统一映射为 "attack"
        if (suffix.StartsWith("attack_") || suffix == "attack")
            return "attack";

        // combat 也映射为 "damaged"（在生成 JSON 时同时记录两个 key）
        // 这里先返回 "combat"，映射表生成时会同时写入 combat 和 damaged

        return suffix;
    }

    // ==================== Spine 导入触发 ====================

    /// <summary>
    /// 手动触发 Spine 编辑器导入流程（仅对兼容版本的条目）
    /// </summary>
    static void TriggerSpineImport(List<CharacterEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;

        var assetPaths = new HashSet<string>();

        foreach (var entry in entries)
        {
            if (entry.usedSpriteFallback) continue; // 跳过精灵回退的条目

            foreach (var kvp in entry.animations)
            {
                // 从 SkeletonDataAsset 路径反推 skel 和 atlas 的 asset 路径
                string basePath = kvp.Value.Replace("_SkeletonData.asset", "");
                string atlasAssetPath = basePath + ".atlas.txt";
                string skelAssetPath = basePath + ".skel.bytes";

                assetPaths.Add(atlasAssetPath);
                assetPaths.Add(skelAssetPath);
            }
        }

        if (assetPaths.Count > 0)
        {
            try
            {
                SpineEditorUtilities.ImportSpineContent(assetPaths.ToArray(), false);
                Debug.Log($"[DD1Importer] 已触发 Spine 导入，共 {assetPaths.Count} 个文件");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DD1Importer] SpineEditorUtilities.ImportSpineContent 调用异常: {ex.Message}");
            }
        }
    }

    // ==================== JSON 映射表生成 ====================

    /// <summary>
    /// 生成 Resources/dd1_character_map.json 映射表
    /// 包含三个层级的回退路径：
    ///   1. skeletonPath  - Spine SkeletonDataAsset（兼容时可用）
    ///   2. spritePath    - 精灵 PNG（Spine 不兼容时的回退）
    ///   3. portraitPath  - 肖像立绘（最终回退，在 BattleSetup 中处理）
    /// </summary>
    static void GenerateJsonMap(List<CharacterEntry> entries)
    {
        var root = new SerializableMap();
        var entryList = new List<SerializableEntry>();

        foreach (var entry in entries)
        {
            var animList = new List<AnimationMapping>();

            foreach (var kvp in entry.animations)
            {
                string state = kvp.Key;

                // 为 combat 状态同时添加 "damaged" 映射
                if (state == "combat")
                {
                    animList.Add(new AnimationMapping { state = "damaged", assetPath = kvp.Value });
                }

                animList.Add(new AnimationMapping { state = state, assetPath = kvp.Value });
            }

            // 确保通用状态有回退映射
            var fallbackMap = new Dictionary<string, string>(entry.animations);
            // 复制 spritePaths 到 fallbackMap 中供回退使用
            foreach (var sp in entry.spritePaths)
            {
                if (!fallbackMap.ContainsKey(sp.Key))
                    fallbackMap[sp.Key] = sp.Value;
            }
            EnsureFallbackStates(fallbackMap);

            string defaultAnim = PickDefaultAnimation(fallbackMap);

            var ser = new SerializableEntry
            {
                unitName = entry.unitName,
                category = entry.category,
                defaultAnimation = defaultAnim,
                animations = animList.ToArray(),
                idlePath = fallbackMap.ContainsKey("idle") ? fallbackMap["idle"] : "",
                combatPath = fallbackMap.ContainsKey("combat") ? fallbackMap["combat"] : "",
                attackPath = fallbackMap.ContainsKey("attack") ? fallbackMap["attack"] : "",
                damagedPath = fallbackMap.ContainsKey("damaged") ? fallbackMap["damaged"] :
                              (fallbackMap.ContainsKey("combat") ? fallbackMap["combat"] : ""),
                usedSpriteFallback = entry.usedSpriteFallback,
                // 额外记录精灵路径表（供 BattleSetup 运行时快速查找）
                spriteIdlePath = entry.spritePaths.ContainsKey("idle") ? entry.spritePaths["idle"] : "",
                spriteCombatPath = entry.spritePaths.ContainsKey("combat") ? entry.spritePaths["combat"] : "",
                spriteAttackPath = entry.spritePaths.ContainsKey("attack") ? entry.spritePaths["attack"] : ""
            };

            entryList.Add(ser);
        }

        root.entries = entryList.ToArray();

        string json = JsonUtility.ToJson(root, true);

        // 确保 Resources 目录存在
        string resourcesDir = Path.GetDirectoryName(JsonMapPath);
        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);

        File.WriteAllText(JsonMapPath, json);
        Debug.Log($"[DD1Importer] 已生成映射表: {JsonMapPath}，共 {entries.Count} 个角色");
    }

    /// <summary>
    /// 选择默认动画（优先 idle，其次 combat，否则取第一个）
    /// </summary>
    static string PickDefaultAnimation(Dictionary<string, string> animations)
    {
        string[] priority = { "idle", "combat", "walk", "defend", "heroic", "camp" };
        foreach (string p in priority)
        {
            if (animations.ContainsKey(p) && !string.IsNullOrEmpty(animations[p]))
                return p;
        }
        return animations.Keys.FirstOrDefault(k => !string.IsNullOrEmpty(animations[k])) ?? "idle";
    }

    /// <summary>
    /// 确保通用动画状态（idle / combat / attack / damaged）有回退映射
    /// </summary>
    static void EnsureFallbackStates(Dictionary<string, string> map)
    {
        string fallback = map.ContainsKey("idle") && !string.IsNullOrEmpty(map["idle"])
            ? map["idle"]
            : (map.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "");

        string[] requiredStates = { "idle", "combat", "attack", "damaged" };
        foreach (string state in requiredStates)
        {
            if (!map.ContainsKey(state) || string.IsNullOrEmpty(map[state]))
            {
                string matched = SmartMatchAnimation(map, state);
                map[state] = matched ?? fallback;
            }
        }
    }

    /// <summary>
    /// 智能匹配动画状态
    /// </summary>
    static string SmartMatchAnimation(Dictionary<string, string> map, string targetState)
    {
        // 精确匹配
        foreach (var kvp in map)
        {
            if (kvp.Key == targetState && !string.IsNullOrEmpty(kvp.Value))
                return kvp.Value;
        }

        // 子串匹配
        foreach (var kvp in map)
        {
            if (kvp.Key.Contains(targetState) && !string.IsNullOrEmpty(kvp.Value))
                return kvp.Value;
        }

        // attack 特殊处理：匹配 attack_* 前缀
        if (targetState == "attack")
        {
            foreach (var kvp in map)
            {
                string k = kvp.Key;
                if ((k.StartsWith("attack_") || k == "attack") && !string.IsNullOrEmpty(kvp.Value))
                    return kvp.Value;
            }
            // 回退到 combat
            if (map.ContainsKey("combat") && !string.IsNullOrEmpty(map["combat"]))
                return map["combat"];
        }

        // damaged 特殊处理：回退到 combat
        if (targetState == "damaged")
        {
            if (map.ContainsKey("combat") && !string.IsNullOrEmpty(map["combat"]))
                return map["combat"];
        }

        return null;
    }

    // ==================== 数据模型 ====================

    class CharacterEntry
    {
        public string unitName;
        public string category;
        public Dictionary<string, string> animations;   // 动画状态 → 资源路径（SkeletonDataAsset 或 Sprite）
        public Dictionary<string, string> spritePaths;  // 动画状态 → PNG Sprite 路径（精灵回退用）
        public bool usedSpriteFallback;
    }

    [System.Serializable]
    class SerializableMap
    {
        public SerializableEntry[] entries;
    }

    [System.Serializable]
    class SerializableEntry
    {
        public string unitName;
        public string category;
        public string defaultAnimation;
        public AnimationMapping[] animations;
        public string idlePath;
        public string combatPath;
        public string attackPath;
        public string damagedPath;
        public bool usedSpriteFallback;

        // 精灵回退专用路径（供 BattleSetup 运行时使用）
        public string spriteIdlePath;
        public string spriteCombatPath;
        public string spriteAttackPath;
    }

    [System.Serializable]
    class AnimationMapping
    {
        public string state;
        public string assetPath;
    }
}
