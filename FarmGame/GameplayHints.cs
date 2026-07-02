using Raylib_cs;

namespace FarmGame;

public static class GameplayHints
{
    private static readonly Color HintText = new(190, 195, 205, 255);
    private static readonly Color HintMuted = new(130, 135, 150, 255);
    private static readonly Color PanelFill = new(20, 22, 28, 190);
    private static readonly Color PanelBorder = new(60, 65, 80, 255);

    public static void Draw(int screenWidth, int screenHeight, bool inventoryOpen, bool assetEditorOpen)
    {
        if (assetEditorOpen)
        {
            return;
        }

        const int padding = 10;
        const int lineHeight = 18;
        const int fontSize = 15;
        int x = 12;
        int y = 10;

        string[] lines =
        [
            "WASD / Arrows  Move",
            "Space / Click   Use tool or plant seeds",
            "1-9             Select hotbar slot",
            "Q / E           Previous / next hotbar slot",
            "I               Inventory",
            "Tab             Asset editor",
        ];

        int panelWidth = 320;
        int panelHeight = padding * 2 + lines.Length * lineHeight + 8;
        Raylib.DrawRectangle(x - 4, y - 6, panelWidth, panelHeight, PanelFill);
        Raylib.DrawRectangleLines(x - 4, y - 6, panelWidth, panelHeight, PanelBorder);

        UiText.DrawText("Controls", x, y, fontSize + 1, HintText);
        y += lineHeight + 4;

        foreach (string line in lines)
        {
            UiText.DrawText(line, x, y, fontSize, HintMuted);
            y += lineHeight;
        }

        if (inventoryOpen)
        {
            string inventoryHint = "Inventory open — click two slots to swap, drag to move · I or Esc to close";
            int hintY = screenHeight - 36;
            Raylib.DrawRectangle(0, hintY - 8, screenWidth, 32, PanelFill);
            UiText.DrawText(inventoryHint, 12, hintY, fontSize, HintText);
        }
    }
}
