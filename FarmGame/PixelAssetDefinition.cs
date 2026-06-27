using Raylib_cs;

namespace FarmGame;

/// <summary>
/// Immutable pixel art defined in code (e.g. ASCII rows + palette).
/// </summary>
public sealed class PixelAssetDefinition
{
    public int Width { get; }
    public int Height { get; }
    public Color[] Pixels { get; }

    public PixelAssetDefinition(int width, int height, Color[] pixels)
    {
        if (pixels.Length != width * height)
        {
            throw new ArgumentException($"Expected {width * height} pixels, got {pixels.Length}.");
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    /// <summary>
    /// Build from text rows. Use '.' for transparent pixels. All rows must share the same width.
    /// </summary>
    public static PixelAssetDefinition FromRows(IReadOnlyDictionary<char, Color> palette, params string[] rows)
    {
        if (rows.Length == 0)
        {
            throw new ArgumentException("At least one row is required.");
        }

        int width = rows[0].Length;
        var pixels = new Color[width * rows.Length];

        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y];
            if (row.Length != width)
            {
                throw new ArgumentException($"Row {y} width {row.Length} does not match first row width {width}.");
            }

            for (int x = 0; x < width; x++)
            {
                char key = row[x];
                if (key == '.')
                {
                    pixels[y * width + x] = Color.BLANK;
                    continue;
                }

                if (!palette.TryGetValue(key, out Color color))
                {
                    throw new ArgumentException($"Character '{key}' at ({x},{y}) is missing from the palette.");
                }

                pixels[y * width + x] = color;
            }
        }

        return new PixelAssetDefinition(width, rows.Length, pixels);
    }

    public static PixelAssetDefinition FromPixels(int width, int height, ReadOnlySpan<Color> pixels)
    {
        var copy = new Color[width * height];
        pixels.CopyTo(copy);
        return new PixelAssetDefinition(width, height, copy);
    }
}
