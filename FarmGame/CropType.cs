namespace FarmGame;

public enum CropType
{
    Carrot,
    Wheat,
    Tomato,
}

public static class CropTypeInfo
{
    public static CropType? FromTool(PlayerTool tool) => tool switch
    {
        PlayerTool.CarrotSeeds => CropType.Carrot,
        PlayerTool.WheatSeeds => CropType.Wheat,
        PlayerTool.TomatoSeeds => CropType.Tomato,
        _ => null,
    };

    public static string DisplayName(CropType type) => type switch
    {
        CropType.Carrot => "Carrot",
        CropType.Wheat => "Wheat",
        CropType.Tomato => "Tomato",
        _ => type.ToString(),
    };

    public static string SeedAssetName(CropType type)
    {
        IReadOnlyList<string> stages = GrowthStageAssetNames(type);
        return stages.Count > 0 ? stages[0] : "Seeds";
    }

    public static string SproutAssetName(CropType type)
    {
        IReadOnlyList<string> stages = GrowthStageAssetNames(type);
        return stages.Count > 1 ? stages[1] : "Sprout";
    }

    public static IReadOnlyList<string> GrowthStageAssetNames(CropType type)
    {
        string name = DisplayName(type);
        IReadOnlyList<string> fromFile = CropDefinitionStore.GetStages(name);
        if (fromFile.Count > 0)
        {
            return fromFile;
        }

        return type switch
        {
            CropType.Carrot => ["Seeds", "Sprout", "Sprout_2", "Sprout_3", "Sprout_4", "Sprout_5"],
            CropType.Wheat => ["Seeds", "Sprout", "Sprout_2", "Sprout_3", "Sprout_4", "Sprout_5"],
            _ => ["Seeds", "Sprout", "Sprout_2", "Sprout_3", "Sprout_4", "Sprout_5"],
        };
    }
}
