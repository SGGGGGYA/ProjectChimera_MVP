using System;

public enum ModifierType { Add, Mul }
public enum AttributeTarget { MaxHP, SPD, ACC, DOD, DEF, STR, INT, CRT }

[System.Serializable]
public struct AttributeModifier
{
    public ModifierType type;
    public AttributeTarget target;
    public float value;
    public string source;
    /// <summary>持续回合数，0=永久。复制时自动写入 remainingTurns。</summary>
    public int duration;
    /// <summary>剩余回合数，每回合结束 -1，归零时移除。duration=0 时该字段无意义。</summary>
    public int remainingTurns;

    public AttributeModifier(ModifierType type, AttributeTarget target, float value, string source, int duration = 0)
    {
        this.type = type;
        this.target = target;
        this.value = value;
        this.source = source;
        this.duration = duration;
        this.remainingTurns = duration;
    }

    public bool IsExpired => duration > 0 && remainingTurns <= 0;
}
