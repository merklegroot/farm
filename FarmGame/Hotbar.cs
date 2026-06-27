using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class Hotbar
{
    private const int SlotSize = 48;
    private const int SlotPadding = 6;
    private const int BarPadding = 12;
    private const int BarHeight = 72;

    private readonly Texture2D _playerTexture;
    private readonly Texture2D _actionsTexture;
    private readonly Texture2D _decorTexture;

    private int _selectedIndex;

    public PlayerTool SelectedTool => (PlayerTool)_selectedIndex;

    public Hotbar(Texture2D playerTexture, Texture2D actionsTexture, Texture2D decorTexture, int initialSelection = 0)
    {
        _playerTexture = playerTexture;
        _actionsTexture = actionsTexture;
        _decorTexture = decorTexture;
        _selectedIndex = Math.Clamp(initialSelection, 0, PlayerToolInfo.SlotCount - 1);
    }

    public void Update()
    {
        for (int i = 0; i < PlayerToolInfo.SlotCount; i++)
        {
            if (Raylib.IsKeyPressed((KeyboardKey)((int)KeyboardKey.KEY_ONE + i)))
            {
                _selectedIndex = i;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
        {
            _selectedIndex = (_selectedIndex + PlayerToolInfo.SlotCount - 1) % PlayerToolInfo.SlotCount;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_E))
        {
            _selectedIndex = (_selectedIndex + 1) % PlayerToolInfo.SlotCount;
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        int slotCount = PlayerToolInfo.SlotCount;
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
            var tool = (PlayerTool)i;
            bool selected = i == _selectedIndex;
            var slotRect = new Rectangle(slotX, slotY, SlotSize, SlotSize);

            Raylib.DrawRectangleRec(slotRect, new Color(32, 35, 42, 255));
            if (selected)
            {
                Raylib.DrawRectangleLinesEx(slotRect, 3f, new Color(240, 200, 80, 255));
            }
            else
            {
                Raylib.DrawRectangleLinesEx(slotRect, 1f, new Color(70, 75, 90, 255));
            }

            DrawToolIcon(tool, slotRect);

            string keyLabel = (i + 1).ToString();
            UiText.DrawText(keyLabel, (int)slotX + 4, (int)slotY + 2, 14, new Color(180, 185, 195, 255));

            slotX += SlotSize + SlotPadding;
        }
    }

    private void DrawToolIcon(PlayerTool tool, Rectangle slot)
    {
        const int iconFrame = 32;
        const int actionFrame = 48;
        const int decorFrame = 16;

        if (tool == PlayerTool.Hands)
        {
            float iconScale = (SlotSize - 8f) / iconFrame;
            DrawIcon(_playerTexture, new Rectangle(0, 0, iconFrame, iconFrame), slot, iconScale, iconFrame);
            return;
        }

        if (PlayerToolInfo.IsSeed(tool))
        {
            CropType type = CropTypeInfo.FromTool(tool)!.Value;
            float iconScale = (SlotSize - 8f) / decorFrame;
            DrawIcon(_decorTexture, CropSprites.SeedBag(type), slot, iconScale, decorFrame);
            return;
        }

        int row = PlayerToolInfo.IconRow(tool);
        float actionScale = (SlotSize - 8f) / actionFrame;
        DrawIcon(_actionsTexture, new Rectangle(0, row * actionFrame, actionFrame, actionFrame), slot, actionScale, actionFrame);
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
