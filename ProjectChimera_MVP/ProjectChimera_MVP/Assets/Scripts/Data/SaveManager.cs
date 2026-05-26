using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档数据结构
/// </summary>
[System.Serializable]
public class SaveData
{
    public List<UnitBattleData> playerTeam;
    public int squadX;
    public int squadY;
    public string timestamp;
    public List<ItemStack> inventory = new List<ItemStack>();
    public int gold;
}

/// <summary>
/// 存档系统 — 保存/读取 JSON 文件到项目根目录
/// </summary>
public static class SaveManager
{
    private static string _savePath;
    public static string SavePath
    {
        get
        {
            if (_savePath == null)
                _savePath = Application.dataPath + "/../save_data.json";
            return _savePath;
        }
    }

    /// <summary>写入存档</summary>
    public static void Save(SaveData data)
    {
        try
        {
            data.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[存档] 已保存到 {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[存档] 保存失败: {e.Message}");
        }
    }

    /// <summary>读取存档，不存在返回 null</summary>
    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
                return null;

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[存档] 已读取存档 ({data?.timestamp})");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[存档] 读取失败: {e.Message}");
            return null;
        }
    }

    /// <summary>是否有存档</summary>
    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    /// <summary>删除存档</summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[存档] 已删除");
        }
    }
}
