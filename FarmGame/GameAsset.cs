using System.Numerics;
using Raylib_cs;

namespace FarmGame;

/// <summary>
/// Runtime texture built from a <see cref="PixelAssetDefinition"/>.
/// </summary>
public sealed class GameAsset
{
    private readonly int _width;
    private readonly int _height;
    private readonly Texture2D _texture;

    public int Width => _width;
    public int Height => _height;
    public Texture2D Texture => _texture;

    public GameAsset(PixelAssetDefinition definition)
    {
        _width = definition.Width;
        _height = definition.Height;
        _texture = CreateTexture(definition);
        Raylib.SetTextureFilter(_texture, TextureFilter.TEXTURE_FILTER_POINT);
    }

    public void DrawWorld(Vector2 worldPosition, float scale, Vector2 mapOffset)
    {
        DrawInternal(mapOffset + worldPosition, scale);
    }

    public void DrawScreen(Vector2 screenPosition, float scale)
    {
        DrawInternal(screenPosition, scale);
    }

    public void DrawScreenTopLeft(Vector2 screenTopLeft, float scale, Color tint)
    {
        var src = new Rectangle(0, 0, _width, _height);
        var dest = new Rectangle(screenTopLeft.X, screenTopLeft.Y, _width * scale, _height * scale);
        Raylib.DrawTexturePro(_texture, src, dest, Vector2.Zero, 0f, tint);
    }

    public void DrawScreenTopLeft(Vector2 screenTopLeft, float scale) =>
        DrawScreenTopLeft(screenTopLeft, scale, Color.WHITE);

    public void Unload()
    {
        Raylib.UnloadTexture(_texture);
    }

    private void DrawInternal(Vector2 center, float scale)
    {
        float destW = _width * scale;
        float destH = _height * scale;
        var src = new Rectangle(0, 0, _width, _height);
        var dest = new Rectangle(center.X - destW * 0.5f, center.Y - destH * 0.5f, destW, destH);
        Raylib.DrawTexturePro(_texture, src, dest, Vector2.Zero, 0f, Color.WHITE);
    }

    private static Texture2D CreateTexture(PixelAssetDefinition definition)
    {
        Image image = Raylib.GenImageColor(definition.Width, definition.Height, Color.BLANK);
        for (int y = 0; y < definition.Height; y++)
        {
            for (int x = 0; x < definition.Width; x++)
            {
                Color color = definition.Pixels[y * definition.Width + x];
                if (color.A == 0)
                {
                    continue;
                }

                Raylib.ImageDrawPixel(ref image, x, y, color);
            }
        }

        Texture2D texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        return texture;
    }
}
