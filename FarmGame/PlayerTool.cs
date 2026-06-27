namespace FarmGame;

public enum PlayerTool
{
    Hands = 0,
    Pickaxe = 1,
    Axe = 2,
    Hoe = 3,
    WateringCan = 4,
    CarrotSeeds = 5,
    WheatSeeds = 6,
}

public static class PlayerToolInfo
{
    public static int SlotCount => 7;

    /// <summary>First row of this tool's block in Player_Actions.png (side-facing row).</summary>
    public static int ActionSideRow(PlayerTool tool) => tool switch
    {
        PlayerTool.Pickaxe => 0,
        PlayerTool.Axe => 3,
        PlayerTool.Hoe => 6,
        PlayerTool.WateringCan => 9,
        _ => -1,
    };

    public static bool HasAction(PlayerTool tool) =>
        tool is PlayerTool.Pickaxe or PlayerTool.Axe or PlayerTool.Hoe or PlayerTool.WateringCan;

    public static bool IsSeed(PlayerTool tool) =>
        tool is PlayerTool.CarrotSeeds or PlayerTool.WheatSeeds;

    /// <summary>Icon source row in Player_Actions (down-facing start frame).</summary>
    public static int IconRow(PlayerTool tool) => tool switch
    {
        PlayerTool.Pickaxe => 1,
        PlayerTool.Axe => 4,
        PlayerTool.Hoe => 7,
        PlayerTool.WateringCan => 10,
        _ => -1,
    };
}
