/// <summary>
/// 旧版资源类型枚举 — 仅供 HexTile.legacyResourceType 兼容字段使用。
/// 新代码请使用 ResourceType（HexTypes.cs）+ TileYield 产出系统。
/// </summary>
[System.Obsolete("使用 ResourceType + YieldTable 代替")]
public enum TileResourceType
{
    None,
    Food,
    Production,
    Gold,
    Science,
    Mana
}
