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

    /// <summary>
    /// 从 PrimaryAttributes + 职业基础值 派生出战斗用属性。
    /// 公式（与 UnitData.GetFinalStat 管线约定保持一致）：
    ///   maxHP = VIT*5 + classBaseHP
    ///   SPD   = AGI*2 + classBaseSPD
    ///   ACC   = 5 + AGI*5 + classBaseACC        // 5 是基线命中
    ///   DOD   = AGI*2 + classBaseDOD
    ///   CRT   = AGI*1.5 + classBaseCRT
    ///   DEF   = baseDefense                     // 由调用方提供，不参与 Primary 缩放
    /// </summary>
    public DerivedAttributes(
        PrimaryAttributes primary,
        int baseDefense,
        int classBaseHP,
        int classBaseSPD,
        int classBaseACC,
        int classBaseDOD,
        int classBaseCRT)
    {
        maxHP = primary.VIT * 5 + classBaseHP;
        SPD = primary.AGI * 2 + classBaseSPD;
        ACC = 5 + primary.AGI * 5 + classBaseACC;
        DOD = Mathf.RoundToInt(primary.AGI * 2f) + classBaseDOD;
        DEF = baseDefense;
        CRT = Mathf.RoundToInt(primary.AGI * 1.5f) + classBaseCRT;
    }
}
