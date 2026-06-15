using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DD1 UI 美术资源导入器
/// 将暗黑地牢1的 UI 素材扫描并复制到项目 Resources 目录，供运行时动态加载
/// </summary>
public class DD1UIArtImporter : EditorWindow
{
    private static readonly string SourceRoot = @"E:\xlyp\DD.v26186";
    private static readonly string ProjectRoot = @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP";

    [MenuItem("Tools/DD1 UI Art Importer")]
    static void OpenWindow()
    {
        GetWindow<DD1UIArtImporter>("DD1 UI Art Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("DD1 UI 美术资源导入器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label($"源路径: {SourceRoot}");
        GUILayout.Label($"目标路径: {ProjectRoot}/Assets/Resources");
        GUILayout.Space(10);

        if (GUILayout.Button("执行导入", GUILayout.Height(40)))
        {
            ImportAll();
        }
    }

    static void ImportAll()
    {
        int copied = 0;
        int skipped = 0;

        // 1. tray_*.png → Assets/Resources/UI/Icons/Status/
        copied += ImportPattern(
            Path.Combine(SourceRoot, "overlays"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Icons/Status"),
            "tray_*.png",
            "dd1_");

        // 2. target_*.png / target_h_*.png / selected_*.png → Assets/Resources/UI/Overlays/
        copied += ImportPattern(
            Path.Combine(SourceRoot, "overlays"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Overlays"),
            "target_*.png",
            "dd1_");
        copied += ImportPattern(
            Path.Combine(SourceRoot, "overlays"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Overlays"),
            "target_h_*.png",
            "dd1_");
        copied += ImportPattern(
            Path.Combine(SourceRoot, "overlays"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Overlays"),
            "selected_*.png",
            "dd1_");

        // 3. panel_*.png / retreat_button.png / side_decor.png → Assets/Resources/UI/Panels/
        copied += ImportPattern(
            Path.Combine(SourceRoot, "panels"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Panels"),
            "panel_*.png",
            "dd1_");
        copied += ImportPattern(
            Path.Combine(SourceRoot, "panels"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Panels"),
            "retreat_button.png",
            "dd1_");
        copied += ImportPattern(
            Path.Combine(SourceRoot, "panels"),
            Path.Combine(ProjectRoot, "Assets/Resources/UI/Panels"),
            "side_decor.png",
            "dd1_");

        // 4. heroes/<name>/*_portrait_roster.png → Assets/Resources/UI/Portraits/
        copied += ImportHeroPortraits();

        // 5. heroes/<name>/*.ability.*.png → Assets/Resources/UI/Icons/Skills/
        copied += ImportHeroAbilities();

        Debug.Log($"[DD1Importer] 导入完成: 复制 {copied} 个文件, 跳过 {skipped} 个文件");
        AssetDatabase.Refresh();
    }

    static int ImportPattern(string sourceDir, string destDir, string searchPattern, string prefix)
    {
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"[DD1Importer] 源目录不存在: {sourceDir}");
            return 0;
        }

        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        int count = 0;
        string[] files = Directory.GetFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            string destName = prefix + fileName;
            string destPath = Path.Combine(destDir, destName);

            try
            {
                File.Copy(file, destPath, true);
                count++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DD1Importer] 复制失败: {file} → {destPath}, 错误: {e.Message}");
            }
        }
        return count;
    }

    static int ImportHeroPortraits()
    {
        string heroesDir = Path.Combine(SourceRoot, "heroes");
        string destDir = Path.Combine(ProjectRoot, "Assets/Resources/UI/Portraits");
        if (!Directory.Exists(heroesDir))
        {
            Debug.LogWarning($"[DD1Importer] 英雄目录不存在: {heroesDir}");
            return 0;
        }

        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        int count = 0;
        foreach (var heroDir in Directory.GetDirectories(heroesDir))
        {
            string heroName = Path.GetFileName(heroDir);
            string[] portraits = Directory.GetFiles(heroDir, "*_portrait_roster.png", SearchOption.TopDirectoryOnly);
            foreach (var portrait in portraits)
            {
                string fileName = Path.GetFileName(portrait);
                string destName = $"dd1_{heroName}_{fileName}";
                string destPath = Path.Combine(destDir, destName);
                try
                {
                    File.Copy(portrait, destPath, true);
                    count++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DD1Importer] 复制失败: {portrait} → {destPath}, 错误: {e.Message}");
                }
            }
        }
        return count;
    }

    static int ImportHeroAbilities()
    {
        string heroesDir = Path.Combine(SourceRoot, "heroes");
        string destDir = Path.Combine(ProjectRoot, "Assets/Resources/UI/Icons/Skills");
        if (!Directory.Exists(heroesDir))
        {
            Debug.LogWarning($"[DD1Importer] 英雄目录不存在: {heroesDir}");
            return 0;
        }

        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        int count = 0;
        foreach (var heroDir in Directory.GetDirectories(heroesDir))
        {
            string heroName = Path.GetFileName(heroDir);
            string[] abilities = Directory.GetFiles(heroDir, "*.ability.*.png", SearchOption.TopDirectoryOnly);
            foreach (var ability in abilities)
            {
                string fileName = Path.GetFileName(ability);
                string destName = $"dd1_{heroName}_{fileName}";
                string destPath = Path.Combine(destDir, destName);
                try
                {
                    File.Copy(ability, destPath, true);
                    count++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DD1Importer] 复制失败: {ability} → {destPath}, 错误: {e.Message}");
                }
            }
        }
        return count;
    }
}
