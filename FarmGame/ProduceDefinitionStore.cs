using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FarmGame;

public sealed class ProduceDefinition
{
    public required string Name { get; init; }
    public required string[] Frames { get; init; }
}

public static class ProduceDefinitionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string ProduceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets"));

    public static void EnsureDirectoryExists() =>
        Directory.CreateDirectory(ProduceDirectory);

    public static string Save(ProduceDefinition definition)
    {
        EnsureDirectoryExists();
        string fileName = ToFileName(definition.Name);
        if (fileName.Length == 0)
        {
            throw new InvalidOperationException("Produce name must contain letters or numbers.");
        }

        string path = Path.Combine(ProduceDirectory, $"{fileName}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(definition, JsonOptions));
        return fileName;
    }

    public static ProduceDefinition Load(string name)
    {
        string fileName = ResolveFileStem(name);
        string path = Path.Combine(ProduceDirectory, $"{fileName}.json");
        ProduceDefinition? definition = TryLoadDefinition(path);
        return definition ?? throw new InvalidDataException($"Could not parse produce file '{path}'.");
    }

    public static string ResolveFileStem(string name)
    {
        foreach (string candidate in GetFileNameCandidates(name))
        {
            if (File.Exists(Path.Combine(ProduceDirectory, $"{candidate}.json")))
            {
                ProduceDefinition? definition = TryLoadDefinition(Path.Combine(ProduceDirectory, $"{candidate}.json"));
                if (definition != null)
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException($"Produce '{name}' was not found.");
    }

    public static IReadOnlyList<string> ListNames()
    {
        EnsureDirectoryExists();
        var names = new List<string>();
        foreach (string path in Directory.EnumerateFiles(ProduceDirectory, "*.json"))
        {
            if (TryLoadDefinition(path) != null)
            {
                names.Add(Path.GetFileNameWithoutExtension(path)!);
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public static void Delete(string name)
    {
        try
        {
            string fileName = ResolveFileStem(name);
            string path = Path.Combine(ProduceDirectory, $"{fileName}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (FileNotFoundException)
        {
            // Already removed.
        }
    }

    public static string SuggestNewName()
    {
        EnsureDirectoryExists();
        var existing = new HashSet<string>(ListNames(), StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < 1000; i++)
        {
            string candidate = $"produce_{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"produce_{Guid.NewGuid():N}"[..16];
    }

    public static string SuggestCloneName(string sourceName)
    {
        EnsureDirectoryExists();
        var existingFiles = new HashSet<string>(ListNames(), StringComparer.OrdinalIgnoreCase);
        string root = GetCloneRootName(sourceName);

        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{root}_{i}";
            if (!existingFiles.Contains(ToFileName(candidate)))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not find an available clone name.");
    }

    public static string ToFileName(string name) =>
        DefinedAssetStore.SanitizeFileName(name.Trim());

    private static ProduceDefinition? TryLoadDefinition(string path)
    {
        try
        {
            ProduceDefinitionFile? file = JsonSerializer.Deserialize<ProduceDefinitionFile>(File.ReadAllText(path), JsonOptions);
            if (file?.Frames == null || file.Frames.Length == 0)
            {
                return null;
            }

            string name = string.IsNullOrWhiteSpace(file.Name)
                ? Path.GetFileNameWithoutExtension(path)
                : file.Name.Trim();

            return new ProduceDefinition
            {
                Name = name,
                Frames = file.Frames,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<string> GetFileNameCandidates(string name)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            yield break;
        }

        yield return trimmed;

        string sanitized = ToFileName(trimmed);
        if (!string.Equals(sanitized, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            yield return sanitized;
        }
    }

    private static string GetCloneRootName(string sourceName)
    {
        string trimmed = sourceName.Trim();
        Match parenNumbered = Regex.Match(trimmed, @"^(.+?)\s+\((\d+)\)$");
        if (parenNumbered.Success)
        {
            return parenNumbered.Groups[1].Value.Trim();
        }

        Match underscoreNumbered = Regex.Match(trimmed, @"^(.+?)_(\d+)$");
        if (underscoreNumbered.Success)
        {
            return underscoreNumbered.Groups[1].Value.Trim();
        }

        return trimmed;
    }

    private sealed class ProduceDefinitionFile
    {
        public string? Name { get; init; }
        public string[]? Frames { get; init; }
    }
}
