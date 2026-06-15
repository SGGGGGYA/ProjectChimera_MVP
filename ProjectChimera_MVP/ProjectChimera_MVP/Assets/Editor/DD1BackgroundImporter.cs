using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DD1 背景美术资源导入工具
/// 将《暗黑地牢1》的场景背景资源批量导入到 Unity 项目的 Resources/Backgrounds/ 目录下。
/// Menu: Tools > DD1 Background Importer
/// </summary>
public class DD1BackgroundImporter : EditorWindow
{
    string dd1Path = @"E:\xlyp\DD.v26186";

    [MenuItem("Tools/DD1 Background Importer")]
    static void OpenWindow()
    {
        GetWindow<DD1BackgroundImporter>("DD1 Background Importer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("DD1 素材根目录", EditorStyles.boldLabel);
        dd1Path = EditorGUILayout.TextField("路径", dd1Path);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("一键导入背景资源", GUILayout.Height(40)))
        {
            ImportAll();
        }
    }

    void ImportAll()
    {
        if (!Directory.Exists(dd1Path))
        {
            EditorUtility.DisplayDialog("错误", $"找不到 DD1 素材目录: {dd1Path}", "确定");
            return;
        }

        int totalCount = 0;
        int successCount = 0;

        // ===== 1. 主菜单背景 =====
        if (TryImport($"{dd1Path}/fe_flow/title_bg.png", "Assets/Resources/Backgrounds/Menu/title_bg.png")) successCount++;
        if (TryImport($"{dd1Path}/fe_flow/title_house.png", "Assets/Resources/Backgrounds/Menu/title_house.png")) successCount++;
        totalCount += 2;

        // ===== 2. 主菜单按钮（导入到 UI/Menu/） =====
        if (TryImport($"{dd1Path}/fe_flow/start_button.png", "Assets/Resources/UI/Menu/start_button.png")) successCount++;
        if (TryImport($"{dd1Path}/fe_flow/settings.button.png", "Assets/Resources/UI/Menu/settings_button.png")) successCount++;
        totalCount += 2;

        // ===== 3. 城镇/世界地图背景 =====
        if (TryImport($"{dd1Path}/campaign/town/town_bg.png", "Assets/Resources/Backgrounds/Town/town_bg.png")) successCount++;
        if (TryImport($"{dd1Path}/campaign/town/sky/sky01.png", "Assets/Resources/Backgrounds/Town/sky01.png")) successCount++;
        if (TryImport($"{dd1Path}/campaign/town/sky/sky02.png", "Assets/Resources/Backgrounds/Town/sky02.png")) successCount++;
        totalCount += 3;

        // ===== 4. 地牢背景 =====
        if (TryImport($"{dd1Path}/campaign/town/sky/ruins.png", "Assets/Resources/Backgrounds/Dungeon/ruins.png")) successCount++;
        totalCount += 1;

        // ===== 5. 战斗场景背景（从多个可能路径尝试） =====
        // 尝试从 dungeon/ 目录导入战斗远景
        if (TryImport($"{dd1Path}/dungeons/ruins/ruins_bg.png", "Assets/Resources/Backgrounds/Battle/ruins_bg.png"))
            successCount++;
        totalCount++;
        
        // 尝试导入战斗中景石柱
        if (TryImport($"{dd1Path}/dungeons/ruins/pillar.png", "Assets/Resources/Backgrounds/Battle/ruins_pillar.png"))
            successCount++;
        totalCount++;
        
        // 尝试从 props 目录导入石柱
        if (TryImport($"{dd1Path}/props/shared/pillar.png", "Assets/Resources/Backgrounds/Battle/side_decor.png"))
            successCount++;
        totalCount++;

        // ===== 6. 营地背景 =====
        if (TryImport($"{dd1Path}/props/shared/campfire.png", "Assets/Resources/Backgrounds/Camp/campfire.png")) successCount++;
        totalCount += 1;

        // ===== 7. 尝试从 fx 目录导入额外特效资源 =====
        if (TryImport($"{dd1Path}/fx/light_glow.png", "Assets/Resources/Backgrounds/Battle/light_glow.png")) successCount++;
        totalCount += 1;

        // ===== 确保所有子目录存在 =====
        EnsureDirectoryExists("Assets/Resources/Backgrounds");
        EnsureDirectoryExists("Assets/Resources/Backgrounds/Menu");
        EnsureDirectoryExists("Assets/Resources/Backgrounds/Town");
        EnsureDirectoryExists("Assets/Resources/Backgrounds/Dungeon");
        EnsureDirectoryExists("Assets/Resources/Backgrounds/Battle");
        EnsureDirectoryExists("Assets/Resources/Backgrounds/Camp");

        int failCount = totalCount - successCount;
        EditorUtility.DisplayDialog("导入完成",
            $"成功: {successCount}\n失败（源文件不存在）: {failCount}\n总计尝试: {totalCount}",
            "确定");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 尝试导入单个文件，源文件不存在时仅警告而不报错
    /// </summary>
    bool TryImport(string sourcePath, string destAssetPath)
    {
        return ImportSprite(sourcePath, destAssetPath);
    }

    /// <summary>
    /// 确保目标目录存在（Unity Assets 目录下）
    /// </summary>
    void EnsureDirectoryExists(string assetDir)
    {
        string fullPath = Path.Combine(Application.dataPath, assetDir.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"[DD1Importer] 已创建目录: {assetDir}");
        }
    }

    bool ImportSprite(string sourcePath, string destAssetPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"[DD1Importer] 源文件不存在（跳过）: {sourcePath}");
            return false;
        }

        try
        {
            string destDir = Path.GetDirectoryName(destAssetPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // 复制文件到项目
            File.Copy(sourcePath, destAssetPath, true);
            AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);

            // 设置导入参数：Sprite (2D and UI)
            TextureImporter importer = AssetImporter.GetAtPath(destAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Debug.Log($"[DD1Importer] 已导入: {destAssetPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DD1Importer] 导入失败: {sourcePath} -> {destAssetPath}\n{e.Message}");
            return false;
        }
    }
}
