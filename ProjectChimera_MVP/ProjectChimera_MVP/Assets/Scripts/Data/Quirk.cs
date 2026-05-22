using System.Collections.Generic;

[System.Serializable]
public class QuirkEffect
{
    public StatType stat;
    public int amount;
    public bool isPercent;
}

[System.Serializable]
public class Quirk
{
    public string id;
    public string localizedName;
    public bool isPositive;
    public bool isLocked;
    public List<QuirkEffect> effects = new List<QuirkEffect>();

    public int GetFlatMod(StatType stat)
    {
        int sum = 0;
        foreach (var e in effects)
            if (e.stat == stat && !e.isPercent)
                sum += e.amount;
        return sum;
    }

    public float GetPercentMod(StatType stat)
    {
        float sum = 0;
        foreach (var e in effects)
            if (e.stat == stat && e.isPercent)
                sum += e.amount * 0.01f;
        return sum;
    }
}
