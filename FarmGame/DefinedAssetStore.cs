using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Raylib_cs;

namespace FarmGame;

public sealed class SavedAssetFile
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string[] Pixels { get; init; }

    public PixelAssetDefinition ToDefinition()
    {
        if (Pixels.Length != Width * Height)
        {
            throw new InvalidDataException($"Asset '{Name}' pixel count does not match {Width}x{Height}.");
        }

        var colors = new Color[Pixels.Length];
        for (int i = 0; i < Pixels.Length; i++)
        {
            colors[i] = ParsePixel(Pixels[i]);
        }

        return new PixelAssetDefinition(Width, Height, colors);
    }

    public static SavedAssetFile FromDefinition(string name, PixelAssetDefinition definition)
    {
        var hex = new string[definition.Pixels.Length];
        for (int i = 0; i < definition.Pixels.Length; i++)
        {
            hex[i] = FormatPixel(definition.Pixels[i]);
        }

        return new SavedAssetFile
        {
            Name = name,
            Width = definition.Width,
            Height = definition.Height,
            Pixels = hex,
        };
    }

    private static string FormatPixel(Color color) =>
        $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    private static Color ParsePixel(string hex)
    {
        if (hex.Length != 8)
        {
            throw new FormatException($"Expected 8 hex digits, got '{hex}'.");
        }

        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        byte a = Convert.ToByte(hex[6..8], 16);
        return new Color(r, g, b, a);
    }
}

public sealed class SavedPlacementsFile
{
    public List<SavedPlacementEntry> Placements { get; init; } = [];
}

public sealed class SavedPlacementEntry
{
    public required string Asset { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
}

public static class DefinedAssetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string AssetsDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets/defined"));

    public static string PlacementsPath => Path.Combine(AssetsDirectory, "placements.json");

    public static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(AssetsDirectory);
    }

    public static string SaveAsset(SavedAssetFile asset)
    {
        EnsureDirectoryExists();
        string fileName = ToAssetFileName(asset.Name);
        if (fileName.Length == 0)
        {
            throw new InvalidOperationException("Asset name must contain letters or numbers.");
        }

        string path = Path.Combine(AssetsDirectory, $"{fileName}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(asset, JsonOptions));
        return fileName;
    }

    public static SavedAssetFile LoadAsset(string name)
    {
        string fileName = ResolveAssetFileStem(name);
        string path = Path.Combine(AssetsDirectory, $"{fileName}.json");
        return JsonSerializer.Deserialize<SavedAssetFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Could not parse asset file '{path}'.");
    }

    public static string ResolveAssetFileStem(string name)
    {
        foreach (string candidate in GetAssetFileNameCandidates(name))
        {
            string path = Path.Combine(AssetsDirectory, $"{candidate}.json");
            if (File.Exists(path))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Saved asset '{name}' was not found.");
    }

    public static IReadOnlyList<string> ListAssetNames()
    {
        EnsureDirectoryExists();
        return Directory.EnumerateFiles(AssetsDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.Equals(name, "placements", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => name!)
            .ToList();
    }

    public static void SavePlacements(IEnumerable<SavedPlacementEntry> placements)
    {
        EnsureDirectoryExists();
        var file = new SavedPlacementsFile { Placements = placements.ToList() };
        File.WriteAllText(PlacementsPath, JsonSerializer.Serialize(file, JsonOptions));
    }

    public static IReadOnlyList<SavedPlacementEntry> LoadPlacements()
    {
        if (!File.Exists(PlacementsPath))
        {
            return [];
        }

        SavedPlacementsFile? file = JsonSerializer.Deserialize<SavedPlacementsFile>(File.ReadAllText(PlacementsPath), JsonOptions);
        return file?.Placements ?? [];
    }

    public static void DeleteAsset(string name)
    {
        try
        {
            string fileName = ResolveAssetFileStem(name);
            string path = Path.Combine(AssetsDirectory, $"{fileName}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (FileNotFoundException)
        {
            // Already removed or never saved.
        }
    }

    public static string SuggestNewAssetName()
    {
        EnsureDirectoryExists();
        var existing = new HashSet<string>(ListAssetNames(), StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < 1000; i++)
        {
            string candidate = $"asset_{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"asset_{Guid.NewGuid():N}"[..16];
    }

    public static string SuggestCloneName(string sourceName)
    {
        EnsureDirectoryExists();
        var existingFiles = new HashSet<string>(ListAssetNames(), StringComparer.OrdinalIgnoreCase);
        string root = GetCloneRootName(sourceName);

        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{root} ({i})";
            if (!existingFiles.Contains(ToAssetFileName(candidate)))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not find an available clone name.");
    }

    public static string ToAssetFileName(string name)
    {
        name = name.Trim();
        Match numbered = Regex.Match(name, @"^(.+?)\s+\((\d+)\)$");
        if (numbered.Success)
        {
            string root = SanitizeFileName(numbered.Groups[1].Value);
            string index = numbered.Groups[2].Value;
            if (root.Length == 0)
            {
                return SanitizeFileName(name);
            }

            return $"{root}_{index}";
        }

        return SanitizeFileName(name);
    }

    private static string GetCloneRootName(string sourceName)
    {
        Match numbered = Regex.Match(sourceName.Trim(), @"^(.+?)\s+\((\d+)\)$");
        return numbered.Success ? numbered.Groups[1].Value.Trim() : sourceName.Trim();
    }

    public static string SanitizeFileName(string name)
    {
        var chars = name
            .Trim()
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_')
            .ToArray();
        return new string(chars).Trim('_');
    }

    private static IEnumerable<string> GetAssetFileNameCandidates(string name)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            yield break;
        }

        yield return trimmed;

        string fromDisplay = ToAssetFileName(trimmed);
        if (!string.Equals(fromDisplay, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            yield return fromDisplay;
        }

        string sanitized = SanitizeFileName(trimmed);
        if (!string.Equals(sanitized, trimmed, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sanitized, fromDisplay, StringComparison.OrdinalIgnoreCase))
        {
            yield return sanitized;
        }
    }
}
