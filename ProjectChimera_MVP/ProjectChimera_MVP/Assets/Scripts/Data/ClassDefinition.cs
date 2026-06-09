using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewClass", menuName = "Chimera/ClassDefinition")]
public class ClassDefinition : ScriptableObject
{
    public string id;
    public string unitName;

    [Header("基础属性")]
    public int baseVIT = 10;
    public int baseSTR = 10;
    public int baseDEF = 5;
    public int baseAGI = 5;
    public int baseINT = 3;
    public int baseWeaponAttack;

    [Header("生命")]
    public int baseHp = 40;

    /// <summary>职业基础速度（= AGI*2 + 此值）。敏捷型职业可填正数抵消笨重减益。</summary>
    [Header("战斗属性")]
    public int baseSPD = 0;
    /// <summary>职业基础命中（= 5 + AGI*5 + 此值）。满 5 = 默认水平；刺客/弓手类 +10~+15。</summary>
    public int baseACC = 5;
    /// <summary>职业基础闪避（= AGI*2 + 此值）。盗贼/舞者类 +10~+20。</summary>
    public int baseDOD = 0;
    /// <summary>职业基础暴击（= AGI*1.5 + 此值）。刺客/暴击流派 +5~+10。</summary>
    public int baseCRT = 0;

    [Header("技能池")]
    public List<SkillData> skillPool = new List<SkillData>();
}
