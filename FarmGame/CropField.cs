using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class CropField
{
    private const float StageDuration = 5f;
    private const int MaxStage = 3;

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
        for (int i = 0; i < _crops.Length; i++)
        {
            if (_crops[i] is not Crop crop)
            {
                continue;
            }

            if (crop.Stage >= MaxStage)
            {
                continue;
            }

            crop.StageTimer += deltaTime;
            if (crop.StageTimer >= StageDuration)
            {
                crop.StageTimer -= StageDuration;
                crop.Stage++;
            }
        }
    }

    public bool TryPlant(int tileX, int tileY, CropType type, TileMap map)
    {
        if (!InBounds(tileX, tileY) || _crops[Index(tileX, tileY)] != null)
        {
            return false;
        }

        if (!TileIds.IsFarmLand(map.GetTileId(tileX, tileY)))
        {
            return false;
        }

        _crops[Index(tileX, tileY)] = new Crop { Type = type, Stage = 0, StageTimer = 0f };
        return true;
    }

    public void Draw(TileMap map, float scale, Vector2 offset, Texture2D decorTexture)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Crop? crop = _crops[Index(x, y)];
                if (crop == null)
                {
                    continue;
                }

                Rectangle src = CropSprites.GrowthStage(crop.Type, crop.Stage);
                float destX = offset.X + x * map.TileWidth * scale;
                float destY = offset.Y + y * map.TileHeight * scale;
                var dest = new Rectangle(destX, destY, map.TileWidth * scale, map.TileHeight * scale);
                Raylib.DrawTexturePro(decorTexture, src, dest, Vector2.Zero, 0f, Color.WHITE);
            }
        }
    }

    private bool InBounds(int tileX, int tileY) =>
        tileX >= 0 && tileY >= 0 && tileX < _width && tileY < _height;

    private int Index(int tileX, int tileY) => tileY * _width + tileX;

    private sealed class Crop
    {
        public required CropType Type { get; init; }
        public int Stage;
        public float StageTimer;
    }
}
