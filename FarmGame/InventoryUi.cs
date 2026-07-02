using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class InventoryUi
{
    private const int Columns = 9;
    private const int BackpackRows = 3;
    private const int PanelPadding = 16;
    private const int SectionGap = 12;
    private const float DragThreshold = 4f;
    private const float CancelDropRadiusFactor = 0.25f;

    private readonly Texture2D _playerTexture;
    private readonly Texture2D _actionsTexture;
    private readonly Texture2D _decorTexture;

    private bool _isOpen;
    private int? _selectedSlotIndex;
    private int? _pressSlotIndex;
    private Vector2 _pressMouse;
    private bool _isDragging;
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
        ResetInteraction();
    }

    public void Close()
    {
        _isOpen = false;
        ResetInteraction();
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
        HandleSlotInput(inventory);
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

        int? dropTarget = null;
        if (_isDragging && _pressSlotIndex != null)
        {
            dropTarget = ResolveDropTarget(Raylib.GetMousePosition(), _pressSlotIndex.Value);
        }

        int backpackY = y;
        for (int row = 0; row < BackpackRows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int slotIndex = Inventory.BackpackStartIndex + row * Columns + col;
                DrawInventorySlot(
                    inventory,
                    slotIndex,
                    GetSlotRect(x, backpackY, col),
                    hotbarSelectedIndex,
                    dropTarget);
            }

            backpackY += ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding;
        }

        y = backpackY + SectionGap;
        UiText.DrawText("Hotbar", x, y, 14, new Color(150, 155, 170, 255));
        y += 20;

        for (int col = 0; col < Inventory.HotbarSlotCount; col++)
        {
            DrawInventorySlot(
                inventory,
                col,
                GetSlotRect(x, y, col),
                hotbarSelectedIndex,
                dropTarget);
        }

        if (_isDragging && _pressSlotIndex != null)
        {
            InventorySlot dragged = inventory.GetSlot(_pressSlotIndex.Value);
            if (!dragged.IsEmpty)
            {
                ItemSlotUi.DrawFloatingSlot(
                    Raylib.GetMousePosition(),
                    dragged,
                    _playerTexture,
                    _actionsTexture,
                    _decorTexture);
            }
        }

        y += ItemSlotUi.SlotSize + 8;
        UiText.DrawText("Click two slots to swap · drag to move · I or Esc to close", x, y, 14, new Color(130, 135, 150, 255));
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

    private void ResetInteraction()
    {
        _selectedSlotIndex = null;
        _pressSlotIndex = null;
        _isDragging = false;
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

    private static Rectangle GetSlotRect(int originX, int originY, int col) =>
        new Rectangle(
            originX + col * (ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding),
            originY,
            ItemSlotUi.SlotSize,
            ItemSlotUi.SlotSize);

    private void DrawInventorySlot(
        Inventory inventory,
        int slotIndex,
        Rectangle slotRect,
        int hotbarSelectedIndex,
        int? dropTargetIndex)
    {
        bool selected = _selectedSlotIndex == slotIndex;
        bool hotbarHighlight = slotIndex == hotbarSelectedIndex;
        bool isDragSource = _isDragging && _pressSlotIndex == slotIndex;
        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (isDragSource)
        {
            slot = InventorySlot.Empty;
        }

        ItemSlotUi.DrawSlot(
            slotRect,
            slot,
            selected || hotbarHighlight,
            _playerTexture,
            _actionsTexture,
            _decorTexture);

        if (hotbarHighlight && !selected && !isDragSource)
        {
            Raylib.DrawRectangleLinesEx(slotRect, 2f, new Color(120, 170, 220, 255));
        }

        if (dropTargetIndex == slotIndex)
        {
            Raylib.DrawRectangleLinesEx(slotRect, 2f, new Color(140, 220, 140, 255));
        }
        else if (_isDragging && _pressSlotIndex == slotIndex && IsNearSourceCancelZone(Raylib.GetMousePosition(), slotIndex))
        {
            Raylib.DrawRectangleLinesEx(slotRect, 2f, new Color(220, 120, 120, 255));
        }
    }

    private bool IsNearSourceCancelZone(Vector2 mouse, int sourceIndex)
    {
        foreach ((int index, Rectangle rect) in EnumerateSlotRects())
        {
            if (index != sourceIndex)
            {
                continue;
            }

            float cancelRadius = ItemSlotUi.SlotSize * CancelDropRadiusFactor;
            return Vector2.Distance(mouse, GetRectCenter(rect)) < cancelRadius;
        }

        return false;
    }

    private int? ResolveDropTarget(Vector2 mouse, int sourceIndex)
    {
        Rectangle? sourceRect = null;
        foreach ((int index, Rectangle rect) in EnumerateSlotRects())
        {
            if (index == sourceIndex)
            {
                sourceRect = rect;
                break;
            }
        }

        if (sourceRect != null)
        {
            float cancelRadius = ItemSlotUi.SlotSize * CancelDropRadiusFactor;
            if (Vector2.Distance(mouse, GetRectCenter(sourceRect.Value)) < cancelRadius)
            {
                return null;
            }
        }

        int? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach ((int index, Rectangle rect) in EnumerateSlotRects())
        {
            if (index == sourceIndex)
            {
                continue;
            }

            float distance = Vector2.Distance(mouse, GetRectCenter(rect));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = index;
            }
        }

        return nearest;
    }

    private IEnumerable<(int Index, Rectangle Rect)> EnumerateSlotRects()
    {
        int x = (int)_panelRect.X + PanelPadding;
        int backpackY = (int)_panelRect.Y + PanelPadding + 30 + 20;

        for (int row = 0; row < BackpackRows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                yield return (Inventory.BackpackStartIndex + row * Columns + col, GetSlotRect(x, backpackY, col));
            }

            backpackY += ItemSlotUi.SlotSize + ItemSlotUi.SlotPadding;
        }

        int hotbarY = backpackY + SectionGap + 20;
        for (int col = 0; col < Inventory.HotbarSlotCount; col++)
        {
            yield return (col, GetSlotRect(x, hotbarY, col));
        }
    }

    private static Vector2 GetRectCenter(Rectangle rect) =>
        new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

    private void HandleSlotInput(Inventory inventory)
    {
        Vector2 mouse = Raylib.GetMousePosition();

        if (_isDragging)
        {
            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (_pressSlotIndex != null)
                {
                    int? target = ResolveDropTarget(mouse, _pressSlotIndex.Value);
                    if (target != null)
                    {
                        inventory.SwapSlots(_pressSlotIndex.Value, target.Value);
                    }
                }

                ResetInteraction();
            }

            return;
        }

        if (_pressSlotIndex != null && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            if (Vector2.Distance(mouse, _pressMouse) >= DragThreshold &&
                !inventory.GetSlot(_pressSlotIndex.Value).IsEmpty)
            {
                _isDragging = true;
                _selectedSlotIndex = null;
            }

            return;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            int? clicked = GetSlotIndexAt(mouse);
            if (clicked == null)
            {
                _selectedSlotIndex = null;
                _pressSlotIndex = null;
                return;
            }

            _pressSlotIndex = clicked;
            _pressMouse = mouse;
            return;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT) && _pressSlotIndex != null)
        {
            int? clicked = GetSlotIndexAt(mouse);
            _pressSlotIndex = null;

            if (clicked == null)
            {
                return;
            }

            if (_selectedSlotIndex == null)
            {
                _selectedSlotIndex = clicked;
            }
            else if (_selectedSlotIndex == clicked)
            {
                _selectedSlotIndex = null;
            }
            else
            {
                inventory.SwapSlots(_selectedSlotIndex.Value, clicked.Value);
                _selectedSlotIndex = null;
            }
        }
    }

    private int? GetSlotIndexAt(Vector2 mouse)
    {
        int? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach ((int index, Rectangle rect) in EnumerateSlotRects())
        {
            if (!Raylib.CheckCollisionPointRec(mouse, rect))
            {
                continue;
            }

            float distance = Vector2.Distance(mouse, GetRectCenter(rect));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = index;
            }
        }

        return nearest;
    }
}
