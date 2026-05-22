using System;

public enum ModifierType { Add, Mul }
public enum AttributeTarget { MaxHP, SPD, ACC, DOD, DEF, STR, INT }

[System.Serializable]
public struct AttributeModifier
{
    public ModifierType type;
    public AttributeTarget target;
    public float value;
    public string source;

    public AttributeModifier(ModifierType type, AttributeTarget target, float value, string source)
    {
        this.type = type;
        this.target = target;
        this.value = value;
        this.source = source;
    }
}
