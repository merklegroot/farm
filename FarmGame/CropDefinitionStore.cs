using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmGame;

public sealed class CropDefinition
{
    public required string Name { get; init; }
    public required string[] Stages { get; init; }
}

public static class CropDefinitionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Dictionary<string, CropDefinition> ByName =
        new(StringComparer.OrdinalIgnoreCase);

    public static string CropsDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets"));

    public static void LoadAll()
    {
        ByName.Clear();

        if (!Directory.Exists(CropsDirectory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(CropsDirectory, "*.json"))
        {
            CropDefinition? definition = TryLoadDefinition(path);
            if (definition != null)
            {
                ByName[definition.Name] = definition;
            }
        }
    }

    public static bool TryGet(string name, out CropDefinition definition) =>
        ByName.TryGetValue(name, out definition!);

    public static IReadOnlyList<string> GetStages(string name)
    {
        if (TryGet(name, out CropDefinition? definition) && definition.Stages.Length > 0)
        {
            return definition.Stages;
        }

        return [];
    }

    private static CropDefinition? TryLoadDefinition(string path)
    {
        try
        {
            CropDefinitionFile? file = JsonSerializer.Deserialize<CropDefinitionFile>(File.ReadAllText(path), JsonOptions);
            if (file?.Stages == null || file.Stages.Length == 0 || file.Frames != null)
            {
                return null;
            }

            string name = string.IsNullOrWhiteSpace(file.Name)
                ? Path.GetFileNameWithoutExtension(path)
                : file.Name.Trim();

            return new CropDefinition
            {
                Name = name,
                Stages = file.Stages,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class CropDefinitionFile
    {
        public string? Name { get; init; }
        public string[]? Stages { get; init; }
        public string[]? Frames { get; init; }
    }
}
