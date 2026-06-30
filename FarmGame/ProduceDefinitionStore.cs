using System.Text.Json;
using System.Text.Json.Serialization;

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
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets/produce"));

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
        ProduceDefinition? definition = JsonSerializer.Deserialize<ProduceDefinition>(File.ReadAllText(path), JsonOptions);
        return definition ?? throw new InvalidDataException($"Could not parse produce file '{path}'.");
    }

    public static string ResolveFileStem(string name)
    {
        foreach (string candidate in GetFileNameCandidates(name))
        {
            if (File.Exists(Path.Combine(ProduceDirectory, $"{candidate}.json")))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Produce '{name}' was not found.");
    }

    public static IReadOnlyList<string> ListNames()
    {
        EnsureDirectoryExists();
        return Directory.EnumerateFiles(ProduceDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => name!)
            .ToList();
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

    public static string ToFileName(string name) =>
        DefinedAssetStore.SanitizeFileName(name.Trim());

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
}
