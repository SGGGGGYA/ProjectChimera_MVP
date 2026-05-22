using System;

[System.Serializable]
public struct PrimaryAttributes
{
    public int VIT;
    public int STR;
    public int AGI;
    public int INT;

    public PrimaryAttributes(int vit, int str, int agi, int intelligence)
    {
        VIT = vit;
        STR = str;
        AGI = agi;
        INT = intelligence;
    }
}
