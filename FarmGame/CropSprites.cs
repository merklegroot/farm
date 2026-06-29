using Raylib_cs;

namespace FarmGame;

/// <summary>16×16 cells in Outdoor_Decor_Free.png.</summary>
public static class CropSprites
{
    private const int Cell = 16;

    public static Rectangle SeedBag(CropType type) => type switch
    {
        CropType.Carrot => CellRect(3, 0),
        CropType.Wheat => CellRect(5, 0),
        CropType.Tomato => CellRect(4, 0),
        _ => CellRect(3, 0),
    };

    public static Rectangle GrowthStage(CropType type, int stage)
    {
        int row = 8 + Math.Clamp(stage, 0, 3);
        int col = type switch
        {
            CropType.Carrot => 0,
            CropType.Wheat => 2,
            _ => 0,
        };
        return CellRect(col, row);
    }

    private static Rectangle CellRect(int col, int row) =>
        new Rectangle(col * Cell, row * Cell, Cell, Cell);
}
