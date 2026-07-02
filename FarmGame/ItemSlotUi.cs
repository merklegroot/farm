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
        string? keyLabel = null)
    {
        Raylib.DrawRectangleRec(slotRect, new Color(32, 35, 42, 255));
        if (selected)
        {
            Raylib.DrawRectangleLinesEx(slotRect, 3f, new Color(240, 200, 80, 255));
        }
        else
        {
            Raylib.DrawRectangleLinesEx(slotRect, 1f, new Color(70, 75, 90, 255));
        }

        if (!slot.IsEmpty)
        {
            DrawToolIcon(slot.Tool!.Value, slotRect, playerTexture, actionsTexture, decorTexture);
            DrawCount(slotRect, slot);
        }

        if (keyLabel != null)
        {
            UiText.DrawText(keyLabel, (int)slotRect.X + 4, (int)slotRect.Y + 2, 14, new Color(180, 185, 195, 255));
        }
    }

    public static void DrawToolIcon(
        PlayerTool tool,
        Rectangle slot,
        Texture2D playerTexture,
        Texture2D actionsTexture,
        Texture2D decorTexture)
    {
        const int iconFrame = 32;
        const int actionFrame = 48;
        const int decorFrame = 16;

        if (tool == PlayerTool.Hands)
        {
            float iconScale = (SlotSize - 8f) / iconFrame;
            DrawIcon(playerTexture, new Rectangle(0, 0, iconFrame, iconFrame), slot, iconScale, iconFrame);
            return;
        }

        if (PlayerToolInfo.IsSeed(tool))
        {
            CropType type = CropTypeInfo.FromTool(tool)!.Value;
            float iconScale = (SlotSize - 8f) / decorFrame;
            DrawIcon(decorTexture, CropSprites.SeedBag(type), slot, iconScale, decorFrame);
            return;
        }

        int row = PlayerToolInfo.IconRow(tool);
        float actionScale = (SlotSize - 8f) / actionFrame;
        DrawIcon(actionsTexture, new Rectangle(0, row * actionFrame, actionFrame, actionFrame), slot, actionScale, actionFrame);
    }

    private static void DrawCount(Rectangle slot, InventorySlot inventorySlot)
    {
        if (PlayerToolInfo.IsInfinite(inventorySlot.Tool!.Value) || inventorySlot.Count <= 1)
        {
            return;
        }

        string label = inventorySlot.Count.ToString();
        Vector2 size = UiText.MeasureTextSize(label, 14);
        int x = (int)(slot.X + slot.Width - size.X - 4);
        int y = (int)(slot.Y + slot.Height - size.Y - 2);
        UiText.DrawText(label, x, y, 14, Color.WHITE);
    }

    private static void DrawIcon(Texture2D texture, Rectangle src, Rectangle slot, float scale, int frameSize)
    {
        float destW = frameSize * scale;
        float destH = frameSize * scale;
        float destX = slot.X + (slot.Width - destW) * 0.5f;
        float destY = slot.Y + (slot.Height - destH) * 0.5f;
        var dest = new Rectangle(destX, destY, destW, destH);
        Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0f, Color.WHITE);
    }
}
