namespace FarmGame;

public readonly struct InventorySlot
{
    public PlayerTool? Tool { get; init; }
    public int Count { get; init; }

    public bool IsEmpty => Tool == null || Count == 0;

    public static InventorySlot Empty => default;

    public static InventorySlot Of(PlayerTool tool, int count) =>
        new() { Tool = tool, Count = count };

    public static InventorySlot Infinite(PlayerTool tool) =>
        new() { Tool = tool, Count = -1 };
}

public sealed class Inventory
{
    public const int HotbarSlotCount = 9;
    public const int BackpackSlotCount = 27;
    public const int TotalSlotCount = HotbarSlotCount + BackpackSlotCount;

    public const int HotbarStartIndex = 0;
    public const int BackpackStartIndex = HotbarSlotCount;

    private readonly InventorySlot[] _slots = new InventorySlot[TotalSlotCount];

    public Inventory()
    {
        SeedDefaults();
    }

    public InventorySlot GetSlot(int index) =>
        index is >= 0 and < TotalSlotCount ? _slots[index] : InventorySlot.Empty;

    public PlayerTool GetHotbarTool(int hotbarIndex)
    {
        if (hotbarIndex is < 0 or >= HotbarSlotCount)
        {
            return PlayerTool.Hands;
        }

        InventorySlot slot = _slots[hotbarIndex];
        return slot.IsEmpty ? PlayerTool.Hands : slot.Tool!.Value;
    }

    public int GetHotbarCount(int hotbarIndex)
    {
        if (hotbarIndex is < 0 or >= HotbarSlotCount)
        {
            return 0;
        }

        return _slots[hotbarIndex].Count;
    }

    public bool TryConsumeFromHotbar(int hotbarIndex, PlayerTool tool, int amount = 1)
    {
        if (hotbarIndex is < 0 or >= HotbarSlotCount || amount <= 0)
        {
            return false;
        }

        ref InventorySlot slot = ref _slots[hotbarIndex];
        if (slot.IsEmpty || slot.Tool != tool)
        {
            return false;
        }

        if (PlayerToolInfo.IsInfinite(tool))
        {
            return true;
        }

        if (slot.Count < amount)
        {
            return false;
        }

        int remaining = slot.Count - amount;
        slot = remaining > 0 ? InventorySlot.Of(tool, remaining) : InventorySlot.Empty;
        return true;
    }

    public bool HotbarHasUsable(int hotbarIndex, PlayerTool tool)
    {
        if (hotbarIndex is < 0 or >= HotbarSlotCount)
        {
            return false;
        }

        InventorySlot slot = _slots[hotbarIndex];
        if (slot.IsEmpty)
        {
            return tool == PlayerTool.Hands;
        }

        if (slot.Tool != tool)
        {
            return false;
        }

        return PlayerToolInfo.IsInfinite(tool) || slot.Count > 0;
    }

    public void SwapSlots(int a, int b)
    {
        if (a == b || a is < 0 or >= TotalSlotCount || b is < 0 or >= TotalSlotCount)
        {
            return;
        }

        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
    }

    private void SeedDefaults()
    {
        _slots[0] = InventorySlot.Infinite(PlayerTool.Hands);
        _slots[1] = InventorySlot.Infinite(PlayerTool.Pickaxe);
        _slots[2] = InventorySlot.Infinite(PlayerTool.Axe);
        _slots[3] = InventorySlot.Infinite(PlayerTool.Hoe);
        _slots[4] = InventorySlot.Infinite(PlayerTool.WateringCan);
        _slots[5] = InventorySlot.Of(PlayerTool.TomatoSeeds, 99);
        _slots[6] = InventorySlot.Of(PlayerTool.CornSeeds, 99);
        _slots[7] = InventorySlot.Of(PlayerTool.CarrotSeeds, 15);
        _slots[8] = InventorySlot.Of(PlayerTool.WheatSeeds, 15);

        _slots[9] = InventorySlot.Of(PlayerTool.CarrotSeeds, 30);
        _slots[10] = InventorySlot.Of(PlayerTool.WheatSeeds, 30);
        _slots[11] = InventorySlot.Of(PlayerTool.TomatoSeeds, 50);
        _slots[12] = InventorySlot.Of(PlayerTool.CornSeeds, 50);
    }
}
