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
}
