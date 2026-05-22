using System;
using UnityEngine;

[System.Serializable]
public struct DerivedAttributes
{
    public int maxHP;
    public int SPD;
    public int ACC;
    public int DOD;
    public int DEF;

    public DerivedAttributes(PrimaryAttributes primary, int baseDefense, int classBaseHP, int classBaseSPD)
    {
        maxHP = primary.VIT * 5 + classBaseHP;
        SPD = primary.AGI * 2 + classBaseSPD;
        ACC = 80 + primary.AGI * 2;
        DOD = Mathf.RoundToInt(primary.AGI * 1.5f);
        DEF = baseDefense;
    }
}
