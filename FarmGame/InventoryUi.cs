using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class InventoryUi
{
    private const int Columns = 9;
    private const int BackpackRows = 3;
    private const int PanelPadding = 16;
    private const int SectionGap = 12;

    private readonly Texture2D _playerTexture;
    private readonly Texture2D _actionsTexture;
    private readonly Texture2D _decorTexture;

    private bool _isOpen;
    private int? _selectedSlotIndex;
    private Rectangle _panelRect;

    public InventoryUi(Texture2D playerTexture, Texture2D actionsTexture, Texture2D decorTexture)
    {
        _playerTexture = playerTexture;
        _actionsTexture = actionsTexture;
        _decorTexture = decorTexture;
    }

    public bool IsOpen => _isOpen;

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _selectedSlotIndex = null;
    }

    public void Close()
    {
        _isOpen = false;
        _selectedSlotIndex = null;
    }

    public void Update(int screenWidth, int screenHeight, Inventory inventory)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_I))
        {
            Toggle();
            return;
        }

        if (!_isOpen)
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            Close();
            return;
        }

        LayoutPanel(screenWidth, screenHeight);
        HandleSlotClicks(inventory);
    }

    public void Draw(int screenWidth, int screenHeight, Inventory inventory, int hotbarSelectedIndex)
    {
        if (!_isOpen)
        {
            return;
        }

        LayoutPanel(screenWidth, screenHeight);

        Raylib.DrawRectangleRec(_panelRect, new Color(20, 22, 28, 245));
        Raylib.DrawRectangleLinesEx(_panelRect, 2f, new Color(90, 95, 110, 255));

        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)_panelRect.Y + PanelPadding;

        UiText.DrawText("Inventory", x, y, 22, Color.WHITE);
        y += 30;

        UiText.DrawText("Backpack", x, y, 14, new Color(150, 155, 170, 255));
        y += 20;

        for (int row = 0; row < BackpackRows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int slotIndex = Inventory.BackpackStartIndex + row * Columns + col;
                DrawInventorySlot(inventory, slotIndex, x, y, col, row, hotbarSelectedIndex: -1);
            }

            y += ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding;
        }

        y += SectionGap;
        UiText.DrawText("Hotbar", x, y, 14, new Color(150, 155, 170, 255));
        y += 20;

        for (int col = 0; col < Inventory.HotbarSlotCount; col++)
        {
            DrawInventorySlot(inventory, col, x, y, col, 0, hotbarSelectedIndex);
        }

        y += ItemSlotUi.SlotSize + 8;
        UiText.DrawText("Click two slots to swap · I or Esc to close", x, y, 14, new Color(130, 135, 150, 255));
    }

    public bool IsMouseOverPanel(Vector2 screenPos) =>
        _isOpen && Raylib.CheckCollisionPointRec(screenPos, _panelRect);

    public static bool IsMouseOverHotbar(Vector2 screenPos, int screenHeight)
    {
        const int barHeight = 72;
        const int barPadding = 12;
        return screenPos.Y >= screenHeight - barHeight - barPadding * 2;
    }

    public static bool BlocksGameplayInput(Vector2 screenPos, int screenWidth, int screenHeight, bool inventoryOpen, bool inventoryHit)
    {
        if (inventoryOpen)
        {
            return true;
        }

        return IsMouseOverHotbar(screenPos, screenHeight);
    }

    private void LayoutPanel(int screenWidth, int screenHeight)
    {
        int gridWidth = Columns * ItemSlotUi.SlotSize + (Columns - 1) * ItemSlotUi.SlotPadding;
        int backpackHeight = BackpackRows * ItemSlotUi.SlotSize + (BackpackRows - 1) * ItemSlotUi.SlotPadding;
        int hotbarHeight = ItemSlotUi.SlotSize;
        int panelWidth = gridWidth + PanelPadding * 2;
        int panelHeight = PanelPadding * 2 + 30 + 20 + backpackHeight + SectionGap + 20 + hotbarHeight + 28;

        float panelX = (screenWidth - panelWidth) * 0.5f;
        float panelY = (screenHeight - panelHeight) * 0.5f;
        _panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
    }

    private void DrawInventorySlot(
        Inventory inventory,
        int slotIndex,
        int originX,
        int originY,
        int col,
        int row,
        int hotbarSelectedIndex)
    {
        var slotRect = new Rectangle(
            originX + col * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
            originY + row * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
            ItemSlotUi.SlotSize,
            ItemSlotUi.SlotSize);

        bool selected = _selectedSlotIndex == slotIndex;
        bool hotbarHighlight = slotIndex == hotbarSelectedIndex;
        InventorySlot slot = inventory.GetSlot(slotIndex);

        ItemSlotUi.DrawSlot(
            slotRect,
            slot,
            selected || hotbarHighlight,
            _playerTexture,
            _actionsTexture,
            _decorTexture);

        if (hotbarHighlight && !selected)
        {
            Raylib.DrawRectangleLinesEx(slotRect, 2f, new Color(120, 170, 220, 255));
        }
    }

    private void HandleSlotClicks(Inventory inventory)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _panelRect))
        {
            return;
        }

        int? clicked = GetSlotIndexAt(mouse);
        if (clicked == null)
        {
            return;
        }

        if (_selectedSlotIndex == null)
        {
            _selectedSlotIndex = clicked;
            return;
        }

        inventory.SwapSlots(_selectedSlotIndex.Value, clicked.Value);
        _selectedSlotIndex = null;
    }

    private int? GetSlotIndexAt(Vector2 mouse)
    {
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)_panelRect.Y + PanelPadding + 30 + 20;

        for (int row = 0; row < BackpackRows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                var slotRect = new Rectangle(
                    x + col * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
                    y + row * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
                    ItemSlotUi.SlotSize,
                    ItemSlotUi.SlotSize);

                if (Raylib.CheckCollisionPointRec(mouse, slotRect))
                {
                    return Inventory.BackpackStartIndex + row * Columns + col;
                }
            }
        }

        y += BackpackRows * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding) + SectionGap + 20;

        for (int col = 0; col < Inventory.HotbarSlotCount; col++)
        {
            var slotRect = new Rectangle(
                x + col * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
                y,
                ItemSlotUi.SlotSize,
                ItemSlotUi.SlotSize);

            if (Raylib.CheckCollisionPointRec(mouse, slotRect))
            {
                return col;
            }
        }

        return null;
    }
}
