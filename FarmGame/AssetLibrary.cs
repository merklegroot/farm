using System.Numerics;

namespace FarmGame;

/// <summary>
/// Named collection of code-defined assets and their world placements.
/// </summary>
public sealed class AssetLibrary
{
    private readonly Dictionary<string, GameAsset> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlacedAsset> _placements = [];

    public IReadOnlyList<PlacedAsset> Placements => _placements;

    public void DefineOrReplace(string name, PixelAssetDefinition definition)
    {
        if (_assets.TryGetValue(name, out GameAsset? existing))
        {
            existing.Unload();
        }

        _assets[name] = new GameAsset(definition);
    }

    public bool TryGetAsset(string name, out GameAsset asset)
    {
        if (_assets.TryGetValue(name, out GameAsset? found))
        {
            asset = found;
            return true;
        }

        asset = null!;
        return false;
    }

    public void Place(string name, Vector2 worldPosition)
    {
        if (!_assets.ContainsKey(name))
        {
            throw new KeyNotFoundException($"Asset '{name}' is not defined.");
        }

        _placements.Add(new PlacedAsset(name, worldPosition));
    }

    public void Draw(float scale, Vector2 mapOffset)
    {
        foreach (PlacedAsset placement in _placements)
        {
            _assets[placement.Name].DrawWorld(placement.WorldPosition, scale, mapOffset);
        }
    }

    public void PersistPlacements()
    {
        DefinedAssetStore.SavePlacements(_placements.Select(p => new SavedPlacementEntry
        {
            Asset = p.Name,
            X = p.WorldPosition.X,
            Y = p.WorldPosition.Y,
        }));
    }

    public void RemoveAsset(string name)
    {
        if (_assets.TryGetValue(name, out GameAsset? asset))
        {
            asset.Unload();
            _assets.Remove(name);
        }
    }

    public void RemovePlacementsForAsset(string name)
    {
        _placements.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        PersistPlacements();
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

    public readonly record struct PlacedAsset(string Name, Vector2 WorldPosition);
}
