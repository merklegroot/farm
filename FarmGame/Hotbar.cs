using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class Hotbar
{
    private const int SlotSize = ItemSlotUi.SlotSize;
    private const int SlotPadding = ItemSlotUi.SlotPadding;
    private const int BarPadding = 12;
    private const int BarHeight = 72;

    private readonly Inventory _inventory;
    private readonly Texture2D _playerTexture;
    private readonly Texture2D _actionsTexture;
    private readonly Texture2D _decorTexture;

    private int _selectedIndex;

    public Hotbar(
        Inventory inventory,
        Texture2D playerTexture,
        Texture2D actionsTexture,
        Texture2D decorTexture,
        int initialSelection = 0)
    {
        _inventory = inventory;
        _playerTexture = playerTexture;
        _actionsTexture = actionsTexture;
        _decorTexture = decorTexture;
        _selectedIndex = Math.Clamp(initialSelection, 0, Inventory.HotbarSlotCount - 1);
    }

    public int SelectedIndex => _selectedIndex;

    public PlayerTool SelectedTool => _inventory.GetHotbarTool(_selectedIndex);

    public void Update()
    {
        for (int i = 0; i < Inventory.HotbarSlotCount; i++)
        {
            if (Raylib.IsKeyPressed((KeyboardKey)((int)KeyboardKey.KEY_ONE + i)))
            {
                _selectedIndex = i;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
        {
            _selectedIndex = (_selectedIndex + Inventory.HotbarSlotCount - 1) % Inventory.HotbarSlotCount;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_E))
        {
            _selectedIndex = (_selectedIndex + 1) % Inventory.HotbarSlotCount;
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        int slotCount = Inventory.HotbarSlotCount;
        int totalWidth = slotCount * SlotSize + (slotCount - 1) * SlotPadding;
        int barX = (screenWidth - totalWidth) / 2 - BarPadding;
        int barY = screenHeight - BarHeight - BarPadding;
        int barW = totalWidth + BarPadding * 2;
        int barH = BarHeight;

        Raylib.DrawRectangle(barX, barY, barW, barH, new Color(20, 22, 28, 220));
        Raylib.DrawRectangleLines(barX, barY, barW, barH, new Color(60, 65, 80, 255));

        int slotX = barX + BarPadding;
        int slotY = barY + (barH - SlotSize) / 2;

        for (int i = 0; i < slotCount; i++)
        {
            bool selected = i == _selectedIndex;
            var slotRect = new Rectangle(slotX, slotY, SlotSize, SlotSize);
            InventorySlot slot = _inventory.GetSlot(i);

            ItemSlotUi.DrawSlot(
                slotRect,
                slot,
                selected,
                _playerTexture,
                _actionsTexture,
                _decorTexture,
                keyLabel: (i + 1).ToString());

            slotX += SlotSize + SlotPadding;
        }
    }
}
