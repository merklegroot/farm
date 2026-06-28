using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class CropField
{
    private const float PlantAnimHalfDuration = 0.45f;

    private readonly int _width;
    private readonly int _height;
    private readonly Crop?[] _crops;

    public CropField(int mapWidth, int mapHeight)
    {
        _width = mapWidth;
        _height = mapHeight;
        _crops = new Crop?[mapWidth * mapHeight];
    }

    public void Update(float deltaTime)
    {
        float dt = Math.Min(deltaTime, 1f / 60f);

        for (int i = 0; i < _crops.Length; i++)
        {
            Crop? crop = _crops[i];
            if (crop != null)
            {
                crop.AnimTimer += dt;
            }
        }
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

        _crops[Index(tileX, tileY)] = new Crop { Type = type, AnimTimer = 0f };
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

                string seedName = CropTypeInfo.SeedAssetName(crop.Type);
                if (!assets.TryGetAsset(seedName, out GameAsset seedAsset))
                {
                    continue;
                }

                float tileLeft = offset.X + x * tileScreenW;
                float tileTop = offset.Y + y * tileScreenH;

                string sproutName = CropTypeInfo.SproutAssetName(crop.Type);
                if (!assets.TryGetAsset(sproutName, out GameAsset sproutAsset))
                {
                    DrawAssetCenteredInTile(seedAsset, tileLeft, tileTop, tileScreenW, tileScreenH, scale, Color.WHITE);
                    continue;
                }

                float blend = ComputePlantBlend(crop.AnimTimer);
                byte seedAlpha = (byte)(255 * (1f - blend));
                byte sproutAlpha = (byte)(255 * blend);

                if (seedAlpha > 0)
                {
                    DrawAssetCenteredInTile(
                        seedAsset,
                        tileLeft,
                        tileTop,
                        tileScreenW,
                        tileScreenH,
                        scale,
                        new Color((byte)255, (byte)255, (byte)255, seedAlpha));
                }

                if (sproutAlpha > 0)
                {
                    DrawAssetCenteredInTile(
                        sproutAsset,
                        tileLeft,
                        tileTop,
                        tileScreenW,
                        tileScreenH,
                        scale,
                        new Color((byte)255, (byte)255, (byte)255, sproutAlpha));
                }
            }
        }
    }

    private static float ComputePlantBlend(float animTimer)
    {
        float cycleLength = PlantAnimHalfDuration * 2f;
        if (animTimer >= cycleLength)
        {
            return 0f;
        }

        return animTimer < PlantAnimHalfDuration
            ? animTimer / PlantAnimHalfDuration
            : (cycleLength - animTimer) / PlantAnimHalfDuration;
    }

    private static void DrawAssetCenteredInTile(
        GameAsset asset,
        float tileLeft,
        float tileTop,
        float tileScreenW,
        float tileScreenH,
        float scale,
        Color tint)
    {
        float assetScreenW = asset.Width * scale;
        float assetScreenH = asset.Height * scale;
        float destX = tileLeft + (tileScreenW - assetScreenW) * 0.5f;
        float destY = tileTop + (tileScreenH - assetScreenH) * 0.5f;
        asset.DrawScreenTopLeft(new Vector2(destX, destY), scale, tint);
    }

    private bool InBounds(int tileX, int tileY) =>
        tileX >= 0 && tileY >= 0 && tileX < _width && tileY < _height;

    private int Index(int tileX, int tileY) => tileY * _width + tileX;

    private sealed class Crop
    {
        public required CropType Type { get; init; }
        public float AnimTimer;
    }
}
