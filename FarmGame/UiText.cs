using System.IO;
using System.Numerics;
using Raylib_cs;

namespace FarmGame;

/// <summary>
/// Renders UI text with a readable TTF instead of Raylib's default bitmap font.
/// </summary>
public static class UiText
{
    private static Font _font;
    private static bool _useCustomFont = false;
    private const float Spacing = 0f;
    private const int AtlasFontSize = 128;

    public static void Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fonts", "OpenSans.ttf");
        if (File.Exists(path))
        {
            _font = Raylib.LoadFontEx(path, AtlasFontSize, null, 0);
            Raylib.SetTextureFilter(_font.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
            _useCustomFont = true;
        }
        else
        {
            _font = Raylib.GetFontDefault();
            _useCustomFont = false;
        }
    }

    public static void Unload()
    {
        if (_useCustomFont)
        {
            Raylib.UnloadFont(_font);
            _useCustomFont = false;
        }
    }

    public static void DrawText(string text, int x, int y, int fontSize, Color color)
    {
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), fontSize, Spacing, color);
    }

    public static Vector2 MeasureTextSize(string text, int fontSize)
    {
        return Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
    }
}
