using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FarmGame;

public sealed class SavedAsset
{
    public required string Name { get; init; }
    public required PixelAssetDefinition Definition { get; init; }
}

public sealed class SavedAssetMeta
{
    public required string Name { get; init; }
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
        MigrateLegacyJsonAssets();
    }

    public static string SaveAsset(SavedAsset asset)
    {
        EnsureDirectoryExists();
        string fileName = ToAssetFileName(asset.Name);
        if (fileName.Length == 0)
        {
            throw new InvalidOperationException("Asset name must contain letters or numbers.");
        }

        string pngPath = AssetPngPath(fileName);
        asset.Definition.SaveToPng(pngPath);
        SaveMeta(fileName, asset.Name);
        return fileName;
    }

    public static SavedAsset LoadAsset(string name)
    {
        string fileName = ResolveAssetFileStem(name);
        string pngPath = AssetPngPath(fileName);
        PixelAssetDefinition definition = PixelAssetDefinition.FromPng(pngPath);
        string displayName = LoadDisplayName(fileName);
        return new SavedAsset
        {
            Name = displayName,
            Definition = definition,
        };
    }

    public static string ResolveAssetFileStem(string name)
    {
        foreach (string candidate in GetAssetFileNameCandidates(name))
        {
            if (File.Exists(AssetPngPath(candidate)))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Saved asset '{name}' was not found.");
    }

    public static IReadOnlyList<string> ListAssetNames()
    {
        EnsureDirectoryExists();
        return Directory.EnumerateFiles(AssetsDirectory, "*.png")
            .Select(Path.GetFileNameWithoutExtension)
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
            string pngPath = AssetPngPath(fileName);
            if (File.Exists(pngPath))
            {
                File.Delete(pngPath);
            }

            string metaPath = AssetMetaPath(fileName);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
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

    public static string SanitizeFileName(string name)
    {
        var chars = name
            .Trim()
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_')
            .ToArray();
        return new string(chars).Trim('_');
    }

    public static void MigrateLegacyJsonAssets()
    {
        if (!Directory.Exists(AssetsDirectory))
        {
            return;
        }

        foreach (string jsonPath in Directory.EnumerateFiles(AssetsDirectory, "*.json"))
        {
            string fileStem = Path.GetFileNameWithoutExtension(jsonPath);
            if (string.Equals(fileStem, "placements", StringComparison.OrdinalIgnoreCase) ||
                jsonPath.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string pngPath = AssetPngPath(fileStem);
            if (File.Exists(pngPath))
            {
                continue;
            }

            LegacySavedAssetFile? legacy = JsonSerializer.Deserialize<LegacySavedAssetFile>(File.ReadAllText(jsonPath), JsonOptions);
            if (legacy?.Pixels == null)
            {
                continue;
            }

            PixelAssetDefinition definition = legacy.ToDefinition();
            definition.SaveToPng(pngPath);
            SaveMeta(fileStem, legacy.Name);
            File.Delete(jsonPath);
        }
    }

    private static string AssetPngPath(string fileStem) =>
        Path.Combine(AssetsDirectory, $"{fileStem}.png");

    private static string AssetMetaPath(string fileStem) =>
        Path.Combine(AssetsDirectory, $"{fileStem}.meta.json");

    private static string LoadDisplayName(string fileStem)
    {
        string metaPath = AssetMetaPath(fileStem);
        if (File.Exists(metaPath))
        {
            SavedAssetMeta? meta = JsonSerializer.Deserialize<SavedAssetMeta>(File.ReadAllText(metaPath), JsonOptions);
            if (!string.IsNullOrWhiteSpace(meta?.Name))
            {
                return meta.Name;
            }
        }

        return fileStem;
    }

    private static void SaveMeta(string fileStem, string displayName)
    {
        string metaPath = AssetMetaPath(fileStem);
        if (string.Equals(displayName, fileStem, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            return;
        }

        var meta = new SavedAssetMeta { Name = displayName };
        File.WriteAllText(metaPath, JsonSerializer.Serialize(meta, JsonOptions));
    }

    private static string GetCloneRootName(string sourceName)
    {
        Match numbered = Regex.Match(sourceName.Trim(), @"^(.+?)\s+\((\d+)\)$");
        return numbered.Success ? numbered.Groups[1].Value.Trim() : sourceName.Trim();
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

    private sealed class LegacySavedAssetFile
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

            var colors = new Raylib_cs.Color[Pixels.Length];
            for (int i = 0; i < Pixels.Length; i++)
            {
                colors[i] = ParsePixel(Pixels[i]);
            }

            return new PixelAssetDefinition(Width, Height, colors);
        }

        private static Raylib_cs.Color ParsePixel(string hex)
        {
            if (hex.Length != 8)
            {
                throw new FormatException($"Expected 8 hex digits, got '{hex}'.");
            }

            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            byte a = Convert.ToByte(hex[6..8], 16);
            return new Raylib_cs.Color(r, g, b, a);
        }
    }
}
