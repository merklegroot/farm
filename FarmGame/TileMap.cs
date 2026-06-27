using System.Numerics;
using System.Xml.Linq;
using Raylib_cs;

namespace FarmGame;

public sealed class TileMap
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int TileWidth { get; init; }
    public required int TileHeight { get; init; }

    public required int TilesetFirstGid { get; init; }
    public required int TilesetColumns { get; init; }
    public required Texture2D TilesetTexture { get; init; }

    /// <summary>Tile GID grids per layer, bottom-to-top (same order as in the TMX).</summary>
    public required int[][] LayerGids { get; init; }

    public static TileMap LoadFromTmx(string tmxPath)
    {
        var doc = XDocument.Load(tmxPath);
        XElement mapEl = doc.Root ?? throw new InvalidOperationException("TMX missing <map> root.");

        int width = (int)mapEl.Attribute("width")!;
        int height = (int)mapEl.Attribute("height")!;
        int tileWidth = (int)mapEl.Attribute("tilewidth")!;
        int tileHeight = (int)mapEl.Attribute("tileheight")!;

        XElement tilesetEl = mapEl.Element("tileset") ?? throw new InvalidOperationException("TMX missing <tileset>.");
        int firstGid = (int)tilesetEl.Attribute("firstgid")!;
        string tsxSource = (string?)tilesetEl.Attribute("source") ?? throw new InvalidOperationException("TMX tileset missing source.");

        string tmxDir = Path.GetDirectoryName(tmxPath) ?? ".";
        string tsxPath = Path.GetFullPath(Path.Combine(tmxDir, tsxSource));

        (int columns, string imagePath) = LoadTsx(tsxPath);
        Texture2D texture = Raylib.LoadTexture(imagePath);
        Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_POINT);

        var layerList = new List<int[]>();
        foreach (XElement layerEl in mapEl.Elements("layer"))
        {
            XElement dataEl = layerEl.Element("data") ?? throw new InvalidOperationException($"Layer '{(string?)layerEl.Attribute("name")}' missing <data>.");
            string encoding = (string?)dataEl.Attribute("encoding") ?? "";
            if (!string.Equals(encoding, "csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Unsupported TMX encoding '{encoding}' in layer '{(string?)layerEl.Attribute("name")}'. Expected csv.");
            }

            layerList.Add(ParseCsvGids(dataEl.Value, width * height));
        }

        if (layerList.Count == 0)
        {
            throw new InvalidOperationException("TMX missing <layer> elements.");
        }

        return new TileMap
        {
            Width = width,
            Height = height,
            TileWidth = tileWidth,
            TileHeight = tileHeight,
            TilesetFirstGid = firstGid,
            TilesetColumns = columns,
            TilesetTexture = texture,
            LayerGids = layerList.ToArray()
        };
    }

    public float PixelWidth => Width * TileWidth;
    public float PixelHeight => Height * TileHeight;

    public int GetGid(int tileX, int tileY, int layer = 0)
    {
        if (!InBounds(tileX, tileY))
        {
            return 0;
        }

        return ClearGidFlags(LayerGids[layer][tileY * Width + tileX]);
    }

    public int GetTileId(int tileX, int tileY, int layer = 0)
    {
        int gid = GetGid(tileX, tileY, layer);
        return gid > 0 ? gid - TilesetFirstGid : -1;
    }

    public bool TryHoe(int tileX, int tileY)
    {
        if (!InBounds(tileX, tileY))
        {
            return false;
        }

        int layer = 0;
        int idx = tileY * Width + tileX;
        int gid = ClearGidFlags(LayerGids[layer][idx]);
        int tileId = gid - TilesetFirstGid;

        if (tileId < 0 || !TileIds.IsHoeable(tileId))
        {
            return false;
        }

        LayerGids[layer][idx] = TilesetFirstGid + TileIds.FarmLandCenter;
        RefreshFarmLandNeighbors(tileX, tileY, layer);
        return true;
    }

    public static (int X, int Y) WorldToTile(Vector2 worldPos, float mapScale, int tileWidth, int tileHeight)
    {
        float tileWorldW = tileWidth * mapScale;
        float tileWorldH = tileHeight * mapScale;
        int x = (int)MathF.Floor(worldPos.X / tileWorldW);
        int y = (int)MathF.Floor(worldPos.Y / tileWorldH);
        return (x, y);
    }

    public void Draw(float scale, Vector2 offset)
    {
        foreach (int[] layer in LayerGids)
        {
            DrawLayer(layer, scale, offset);
        }
    }

    public void Unload()
    {
        Raylib.UnloadTexture(TilesetTexture);
    }

    private void DrawLayer(int[] layer, float scale, Vector2 offset)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int gid = ClearGidFlags(layer[y * Width + x]);
                if (gid == 0)
                {
                    continue;
                }

                int tileId = gid - TilesetFirstGid;
                if (tileId < 0)
                {
                    continue;
                }

                int srcX = (tileId % TilesetColumns) * TileWidth;
                int srcY = (tileId / TilesetColumns) * TileHeight;
                var src = new Rectangle(srcX, srcY, TileWidth, TileHeight);

                float destX = offset.X + x * TileWidth * scale;
                float destY = offset.Y + y * TileHeight * scale;
                var dest = new Rectangle(destX, destY, TileWidth * scale, TileHeight * scale);

                Raylib.DrawTexturePro(TilesetTexture, src, dest, Vector2.Zero, 0f, Color.WHITE);
            }
        }
    }

    private static int ClearGidFlags(int gid) => gid & 0x1FFFFFFF;

    private bool InBounds(int tileX, int tileY) =>
        tileX >= 0 && tileY >= 0 && tileX < Width && tileY < Height;

    private bool IsFarmLandAt(int tileX, int tileY, int layer)
    {
        if (!InBounds(tileX, tileY))
        {
            return false;
        }

        return TileIds.IsFarmLand(GetTileId(tileX, tileY, layer));
    }

    private void RefreshFarmLandNeighbors(int centerX, int centerY, int layer)
    {
        for (int y = centerY - 1; y <= centerY + 1; y++)
        {
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                if (!InBounds(x, y) || !IsFarmLandAt(x, y, layer))
                {
                    continue;
                }

                bool n = IsFarmLandAt(x, y - 1, layer);
                bool e = IsFarmLandAt(x + 1, y, layer);
                bool s = IsFarmLandAt(x, y + 1, layer);
                bool w = IsFarmLandAt(x - 1, y, layer);
                int tileId = SelectFarmLandTile(n, e, s, w);
                LayerGids[layer][y * Width + x] = TilesetFirstGid + tileId;
            }
        }
    }

    /// <summary>
    /// 3×3 farmland blob in tileset indices 72–80:
    ///   72 73 74
    ///   75 76 77
    ///   78 79 80
    /// Neighbor mask: N=1, E=2, S=4, W=8 (bit set when that neighbor is also farmland).
    /// </summary>
    private static int SelectFarmLandTile(bool n, bool e, bool s, bool w)
    {
        int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
        int offset = mask switch
        {
            0 => 4,
            1 => 7,
            2 => 3,
            3 => 6,
            4 => 1,
            5 => 3,
            6 => 0,
            7 => 7,
            8 => 5,
            9 => 8,
            10 => 1,
            11 => 3,
            12 => 2,
            13 => 5,
            14 => 1,
            15 => 4,
            _ => 4,
        };

        return TileIds.FarmLandBase + offset;
    }

    private static (int columns, string imagePath) LoadTsx(string tsxPath)
    {
        var doc = XDocument.Load(tsxPath);
        XElement tsEl = doc.Root ?? throw new InvalidOperationException("TSX missing <tileset> root.");
        int columns = (int)tsEl.Attribute("columns")!;

        XElement imageEl = tsEl.Element("image") ?? throw new InvalidOperationException("TSX missing <image>.");
        string imageSource = (string?)imageEl.Attribute("source") ?? throw new InvalidOperationException("TSX image missing source.");

        string tsxDir = Path.GetDirectoryName(tsxPath) ?? ".";
        string imagePath = Path.GetFullPath(Path.Combine(tsxDir, imageSource));
        return (columns, imagePath);
    }

    private static int[] ParseCsvGids(string csv, int expectedCount)
    {
        var gids = new int[expectedCount];
        int i = 0;
        foreach (string part in csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (i >= expectedCount)
            {
                break;
            }

            gids[i++] = int.Parse(part);
        }

        if (i != expectedCount)
        {
            throw new InvalidOperationException($"TMX CSV had {i} entries, expected {expectedCount}.");
        }

        return gids;
    }
}
