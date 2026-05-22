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

    [Header("技能池")]
    public List<SkillData> skillPool = new List<SkillData>();
}
