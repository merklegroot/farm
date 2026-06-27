using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class AssetEditorUi
{
    private const int MinCanvasSize = 1;
    private const int MaxCanvasSize = 32;
    private const int CellSize = 10;
    private const int PanelWidth = 320;
    private const int PanelPadding = 16;
    private const int AssetRowHeight = 22;
    private const int AssetListVisibleRows = 6;

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

    private readonly SimpleTextField _nameField = new("new_asset");
    private Color[] _pixels = new Color[16 * 16];
    private int _width = 16;
    private int _height = 16;
    private bool _isOpen;
    private bool _isPlacing;
    private int _selectedColorIndex;
    private bool _useEraser;
    private bool _nameFocused;
    private bool _wasNameFocused;
    private bool _pixelsDirty;
    private string? _savedAssetName;
    private string? _selectedAssetName;
    private string _statusMessage = "";
    private float _statusTimer;
    private int _assetListScroll;
    private Rectangle _panelRect;
    private Rectangle _canvasRect;
    private Rectangle _nameFieldRect;
    private Rectangle _assetListRect;
    private IReadOnlyList<string> _savedNames = [];

    public bool IsOpen => _isOpen;
    public bool IsPlacing => _isPlacing;

    public void Toggle(AssetLibrary assets)
    {
        if (_isOpen)
        {
            PersistCurrent(assets);
            _isOpen = false;
            _isPlacing = false;
            return;
        }

        _isOpen = true;
        _isPlacing = false;
        RefreshAssetList();

        if (_selectedAssetName != null && _savedNames.Contains(_selectedAssetName, StringComparer.OrdinalIgnoreCase))
        {
            SelectAsset(_selectedAssetName, assets, persistPending: false);
            return;
        }

        if (_savedNames.Count > 0)
        {
            SelectAsset(_savedNames[0], assets, persistPending: false);
        }
        else
        {
            BeginNewAsset(assets);
        }
    }

    public void Update(int screenWidth, int screenHeight, AssetLibrary assets)
    {
        if (!_isOpen)
        {
            return;
        }

        LayoutPanel(screenWidth, screenHeight);
        _statusTimer = Math.Max(0f, _statusTimer - Raylib.GetFrameTime());

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _isPlacing = false;
            _nameFocused = false;
        }

        HandleFocus();
        _nameField.Update(_nameFocused);

        if (_wasNameFocused && !_nameFocused)
        {
            CommitNameChange(assets);
        }

        _wasNameFocused = _nameFocused;

        HandlePropertyButtons(assets);
        HandleButtons(assets);
        HandleAssetList(assets);
        HandleAssetListScroll();

        if (!_isPlacing)
        {
            HandlePaletteInput();
            HandleCanvasPaint(assets);
        }

        if (_pixelsDirty && Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
        {
            PersistCurrent(assets);
            _pixelsDirty = false;
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

        if (!PersistCurrent(assets, out string saveMessage))
        {
            SetStatus(saveMessage);
            return false;
        }

        string assetName = _nameField.Text;
        Vector2 worldPos = screenPos - mapOffset;
        assets.Place(assetName, worldPos);
        assets.PersistPlacements();
        _isPlacing = false;
        SetStatus($"Placed '{assetName}'");
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

        UiText.DrawText("Name", x, y, 14, new Color(150, 155, 170, 255));
        y += 18;
        _nameField.Draw(_nameFieldRect, _nameFocused);
        y += 34;

        DrawDimensionRow(x, ref y);
        y += 8;

        string mode = _isPlacing ? "Click the map to place" : "Changes save automatically";
        UiText.DrawText(mode, x, y, 15, new Color(180, 185, 195, 255));
        y += 22;

        DrawCanvas(y);
        y += _height * CellSize + 12;

        DrawPalette(x, y);
        y += 36;

        DrawActionButtons(x, y);
        y += 36;

        DrawAssetList(x, y);

        if (_statusTimer > 0f)
        {
            UiText.DrawText(_statusMessage, x, (int)(_panelRect.Y + _panelRect.Height - 22), 14, new Color(170, 220, 150, 255));
        }
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

        float destW = _width * mapScale;
        float destH = _height * mapScale;
        float left = mouse.X - destW * 0.5f;
        float top = mouse.Y - destH * 0.5f;

        for (int py = 0; py < _height; py++)
        {
            for (int px = 0; px < _width; px++)
            {
                Color color = _pixels[py * _width + px];
                if (color.A == 0)
                {
                    continue;
                }

                var cell = new Rectangle(left + px * mapScale, top + py * mapScale, mapScale, mapScale);
                Raylib.DrawRectangleRec(cell, new Color(color.R, color.G, color.B, (byte)160));
            }
        }
    }

    public bool IsMouseOverPanel(Vector2 screenPos) =>
        _isOpen && Raylib.CheckCollisionPointRec(screenPos, _panelRect);

    private void LayoutPanel(int screenWidth, int screenHeight)
    {
        int canvasBlock = _height * CellSize;
        int assetListBlock = 18 + 28 + AssetListVisibleRows * AssetRowHeight + 4;
        int panelHeight = PanelPadding * 2 + 30 + 18 + 34 + 24 + 8 + 22 + canvasBlock + 12 + 36 + 36 + assetListBlock + 24;
        _panelRect = new Rectangle(screenWidth - PanelWidth - 12, 12, PanelWidth, panelHeight);
        _nameFieldRect = new Rectangle(_panelRect.X + PanelPadding, _panelRect.Y + PanelPadding + 30 + 18, PanelWidth - PanelPadding * 2, 28);
        _canvasRect = new Rectangle(
            _panelRect.X + PanelPadding,
            _nameFieldRect.Y + 34 + 24 + 8 + 22,
            _width * CellSize,
            canvasBlock);
        _assetListRect = new Rectangle(
            _panelRect.X + PanelPadding,
            _canvasRect.Y + _canvasRect.Height + 12 + 36 + 36 + 18 + 28,
            PanelWidth - PanelPadding * 2,
            AssetListVisibleRows * AssetRowHeight);
    }

    private void DrawDimensionRow(int x, ref int y)
    {
        UiText.DrawText("Size", x, y, 14, new Color(150, 155, 170, 255));
        DrawSmallButton(new Rectangle(x + 42, y - 2, 22, 22), "-W");
        DrawSmallButton(new Rectangle(x + 68, y - 2, 22, 22), "+W");
        DrawSmallButton(new Rectangle(x + 94, y - 2, 22, 22), "-H");
        DrawSmallButton(new Rectangle(x + 120, y - 2, 22, 22), "+H");
        UiText.DrawText($"{_width} x {_height}", x + 152, y, 16, Color.WHITE);
        y += 22;
    }

    private void DrawCanvas(int y)
    {
        _canvasRect = new Rectangle(_panelRect.X + PanelPadding, y, _width * CellSize, _height * CellSize);

        Raylib.DrawRectangleRec(_canvasRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_canvasRect, 1f, new Color(70, 75, 90, 255));

        for (int py = 0; py < _height; py++)
        {
            for (int px = 0; px < _width; px++)
            {
                Color color = _pixels[py * _width + px];
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

        for (int i = 1; i < _width; i++)
        {
            float gx = _canvasRect.X + i * CellSize;
            Raylib.DrawLineV(new Vector2(gx, _canvasRect.Y), new Vector2(gx, _canvasRect.Y + _canvasRect.Height), new Color(40, 42, 50, 180));
        }

        for (int i = 1; i < _height; i++)
        {
            float gy = _canvasRect.Y + i * CellSize;
            Raylib.DrawLineV(new Vector2(_canvasRect.X, gy), new Vector2(_canvasRect.X + _canvasRect.Width, gy), new Color(40, 42, 50, 180));
        }
    }

    private void DrawPalette(int x, int y)
    {
        const int swatch = 24;
        const int gap = 4;

        for (int i = 0; i < Palette.Length; i++)
        {
            var rect = new Rectangle(x + i * (swatch + gap), y, swatch, swatch);
            Raylib.DrawRectangleRec(rect, Palette[i]);
            Raylib.DrawRectangleLinesEx(rect, (!_useEraser && i == _selectedColorIndex) ? 2f : 1f,
                (!_useEraser && i == _selectedColorIndex) ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));
        }

        var eraserRect = new Rectangle(x + Palette.Length * (swatch + gap), y, swatch, swatch);
        Raylib.DrawRectangleRec(eraserRect, new Color(32, 35, 42, 255));
        UiText.DrawText("X", (int)eraserRect.X + 7, (int)eraserRect.Y + 3, 16, Color.LIGHTGRAY);
        Raylib.DrawRectangleLinesEx(eraserRect, _useEraser ? 2f : 1f, _useEraser ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));
    }

    private void DrawActionButtons(int x, int y)
    {
        DrawButton(new Rectangle(x, y, 68, 28), "Clear", false);
        DrawButton(new Rectangle(x + 74, y, 68, 28), "Place", _isPlacing);
        DrawButton(new Rectangle(x + 148, y, 68, 28), "Close", false);
    }

    private void DrawAssetList(int x, int y)
    {
        UiText.DrawText("Assets", x, y, 14, new Color(150, 155, 170, 255));
        DrawButton(new Rectangle(x + PanelWidth - PanelPadding * 2 - 52, y - 4, 52, 22), "New", false);

        y += 28;
        _assetListRect = new Rectangle(x, y, PanelWidth - PanelPadding * 2, AssetListVisibleRows * AssetRowHeight);
        Raylib.DrawRectangleRec(_assetListRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_assetListRect, 1f, new Color(70, 75, 90, 255));

        if (_savedNames.Count == 0)
        {
            UiText.DrawText("(none yet — paint to create)", x + 8, y + 6, 14, new Color(110, 115, 130, 255));
            return;
        }

        int maxScroll = Math.Max(0, _savedNames.Count - AssetListVisibleRows);
        _assetListScroll = Math.Clamp(_assetListScroll, 0, maxScroll);

        for (int row = 0; row < AssetListVisibleRows; row++)
        {
            int index = _assetListScroll + row;
            if (index >= _savedNames.Count)
            {
                break;
            }

            string name = _savedNames[index];
            bool selected = string.Equals(name, _selectedAssetName, StringComparison.OrdinalIgnoreCase);
            var rowRect = new Rectangle(_assetListRect.X, _assetListRect.Y + row * AssetRowHeight, _assetListRect.Width, AssetRowHeight);
            Raylib.DrawRectangleRec(rowRect, selected ? new Color(58, 62, 78, 255) : new Color(32, 35, 42, 255));
            if (selected)
            {
                Raylib.DrawRectangleLinesEx(rowRect, 1f, new Color(240, 200, 80, 255));
            }

            UiText.DrawText(name, (int)rowRect.X + 8, (int)rowRect.Y + 3, 14, selected ? Color.WHITE : Color.LIGHTGRAY);
        }
    }

    private static void DrawButton(Rectangle rect, string label, bool active)
    {
        Raylib.DrawRectangleRec(rect, active ? new Color(70, 62, 38, 255) : new Color(38, 42, 52, 255));
        Raylib.DrawRectangleLinesEx(rect, 1f, new Color(90, 95, 110, 255));
        Vector2 size = UiText.MeasureTextSize(label, 15);
        UiText.DrawText(label, (int)(rect.X + (rect.Width - size.X) * 0.5f), (int)(rect.Y + 5), 15, Color.WHITE);
    }

    private static void DrawSmallButton(Rectangle rect, string label)
    {
        Raylib.DrawRectangleRec(rect, new Color(38, 42, 52, 255));
        Raylib.DrawRectangleLinesEx(rect, 1f, new Color(90, 95, 110, 255));
        UiText.DrawText(label, (int)rect.X + 3, (int)rect.Y + 3, 12, Color.WHITE);
    }

    private void HandleFocus()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        _nameFocused = Raylib.CheckCollisionPointRec(mouse, _nameFieldRect);
    }

    private void HandlePropertyButtons(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)_nameFieldRect.Y + 34;

        if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 42, y - 2, 22, 22)))
        {
            ResizeCanvas(_width - 1, _height);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 68, y - 2, 22, 22)))
        {
            ResizeCanvas(_width + 1, _height);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 94, y - 2, 22, 22)))
        {
            ResizeCanvas(_width, _height - 1);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 120, y - 2, 22, 22)))
        {
            ResizeCanvas(_width, _height + 1);
            PersistCurrent(assets);
        }
    }

    private void HandleButtons(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)(_canvasRect.Y + _canvasRect.Height + 12 + 36);
        int assetHeaderY = (int)(_canvasRect.Y + _canvasRect.Height + 12 + 36 + 36 + 18);

        if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x, y, 68, 28)))
        {
            ClearCanvas();
            PersistCurrent(assets);
            SetStatus("Canvas cleared");
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 74, y, 68, 28)))
        {
            if (PersistCurrent(assets, out string message))
            {
                _isPlacing = true;
            }
            else
            {
                SetStatus(message);
            }
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 148, y, 68, 28)))
        {
            PersistCurrent(assets);
            _isOpen = false;
            _isPlacing = false;
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + PanelWidth - PanelPadding * 2 - 52, assetHeaderY - 4, 52, 22)))
        {
            BeginNewAsset(assets);
        }
    }

    private void HandleAssetList(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _assetListRect))
        {
            return;
        }

        int row = (int)((mouse.Y - _assetListRect.Y) / AssetRowHeight);
        int index = _assetListScroll + row;
        if (index < 0 || index >= _savedNames.Count)
        {
            return;
        }

        SelectAsset(_savedNames[index], assets, persistPending: true);
    }

    private void HandleAssetListScroll()
    {
        if (_savedNames.Count <= AssetListVisibleRows)
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _assetListRect))
        {
            return;
        }

        float wheel = Raylib.GetMouseWheelMove();
        if (wheel == 0f)
        {
            return;
        }

        _assetListScroll -= (int)wheel;
        _assetListScroll = Math.Clamp(_assetListScroll, 0, _savedNames.Count - AssetListVisibleRows);
    }

    private void HandlePaletteInput()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        int x = (int)_panelRect.X + PanelPadding;
        int y = (int)(_canvasRect.Y + _canvasRect.Height + 12);
        const int swatch = 24;
        const int gap = 4;

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

    private void HandleCanvasPaint(AssetLibrary assets)
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
        if (px < 0 || py < 0 || px >= _width || py >= _height)
        {
            return;
        }

        Color next = _useEraser ? Color.BLANK : Palette[_selectedColorIndex];
        int index = py * _width + px;
        if (_pixels[index].Equals(next))
        {
            return;
        }

        _pixels[index] = next;
        _pixelsDirty = true;
    }

    private void CommitNameChange(AssetLibrary assets)
    {
        string newName = DefinedAssetStore.SanitizeFileName(_nameField.Text);
        if (newName.Length == 0)
        {
            return;
        }

        _nameField.SetText(newName);

        if (string.Equals(newName, _savedAssetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_savedAssetName != null && !string.Equals(newName, _savedAssetName, StringComparison.OrdinalIgnoreCase))
        {
            DefinedAssetStore.DeleteAsset(_savedAssetName);
        }

        PersistCurrent(assets);
        RefreshAssetList();
        _selectedAssetName = newName;
        EnsureSelectedAssetVisible();
    }

    private void BeginNewAsset(AssetLibrary? assets)
    {
        if (assets != null)
        {
            PersistCurrent(assets);
        }

        string name = DefinedAssetStore.SuggestNewAssetName();
        _nameField.SetText(name);
        _savedAssetName = null;
        _selectedAssetName = null;
        _width = 16;
        _height = 16;
        _pixels = new Color[16 * 16];
        ClearCanvas();
        _pixelsDirty = false;
        _isPlacing = false;
    }

    private void SelectAsset(string name, AssetLibrary? assets, bool persistPending)
    {
        if (persistPending && assets != null)
        {
            PersistCurrent(assets);
        }

        SavedAssetFile file = DefinedAssetStore.LoadAsset(name);
        PixelAssetDefinition definition = file.ToDefinition();
        _nameField.SetText(file.Name);
        _savedAssetName = file.Name;
        _selectedAssetName = file.Name;
        ResizeCanvas(definition.Width, definition.Height);
        definition.Pixels.CopyTo(_pixels, 0);
        assets?.DefineOrReplace(file.Name, definition);
        _pixelsDirty = false;
        _isPlacing = false;
        EnsureSelectedAssetVisible();
    }

    private void EnsureSelectedAssetVisible()
    {
        if (_selectedAssetName == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < _savedNames.Count; i++)
        {
            if (string.Equals(_savedNames[i], _selectedAssetName, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        if (index < _assetListScroll)
        {
            _assetListScroll = index;
        }
        else if (index >= _assetListScroll + AssetListVisibleRows)
        {
            _assetListScroll = index - AssetListVisibleRows + 1;
        }
    }

    private bool PersistCurrent(AssetLibrary assets, out string message) =>
        TryPersistCurrent(assets, out message);

    private void PersistCurrent(AssetLibrary assets) =>
        TryPersistCurrent(assets, out _);

    private bool TryPersistCurrent(AssetLibrary assets, out string message)
    {
        string name = DefinedAssetStore.SanitizeFileName(_nameField.Text);
        if (name.Length == 0)
        {
            message = "Enter a valid name";
            return false;
        }

        try
        {
            if (_savedAssetName != null && !string.Equals(name, _savedAssetName, StringComparison.OrdinalIgnoreCase))
            {
                DefinedAssetStore.DeleteAsset(_savedAssetName);
            }

            PixelAssetDefinition definition = BuildDefinition();
            SavedAssetFile file = SavedAssetFile.FromDefinition(name, definition);
            DefinedAssetStore.SaveAsset(file);
            assets.DefineOrReplace(name, definition);
            _nameField.SetText(name);
            _savedAssetName = name;
            _selectedAssetName = name;
            RefreshAssetList();
            EnsureSelectedAssetVisible();
            message = "";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    private void RefreshAssetList() =>
        _savedNames = DefinedAssetStore.ListAssetNames();

    private PixelAssetDefinition BuildDefinition() =>
        PixelAssetDefinition.FromPixels(_width, _height, _pixels);

    private void ResizeCanvas(int newWidth, int newHeight)
    {
        newWidth = Math.Clamp(newWidth, MinCanvasSize, MaxCanvasSize);
        newHeight = Math.Clamp(newHeight, MinCanvasSize, MaxCanvasSize);

        var resized = new Color[newWidth * newHeight];
        for (int y = 0; y < Math.Min(newHeight, _height); y++)
        {
            for (int x = 0; x < Math.Min(newWidth, _width); x++)
            {
                resized[y * newWidth + x] = _pixels[y * _width + x];
            }
        }

        _width = newWidth;
        _height = newHeight;
        _pixels = resized;
    }

    private void ClearCanvas()
    {
        Array.Fill(_pixels, Color.BLANK);
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
        _statusTimer = 3f;
    }
}
