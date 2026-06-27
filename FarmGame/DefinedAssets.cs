using Raylib_cs;

namespace FarmGame;

/// <summary>
/// Example assets defined entirely in code. Add new definitions here, register in <see cref="RegisterAll"/>.
/// </summary>
public static class DefinedAssets
{
    public static void RegisterAll(AssetLibrary library)
    {
        library.DefineOrReplace("farm_sign", FarmSign());
        library.DefineOrReplace("scarecrow", Scarecrow());
    }

    public static PixelAssetDefinition FarmSign()
    {
        var palette = new Dictionary<char, Color>
        {
            ['w'] = new Color(139, 90, 43, 255),
            ['W'] = new Color(101, 67, 33, 255),
            ['s'] = new Color(228, 198, 130, 255),
            ['i'] = new Color(58, 42, 30, 255),
            ['p'] = new Color(110, 74, 38, 255),
        };

        return PixelAssetDefinition.FromRows(palette,
            "....wwww....",
            "...wwssww...",
            "..wwssssww..",
            "..wwssisww..",
            "...wwssww...",
            "....wwww....",
            ".....pp.....",
            ".....pp.....");
    }

    public static PixelAssetDefinition Scarecrow()
    {
        var palette = new Dictionary<char, Color>
        {
            ['w'] = new Color(139, 90, 43, 255),
            ['W'] = new Color(101, 67, 33, 255),
            ['l'] = new Color(86, 152, 56, 255),
            ['L'] = new Color(58, 110, 38, 255),
            ['s'] = new Color(228, 198, 130, 255),
            ['i'] = new Color(58, 42, 30, 255),
        };

        return PixelAssetDefinition.FromRows(palette,
            "..ll..ll..",
            ".llllllll.",
            "..llllll..",
            "...llll...",
            "....ww....",
            "...wssw...",
            "...wiiw...",
            "...wssw...",
            "....ww....",
            "...wwww...",
            "..ww..ww..");
    }
}
