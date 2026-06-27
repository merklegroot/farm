using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class AssetEditorUi
{
    public const int CanvasSize = 16;
    private const int CellSize = 14;
    private const int PanelWidth = 300;
    private const int PanelPadding = 16;

    private static readonly Color[] Palette =
    [
        new Color(58, 42, 30, 255),
        new Color(139, 90, 43, 255),
        new Color(228, 198, 130, 255),
        new Color(86, 152, 56, 255),
        new Color(58, 110, 38, 255),
        new Color(96, 152, 208, 255),
        new Color(208, 72, 72, 255),
        new Color(240, 240, 245, 255),
    ];

    private readonly Color[] _pixels = new Color[CanvasSize * CanvasSize];
    private bool _isOpen;
    private bool _isPlacing;
    private int _selectedColorIndex;
    private bool _useEraser;
    private Rectangle _panelRect;
    private Rectangle _canvasRect;

    public bool IsOpen => _isOpen;
    public bool IsPlacing => _isPlacing;

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _isPlacing = false;
    }

    public void Update(int screenWidth, int screenHeight)
    {
        if (!_isOpen)
        {
            return;
        }

        LayoutPanel(screenWidth, screenHeight);

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _isPlacing = false;
        }

        HandleButtons();
        if (!_isPlacing)
        {
            HandlePaletteInput();
            HandleCanvasPaint();
        }
    }

    public bool TryPlaceAtScreen(Vector2 screenPos, AssetLibrary assets, float mapScale, Vector2 mapOffset)
    {
        if (!_isOpen || !_isPlacing || IsMouseOverPanel(screenPos))
        {
            return false;
        }

        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return false;
        }

        Vector2 worldPos = screenPos - mapOffset;
        assets.PlaceCustom(BuildDefinition(), worldPos);
        _isPlacing = false;
        return true;
    }

    public void Draw(int screenWidth, int screenHeight)
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
        UiText.DrawText("Asset Editor", x, y, 22, Color.WHITE);
        y += 30;

        string mode = _isPlacing ? "Click the map to place" : "Paint pixels, then Place";
        UiText.DrawText(mode, x, y, 16, new Color(180, 185, 195, 255));
        y += 26;

        DrawCanvas(y);
        y += CanvasSize * CellSize + 16;

        DrawPalette(x, y);
        y += 40;

        DrawButtons(x, y);
    }

    public void DrawPlacementPreview(float mapScale, Vector2 mapOffset)
    {
        if (!_isOpen || !_isPlacing)
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (IsMouseOverPanel(mouse))
        {
            return;
        }

        float destW = CanvasSize * mapScale;
        float destH = CanvasSize * mapScale;
        float left = mouse.X - destW * 0.5f;
        float top = mouse.Y - destH * 0.5f;

        for (int py = 0; py < CanvasSize; py++)
        {
            for (int px = 0; px < CanvasSize; px++)
            {
                Color color = _pixels[py * CanvasSize + px];
                if (color.A == 0)
                {
                    continue;
                }

                var cell = new Rectangle(left + px * mapScale, top + py * mapScale, mapScale, mapScale);
                Raylib.DrawRectangleRec(cell, new Color(color.R, color.G, color.B, (byte)160));
            }
        }
    }

    public PixelAssetDefinition BuildDefinition() =>
        PixelAssetDefinition.FromPixels(CanvasSize, CanvasSize, _pixels);

    public bool IsMouseOverPanel(Vector2 screenPos) =>
        _isOpen && Raylib.CheckCollisionPointRec(screenPos, _panelRect);

    private void LayoutPanel(int screenWidth, int screenHeight)
    {
        int panelHeight = PanelPadding * 2 + 30 + 26 + CanvasSize * CellSize + 16 + 40 + 28;
        _panelRect = new Rectangle(screenWidth - PanelWidth - 12, 12, PanelWidth, panelHeight);
        _canvasRect = new Rectangle(
            _panelRect.X + PanelPadding,
            _panelRect.Y + PanelPadding + 30 + 26,
            CanvasSize * CellSize,
            CanvasSize * CellSize);
    }

    private void DrawCanvas(int y)
    {
        Raylib.DrawRectangleRec(_canvasRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_canvasRect, 1f, new Color(70, 75, 90, 255));

        for (int py = 0; py < CanvasSize; py++)
        {
            for (int px = 0; px < CanvasSize; px++)
            {
                Color color = _pixels[py * CanvasSize + px];
                if (color.A == 0)
                {
                    continue;
                }

                var cell = new Rectangle(
                    _canvasRect.X + px * CellSize,
                    _canvasRect.Y + py * CellSize,
                    CellSize,
                    CellSize);
                Raylib.DrawRectangleRec(cell, color);
            }
        }

        for (int i = 1; i < CanvasSize; i++)
        {
            float gx = _canvasRect.X + i * CellSize;
            float gy = _canvasRect.Y + i * CellSize;
            Raylib.DrawLineV(new Vector2(gx, _canvasRect.Y), new Vector2(gx, _canvasRect.Y + _canvasRect.Height), new Color(40, 42, 50, 180));
            Raylib.DrawLineV(new Vector2(_canvasRect.X, gy), new Vector2(_canvasRect.X + _canvasRect.Width, gy), new Color(40, 42, 50, 180));
        }
    }

    private void DrawPalette(int x, int y)
    {
        const int swatch = 28;
        const int gap = 6;

        for (int i = 0; i < Palette.Length; i++)
        {
            var rect = new Rectangle(x + i * (swatch + gap), y, swatch, swatch);
            Raylib.DrawRectangleRec(rect, Palette[i]);
            if (!_useEraser && i == _selectedColorIndex)
            {
                Raylib.DrawRectangleLinesEx(rect, 2f, new Color(240, 200, 80, 255));
            }
            else
            {
                Raylib.DrawRectangleLinesEx(rect, 1f, new Color(70, 75, 90, 255));
            }
        }

        var eraserRect = new Rectangle(x + Palette.Length * (swatch + gap), y, swatch, swatch);
        Raylib.DrawRectangleRec(eraserRect, new Color(32, 35, 42, 255));
        UiText.DrawText("X", (int)eraserRect.X + 8, (int)eraserRect.Y + 4, 18, Color.LIGHTGRAY);
        if (_useEraser)
        {
            Raylib.DrawRectangleLinesEx(eraserRect, 2f, new Color(240, 200, 80, 255));
        }
        else
        {
            Raylib.DrawRectangleLinesEx(eraserRect, 1f, new Color(70, 75, 90, 255));
        }
    }

    private void DrawButtons(int x, int y)
    {
        DrawButton(new Rectangle(x, y, 80, 28), "Clear", false);
        DrawButton(new Rectangle(x + 90, y, 80, 28), "Place", _isPlacing);
        DrawButton(new Rectangle(x + 180, y, 80, 28), "Close", false);
    }

    private static void DrawButton(Rectangle rect, string label, bool active)
    {
        Raylib.DrawRectangleRec(rect, active ? new Color(70, 62, 38, 255) : new Color(38, 42, 52, 255));
        Raylib.DrawRectangleLinesEx(rect, 1f, new Color(90, 95, 110, 255));
        Vector2 size = UiText.MeasureTextSize(label, 16);
        UiText.DrawText(label, (int)(rect.X + (rect.Width - size.X) * 0.5f), (int)(rect.Y + 5), 16, Color.WHITE);
    }

    private void HandleButtons()
    {
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)(_panelRect.Y + _panelRect.Height - PanelPadding - 28);

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x, y, 80, 28)))
            {
                ClearCanvas();
            }
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 90, y, 80, 28)))
            {
                _isPlacing = true;
            }
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 180, y, 80, 28)))
            {
                _isOpen = false;
                _isPlacing = false;
            }
        }
    }

    private void HandlePaletteInput()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)(_canvasRect.Y + _canvasRect.Height + 16);
        const int swatch = 28;
        const int gap = 6;

        for (int i = 0; i < Palette.Length; i++)
        {
            var rect = new Rectangle(x + i * (swatch + gap), y, swatch, swatch);
            if (Raylib.CheckCollisionPointRec(mouse, rect))
            {
                _selectedColorIndex = i;
                _useEraser = false;
                return;
            }
        }

        var eraserRect = new Rectangle(x + Palette.Length * (swatch + gap), y, swatch, swatch);
        if (Raylib.CheckCollisionPointRec(mouse, eraserRect))
        {
            _useEraser = true;
        }
    }

    private void HandleCanvasPaint()
    {
        if (!Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _canvasRect))
        {
            return;
        }

        int px = (int)((mouse.X - _canvasRect.X) / CellSize);
        int py = (int)((mouse.Y - _canvasRect.Y) / CellSize);
        if (px < 0 || py < 0 || px >= CanvasSize || py >= CanvasSize)
        {
            return;
        }

        _pixels[py * CanvasSize + px] = _useEraser ? Color.BLANK : Palette[_selectedColorIndex];
    }

    private void ClearCanvas()
    {
        Array.Fill(_pixels, Color.BLANK);
    }
}
