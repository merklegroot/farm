using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class CropField
{
    private const float StageTransitionDuration = 0.45f;

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

                float tileLeft = offset.X + x * tileScreenW;
                float tileTop = offset.Y + y * tileScreenH;

                DrawCropGrowth(crop, assets, tileLeft, tileTop, tileScreenW, tileScreenH, scale);
            }
        }
    }

    private void DrawCropGrowth(
        Crop crop,
        AssetLibrary assets,
        float tileLeft,
        float tileTop,
        float tileScreenW,
        float tileScreenH,
        float scale)
    {
        IReadOnlyList<string> stageNames = CropTypeInfo.GrowthStageAssetNames(crop.Type);
        var stages = new List<GameAsset>();
        foreach (string name in stageNames)
        {
            if (TryGetGrowthAsset(assets, name, out GameAsset asset))
            {
                stages.Add(asset);
            }
        }

        if (stages.Count == 0)
        {
            return;
        }

        if (stages.Count == 1)
        {
            DrawAssetCenteredInTile(stages[0], tileLeft, tileTop, tileScreenW, tileScreenH, scale, Color.WHITE);
            return;
        }

        ComputeGrowthStage(crop.AnimTimer, stages.Count, out int fromStage, out int toStage, out float blend);
        byte fromAlpha = (byte)(255 * (1f - blend));
        byte toAlpha = (byte)(255 * blend);

        if (fromAlpha > 0)
        {
            DrawAssetCenteredInTile(
                stages[fromStage],
                tileLeft,
                tileTop,
                tileScreenW,
                tileScreenH,
                scale,
                new Color((byte)255, (byte)255, (byte)255, fromAlpha));
        }

        if (toAlpha > 0)
        {
            DrawAssetCenteredInTile(
                stages[toStage],
                tileLeft,
                tileTop,
                tileScreenW,
                tileScreenH,
                scale,
                new Color((byte)255, (byte)255, (byte)255, toAlpha));
        }
    }

    private static bool TryGetGrowthAsset(AssetLibrary assets, string name, out GameAsset asset)
    {
        if (assets.TryGetAsset(name, out asset))
        {
            return true;
        }

        try
        {
            SavedAsset file = DefinedAssetStore.LoadAsset(name);
            if (assets.TryGetAsset(file.Name, out asset))
            {
                return true;
            }
        }
        catch (FileNotFoundException)
        {
            // Fall through.
        }

        asset = null!;
        return false;
    }

    private static void ComputeGrowthStage(
        float animTimer,
        int stageCount,
        out int fromStage,
        out int toStage,
        out float blend)
    {
        int segmentCount = Math.Max(1, (stageCount - 1) * 2);
        float cycleLength = StageTransitionDuration * segmentCount;
        float phase = animTimer % cycleLength;
        int segment = Math.Min((int)(phase / StageTransitionDuration), segmentCount - 1);
        blend = (phase - segment * StageTransitionDuration) / StageTransitionDuration;

        if (segment < stageCount - 1)
        {
            fromStage = segment;
            toStage = segment + 1;
            return;
        }

        int backSegment = segment - (stageCount - 1);
        fromStage = stageCount - 1 - backSegment;
        toStage = fromStage - 1;
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
