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

    public int GetBuyPrice()
    {
        int price = 50;
        if (string.IsNullOrEmpty(id)) return price;
        int hash = 17;
        foreach (char c in id)
            hash = hash * 31 + c;
        var rng = new System.Random(hash);
        foreach (var mod in mods)
            price += rng.Next(30, 81);
        return price;
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
