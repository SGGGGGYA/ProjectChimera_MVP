using System.Collections.Generic;

[System.Serializable]
public class StatMod
{
    public StatType stat;
    public int amount;
    public bool isPercent;
}

[System.Serializable]
public class Equipment
{
    public string id;
    public string equipmentName;
    public List<StatMod> mods = new List<StatMod>();

    public int GetFlatMod(StatType stat)
    {
        int sum = 0;
        foreach (var m in mods)
            if (m.stat == stat && !m.isPercent)
                sum += m.amount;
        return sum;
    }

    public float GetPercentMod(StatType stat)
    {
        float sum = 0;
        foreach (var m in mods)
            if (m.stat == stat && m.isPercent)
                sum += m.amount * 0.01f;
        return sum;
    }
}

[System.Serializable]
public class Weapon : Equipment
{
}

[System.Serializable]
public class Armor : Equipment
{
}
