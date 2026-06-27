using System.Numerics;

namespace FarmGame;

/// <summary>
/// Named collection of code-defined assets and their world placements.
/// </summary>
public sealed class AssetLibrary
{
    private readonly Dictionary<string, GameAsset> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlacedAsset> _placements = [];

    public void Define(string name, PixelAssetDefinition definition)
    {
        if (_assets.ContainsKey(name))
        {
            throw new InvalidOperationException($"Asset '{name}' is already defined.");
        }

        _assets[name] = new GameAsset(definition);
    }

    public void Place(string name, Vector2 worldPosition)
    {
        if (!_assets.ContainsKey(name))
        {
            throw new KeyNotFoundException($"Asset '{name}' is not defined.");
        }

        _placements.Add(new PlacedAsset(name, worldPosition));
    }

    public void PlaceCustom(PixelAssetDefinition definition, Vector2 worldPosition)
    {
        string name = $"runtime_{_placements.Count}_{_assets.Count}";
        Define(name, definition);
        Place(name, worldPosition);
    }

    public void Draw(float scale, Vector2 mapOffset)
    {
        foreach (PlacedAsset placement in _placements)
        {
            _assets[placement.Name].DrawWorld(placement.WorldPosition, scale, mapOffset);
        }
    }

    public void Unload()
    {
        foreach (GameAsset asset in _assets.Values)
        {
            asset.Unload();
        }

        _assets.Clear();
        _placements.Clear();
    }

    private readonly record struct PlacedAsset(string Name, Vector2 WorldPosition);
}
