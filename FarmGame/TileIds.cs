namespace FarmGame;

/// <summary>Tile indices in tileset.png (GID = index + 1).</summary>
public static class TileIds
{
    public const int Grass = 0;
    public const int PathMiddle = 20;
    public const int PathBeachVariant = 45;

    public const int FarmLandBase = 72;
    public const int FarmLandCenter = 76;

    public static bool IsHoeable(int tileId) =>
        tileId is Grass or PathMiddle or PathBeachVariant;

    public static bool IsFarmLand(int tileId) =>
        tileId is >= FarmLandBase and <= FarmLandBase + 8;
}
