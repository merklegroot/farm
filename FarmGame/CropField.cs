using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class CropField
{
    private readonly int _width;
    private readonly int _height;
    private readonly Crop?[] _crops;

    public CropField(int mapWidth, int mapHeight)
    {
        _width = mapWidth;
        _height = mapHeight;
        _crops = new Crop?[mapWidth * mapHeight];
    }

    public bool TryPlant(int tileX, int tileY, CropType type, TileMap map, AssetLibrary assets)
    {
        if (!InBounds(tileX, tileY) || _crops[Index(tileX, tileY)] != null)
        {
            return false;
        }

        if (!TileIds.IsFarmLand(map.GetTileId(tileX, tileY)))
        {
            return false;
        }

        string assetName = CropTypeInfo.SeedAssetName(type);
        if (!assets.TryGetAsset(assetName, out _))
        {
            return false;
        }

        _crops[Index(tileX, tileY)] = new Crop { Type = type };
        return true;
    }

    public void Draw(TileMap map, float scale, Vector2 offset, AssetLibrary assets)
    {
        float tileScreenW = map.TileWidth * scale;
        float tileScreenH = map.TileHeight * scale;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Crop? crop = _crops[Index(x, y)];
                if (crop == null)
                {
                    continue;
                }

                string assetName = CropTypeInfo.SeedAssetName(crop.Type);
                if (!assets.TryGetAsset(assetName, out GameAsset asset))
                {
                    continue;
                }

                float assetScreenW = asset.Width * scale;
                float assetScreenH = asset.Height * scale;
                float destX = offset.X + x * tileScreenW + (tileScreenW - assetScreenW) * 0.5f;
                float destY = offset.Y + y * tileScreenH + (tileScreenH - assetScreenH) * 0.5f;
                asset.DrawScreenTopLeft(new Vector2(destX, destY), scale);
            }
        }
    }

    private bool InBounds(int tileX, int tileY) =>
        tileX >= 0 && tileY >= 0 && tileX < _width && tileY < _height;

    private int Index(int tileX, int tileY) => tileY * _width + tileX;

    private sealed class Crop
    {
        public required CropType Type { get; init; }
    }
}
