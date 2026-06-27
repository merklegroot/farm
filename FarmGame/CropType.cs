namespace FarmGame;

public enum CropType
{
    Carrot,
    Wheat,
}

public static class CropTypeInfo
{
    public static CropType? FromTool(PlayerTool tool) => tool switch
    {
        PlayerTool.CarrotSeeds => CropType.Carrot,
        PlayerTool.WheatSeeds => CropType.Wheat,
        _ => null,
    };

    public static string SeedAssetName(CropType type) => type switch
    {
        CropType.Carrot => "Seeds",
        CropType.Wheat => "Seeds",
        _ => "Seeds",
    };
}
