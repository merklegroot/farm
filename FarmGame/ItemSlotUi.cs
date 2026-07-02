using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public static class ItemSlotUi
{
    public const int SlotSize = 48;
    public const int SlotPadding = 4;

    public static void DrawSlot(
        Rectangle slotRect,
        InventorySlot slot,
        bool selected,
        Texture2D playerTexture,
        Texture2D actionsTexture,
        Texture2D decorTexture,
        string? keyLabel = null,
        byte alpha = 255)
    {
        int a = alpha;
        var fill = new Color(32, 35, 42, a);
        var borderSelected = new Color(240, 200, 80, a);
        var borderNormal = new Color(70, 75, 90, a);

        Raylib.DrawRectangleRec(slotRect, fill);
        if (selected)
        {
            Raylib.DrawRectangleLinesEx(slotRect, 3f, borderSelected);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(slotRect, 1f, borderNormal);
        }

        if (!slot.IsEmpty)
        {
            DrawToolIcon(slot.Tool!.Value, slotRect, playerTexture, actionsTexture, decorTexture, alpha);
            DrawCount(slotRect, slot, alpha);
        }

        if (keyLabel != null)
        {
            UiText.DrawText(keyLabel, (int)slotRect.X + 4, (int)slotRect.Y + 2, 14, new Color(180, 185, 195, a));
        }
    }

    public static void DrawFloatingSlot(
        Vector2 center,
        InventorySlot slot,
        Texture2D playerTexture,
        Texture2D actionsTexture,
        Texture2D decorTexture)
    {
        var slotRect = new Rectangle(
            center.X - SlotSize * 0.5f,
            center.Y - SlotSize * 0.5f,
            SlotSize,
            SlotSize);
        DrawSlot(slotRect, slot, selected: false, playerTexture, actionsTexture, decorTexture, alpha: 220);
    }

    public static void DrawToolIcon(
        PlayerTool tool,
        Rectangle slot,
        Texture2D playerTexture,
        Texture2D actionsTexture,
        Texture2D decorTexture,
        byte alpha = 255)
    {
        const int iconFrame = 32;
        const int actionFrame = 48;
        const int decorFrame = 16;

        if (tool == PlayerTool.Hands)
        {
            float iconScale = (SlotSize - 8f) / iconFrame;
            DrawIcon(playerTexture, new Rectangle(0, 0, iconFrame, iconFrame), slot, iconScale, iconFrame, alpha);
            return;
        }

        if (PlayerToolInfo.IsSeed(tool))
        {
            CropType type = CropTypeInfo.FromTool(tool)!.Value;
            float iconScale = (SlotSize - 8f) / decorFrame;
            DrawIcon(decorTexture, CropSprites.SeedBag(type), slot, iconScale, decorFrame, alpha);
            return;
        }

        int row = PlayerToolInfo.IconRow(tool);
        float actionScale = (SlotSize - 8f) / actionFrame;
        DrawIcon(actionsTexture, new Rectangle(0, row * actionFrame, actionFrame, actionFrame), slot, actionScale, actionFrame, alpha);
    }

    private static void DrawCount(Rectangle slot, InventorySlot inventorySlot, byte alpha = 255)
    {
        if (PlayerToolInfo.IsInfinite(inventorySlot.Tool!.Value) || inventorySlot.Count <= 1)
        {
            return;
        }

        string label = inventorySlot.Count.ToString();
        Vector2 size = UiText.MeasureTextSize(label, 14);
        int x = (int)(slot.X + slot.Width - size.X - 4);
        int y = (int)(slot.Y + slot.Height - size.Y - 2);
        UiText.DrawText(label, x, y, 14, new Color(255, 255, 255, (int)alpha));
    }

    private static void DrawIcon(Texture2D texture, Rectangle src, Rectangle slot, float scale, int frameSize, byte alpha = 255)
    {
        float destW = frameSize * scale;
        float destH = frameSize * scale;
        float destX = slot.X + (slot.Width - destW) * 0.5f;
        float destY = slot.Y + (slot.Height - destH) * 0.5f;
        var dest = new Rectangle(destX, destY, destW, destH);
        Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0f, new Color(255, 255, 255, (int)alpha));
    }
}
