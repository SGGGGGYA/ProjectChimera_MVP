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
    public int CRT;

    public DerivedAttributes(PrimaryAttributes primary, int baseDefense, int classBaseHP, int classBaseSPD)
    {
        maxHP = primary.VIT * 5 + classBaseHP;
        SPD = primary.AGI * 2 + classBaseSPD;
        ACC = 20 + primary.AGI * 5;
        DOD = Mathf.RoundToInt(primary.AGI * 2f);
        DEF = baseDefense;
        CRT = Mathf.RoundToInt(primary.AGI * 1.5f);
    }
}
