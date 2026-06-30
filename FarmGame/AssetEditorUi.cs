using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class AssetEditorUi
{
    private enum EditorTool
    {
        Brush,
        Select,
    }

    private const int MinCanvasSize = 1;
    private const int MaxCanvasSize = 32;
    private const int CellSize = 10;
    private const int EditorColumnWidth = 320;
    private const int AssetColumnWidth = 168;
    private const int ColumnGap = 16;
    private const int PanelWidth = AssetColumnWidth + ColumnGap + EditorColumnWidth;
    private const int PanelPadding = 16;
    private const int AssetRowHeight = 22;
    private const int MaxUndoSteps = 50;

    private sealed class CanvasSnapshot
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required Color[] Pixels { get; init; }
    }

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
    private readonly ColorPickerUi _colorPicker = new();
    private readonly List<CanvasSnapshot> _undoStack = [];
    private Color[] _pixels = new Color[16 * 16];
    private int _width = 16;
    private int _height = 16;
    private bool _isOpen;
    private bool _isPlacing;
    private EditorTool _tool = EditorTool.Brush;
    private int _selectedColorIndex;
    private bool _useCustomColor;
    private bool _useEraser;
    private bool _nameFocused;
    private bool _wasNameFocused;
    private bool _pixelsDirty;
    private bool _hasSelection;
    private int _selX;
    private int _selY;
    private int _selW;
    private int _selH;
    private Color[]? _liftedPixels;
    private bool _isSelecting;
    private int _selectStartX;
    private int _selectStartY;
    private int _selectEndX;
    private int _selectEndY;
    private bool _isMovingSelection;
    private int _moveX;
    private int _moveY;
    private int _moveGrabOffsetX;
    private int _moveGrabOffsetY;
    private int _originalSelX;
    private int _originalSelY;
    private string? _savedFileKey;
    private string? _selectedFileKey;
    private string _statusMessage = "";
    private float _statusTimer;
    private int _assetListScroll;
    private int _assetListVisibleRows = 6;
    private float _leftColumnX;
    private float _editorColumnX;
    private float _editorColumnWidth;
    private Rectangle _panelRect;
    private Rectangle _canvasRect;
    private Rectangle _nameFieldRect;
    private Rectangle _assetListRect;
    private Rectangle _newAssetButtonRect;
    private Rectangle _cloneAssetButtonRect;
    private Rectangle _deleteAssetButtonRect;
    private Rectangle _brushToolRect;
    private Rectangle _selectToolRect;
    private Rectangle _undoButtonRect;
    private Rectangle _paletteRect;
    private Rectangle _customSwatchRect;
    private Rectangle _colorPickerRect;
    private Rectangle _clearButtonRect;
    private Rectangle _placeButtonRect;
    private Rectangle _closeButtonRect;
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

        if (_selectedFileKey != null && _savedNames.Contains(_selectedFileKey, StringComparer.OrdinalIgnoreCase))
        {
            SelectAsset(_selectedFileKey, assets, persistPending: false);
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
            CancelSelection(restoreLifted: true);
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
        HandleNewAssetButton(assets);
        HandleCloneAssetButton(assets);
        HandleDeleteAssetButton(assets);
        HandleAssetList(assets);
        HandleAssetListScroll();
        HandleToolButtons(assets);
        HandleUndoShortcut(assets);

        if (!_isPlacing)
        {
            HandlePaletteInput();
            HandleColorPickerInput();
            if (_tool == EditorTool.Brush)
            {
                HandleCanvasPaint(assets);
            }
            else
            {
                HandleCanvasSelect(assets);
            }
        }

        if (_pixelsDirty &&
            (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT) ||
             Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_RIGHT)))
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

        float dividerX = _panelRect.X + AssetColumnWidth + ColumnGap * 0.5f;
        Raylib.DrawLineV(
            new Vector2(dividerX, _panelRect.Y + PanelPadding + 28),
            new Vector2(dividerX, _panelRect.Y + _panelRect.Height - PanelPadding),
            new Color(60, 65, 80, 255));

        UiText.DrawText("Asset Editor", (int)_panelRect.X + PanelPadding, (int)_panelRect.Y + PanelPadding, 22, Color.WHITE);

        DrawAssetListColumn();

        int x = (int)_editorColumnX;
        int y = (int)(_panelRect.Y + PanelPadding + 30);

        UiText.DrawText("Name", x, y, 14, new Color(150, 155, 170, 255));
        y += 18;
        _nameField.Draw(_nameFieldRect, _nameFocused);
        y += 34;

        DrawDimensionRow(x, ref y);
        y += 8;

        string mode = _isPlacing ? "Click the map to place"
            : _tool == EditorTool.Select ? "Drag to select, then drag selection to move"
            : "Ctrl+Z undo · changes save automatically";
        UiText.DrawText(mode, x, y, 15, new Color(180, 185, 195, 255));
        y += 22;

        DrawCanvas(y);
        y += _height * CellSize + 12;

        DrawTools(x, y);
        y += 32;

        DrawPalette(x, y);
        y += 28;

        _colorPicker.Draw(x, y);
        y += _colorPicker.Height + 8;

        DrawActionButtons(x, y);

        if (_statusTimer > 0f)
        {
            UiText.DrawText(_statusMessage, (int)_panelRect.X + PanelPadding, (int)(_panelRect.Y + _panelRect.Height - 22), 14, new Color(170, 220, 150, 255));
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
        int colorPickerBlock = _colorPicker.Height + 8;
        int editorContentHeight = 18 + 34 + 24 + 8 + 22 + canvasBlock + 12 + 32 + 28 + colorPickerBlock + 36;
        int panelHeight = PanelPadding * 2 + 30 + editorContentHeight + 24;
        _panelRect = new Rectangle(screenWidth - PanelWidth - 12, 12, PanelWidth, panelHeight);

        _leftColumnX = _panelRect.X + PanelPadding;
        _editorColumnX = _panelRect.X + AssetColumnWidth + ColumnGap + PanelPadding;
        _editorColumnWidth = EditorColumnWidth - PanelPadding * 2;

        float contentY = _panelRect.Y + PanelPadding + 30;

        _nameFieldRect = new Rectangle(_editorColumnX, contentY + 18, _editorColumnWidth, 28);
        _canvasRect = new Rectangle(
            _editorColumnX,
            _nameFieldRect.Y + 34 + 24 + 8 + 22,
            _width * CellSize,
            canvasBlock);

        float toolsY = _canvasRect.Y + _canvasRect.Height + 12;
        _brushToolRect = new Rectangle(_editorColumnX, toolsY, 52, 24);
        _selectToolRect = new Rectangle(_editorColumnX + 58, toolsY, 52, 24);
        _undoButtonRect = new Rectangle(_editorColumnX + 116, toolsY, 52, 24);

        const int swatch = 24;
        const int gap = 4;
        float paletteY = toolsY + 32;
        int paletteWidth = Palette.Length * (swatch + gap) + swatch + gap + swatch;
        _paletteRect = new Rectangle(_editorColumnX, paletteY, paletteWidth, swatch);
        _customSwatchRect = new Rectangle(
            _paletteRect.X + Palette.Length * (swatch + gap) + swatch + gap,
            paletteY,
            swatch,
            swatch);

        float colorPickerY = paletteY + 28;
        _colorPickerRect = new Rectangle(_editorColumnX, colorPickerY, _editorColumnWidth, _colorPicker.Height);

        float actionY = colorPickerY + _colorPicker.Height + 8;
        _clearButtonRect = new Rectangle(_editorColumnX, actionY, 68, 28);
        _placeButtonRect = new Rectangle(_editorColumnX + 74, actionY, 68, 28);
        _closeButtonRect = new Rectangle(_editorColumnX + 148, actionY, 68, 28);

        float assetHeaderY = contentY;
        _newAssetButtonRect = new Rectangle(_leftColumnX, assetHeaderY + 18, 52, 22);
        _cloneAssetButtonRect = new Rectangle(_leftColumnX + 56, assetHeaderY + 18, 52, 22);
        _deleteAssetButtonRect = new Rectangle(_leftColumnX + 112, assetHeaderY + 18, 52, 22);

        float listY = assetHeaderY + 48;
        float listHeight = _panelRect.Y + _panelRect.Height - PanelPadding - 24 - listY;
        _assetListRect = new Rectangle(_leftColumnX, listY, AssetColumnWidth, listHeight);
        _assetListVisibleRows = Math.Max(1, (int)(listHeight / AssetRowHeight));
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
        _canvasRect = new Rectangle(_editorColumnX, y, _width * CellSize, _height * CellSize);

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

        DrawSelectionOverlay();
    }

    private void DrawSelectionOverlay()
    {
        if (_isSelecting)
        {
            NormalizeRect(_selectStartX, _selectStartY, _selectEndX, _selectEndY, out int x, out int y, out int w, out int h);
            DrawSelectionRect(x, y, w, h, new Color(240, 200, 80, 255));
            return;
        }

        if (_isMovingSelection && _liftedPixels != null)
        {
            DrawFloatingPixels(_moveX, _moveY, _selW, _selH, _liftedPixels);
            DrawSelectionRect(_moveX, _moveY, _selW, _selH, new Color(240, 200, 80, 255));
            return;
        }

        if (_hasSelection)
        {
            DrawSelectionRect(_selX, _selY, _selW, _selH, new Color(240, 200, 80, 255));
        }
    }

    private void DrawSelectionRect(int px, int py, int pw, int ph, Color color)
    {
        if (pw <= 0 || ph <= 0)
        {
            return;
        }

        float left = _canvasRect.X + px * CellSize;
        float top = _canvasRect.Y + py * CellSize;
        float right = left + pw * CellSize;
        float bottom = top + ph * CellSize;

        Raylib.DrawLineEx(new Vector2(left, top), new Vector2(right, top), 1f, color);
        Raylib.DrawLineEx(new Vector2(right, top), new Vector2(right, bottom), 1f, color);
        Raylib.DrawLineEx(new Vector2(right, bottom), new Vector2(left, bottom), 1f, color);
        Raylib.DrawLineEx(new Vector2(left, bottom), new Vector2(left, top), 1f, color);
    }

    private void DrawFloatingPixels(int destX, int destY, int w, int h, Color[] pixels)
    {
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                Color color = pixels[py * w + px];
                if (color.A == 0)
                {
                    continue;
                }

                int canvasX = destX + px;
                int canvasY = destY + py;
                if (canvasX < 0 || canvasY < 0 || canvasX >= _width || canvasY >= _height)
                {
                    continue;
                }

                var cell = new Rectangle(
                    _canvasRect.X + canvasX * CellSize,
                    _canvasRect.Y + canvasY * CellSize,
                    CellSize,
                    CellSize);
                Raylib.DrawRectangleRec(cell, color);
            }
        }
    }

    private void DrawTools(int x, int y)
    {
        DrawButton(_brushToolRect, "Brush", _tool == EditorTool.Brush);
        DrawButton(_selectToolRect, "Select", _tool == EditorTool.Select);
        DrawButton(_undoButtonRect, "Undo", false);
    }

    private void DrawPalette(int x, int y)
    {
        const int swatch = 24;
        const int gap = 4;
        float paletteY = _paletteRect.Y;

        for (int i = 0; i < Palette.Length; i++)
        {
            var rect = new Rectangle(x + i * (swatch + gap), paletteY, swatch, swatch);
            Raylib.DrawRectangleRec(rect, Palette[i]);
            Raylib.DrawRectangleLinesEx(rect, (!_useEraser && !_useCustomColor && i == _selectedColorIndex) ? 2f : 1f,
                (!_useEraser && !_useCustomColor && i == _selectedColorIndex) ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));
        }

        var eraserRect = new Rectangle(x + Palette.Length * (swatch + gap), paletteY, swatch, swatch);
        Raylib.DrawRectangleRec(eraserRect, new Color(32, 35, 42, 255));
        UiText.DrawText("X", (int)eraserRect.X + 7, (int)eraserRect.Y + 3, 16, Color.LIGHTGRAY);
        Raylib.DrawRectangleLinesEx(eraserRect, _useEraser ? 2f : 1f, _useEraser ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));

        Raylib.DrawRectangleRec(_customSwatchRect, _colorPicker.Color);
        Raylib.DrawRectangleLinesEx(_customSwatchRect, _useCustomColor ? 2f : 1f,
            _useCustomColor ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));
    }

    private void DrawActionButtons(int x, int y)
    {
        DrawButton(_clearButtonRect, "Clear", false);
        DrawButton(_placeButtonRect, "Place", _isPlacing);
        DrawButton(_closeButtonRect, "Close", false);
    }

    private void DrawAssetListColumn()
    {
        int x = (int)_leftColumnX;
        int y = (int)(_panelRect.Y + PanelPadding + 30);
        UiText.DrawText("Assets", x, y, 14, new Color(150, 155, 170, 255));
        DrawButton(_newAssetButtonRect, "New", false);
        DrawButton(_cloneAssetButtonRect, "Clone", false);
        DrawButton(_deleteAssetButtonRect, "Delete", false);

        Raylib.DrawRectangleRec(_assetListRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_assetListRect, 1f, new Color(70, 75, 90, 255));

        if (_savedNames.Count == 0)
        {
            UiText.DrawText("(none yet)", (int)_assetListRect.X + 8, (int)_assetListRect.Y + 6, 14, new Color(110, 115, 130, 255));
            return;
        }

        int maxScroll = Math.Max(0, _savedNames.Count - _assetListVisibleRows);
        _assetListScroll = Math.Clamp(_assetListScroll, 0, maxScroll);

        for (int row = 0; row < _assetListVisibleRows; row++)
        {
            int index = _assetListScroll + row;
            if (index >= _savedNames.Count)
            {
                break;
            }

            string name = _savedNames[index];
            bool selected = string.Equals(name, _selectedFileKey, StringComparison.OrdinalIgnoreCase);
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
        int x = (int)_editorColumnX;
        int y = (int)_nameFieldRect.Y + 34;

        if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 42, y - 2, 22, 22)))
        {
            PushUndoSnapshot();
            ResizeCanvas(_width - 1, _height);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 68, y - 2, 22, 22)))
        {
            PushUndoSnapshot();
            ResizeCanvas(_width + 1, _height);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 94, y - 2, 22, 22)))
        {
            PushUndoSnapshot();
            ResizeCanvas(_width, _height - 1);
            PersistCurrent(assets);
        }
        else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(x + 120, y - 2, 22, 22)))
        {
            PushUndoSnapshot();
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

        if (Raylib.CheckCollisionPointRec(mouse, _clearButtonRect))
        {
            CancelSelection(restoreLifted: true);
            PushUndoSnapshot();
            ClearCanvas();
            PersistCurrent(assets);
            SetStatus("Canvas cleared");
        }
        else if (Raylib.CheckCollisionPointRec(mouse, _placeButtonRect))
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
        else if (Raylib.CheckCollisionPointRec(mouse, _closeButtonRect))
        {
            PersistCurrent(assets);
            _isOpen = false;
            _isPlacing = false;
        }
    }

    private void HandleNewAssetButton(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _newAssetButtonRect))
        {
            return;
        }

        BeginNewAsset(assets);
        SetStatus("New asset");
    }

    private void HandleCloneAssetButton(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _cloneAssetButtonRect))
        {
            return;
        }

        CloneCurrentAsset(assets);
    }

    private void HandleDeleteAssetButton(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _deleteAssetButtonRect))
        {
            return;
        }

        DeleteSelectedAsset(assets);
    }

    private void DeleteSelectedAsset(AssetLibrary assets)
    {
        string? fileKey = _selectedFileKey ?? _savedFileKey;
        if (fileKey == null)
        {
            SetStatus("Select an asset to delete");
            return;
        }

        try
        {
            SavedAsset file = DefinedAssetStore.LoadAsset(fileKey);
            string displayName = file.Name;

            DefinedAssetStore.DeleteAsset(fileKey);
            assets.RemoveAsset(displayName);
            assets.RemovePlacementsForAsset(displayName);

            RefreshAssetList();
            CancelSelection(restoreLifted: true);

            if (_savedNames.Count > 0)
            {
                SelectAsset(_savedNames[0], assets, persistPending: false);
            }
            else
            {
                BeginNewAsset(assets);
            }

            SetStatus($"Deleted '{displayName}'");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void CloneCurrentAsset(AssetLibrary assets)
    {
        PersistCurrent(assets);

        string sourceName = _nameField.Text.Trim();
        if (sourceName.Length == 0)
        {
            sourceName = _savedFileKey ?? "";
        }

        if (DefinedAssetStore.SanitizeFileName(sourceName).Length == 0)
        {
            SetStatus("Enter a name to clone");
            return;
        }

        try
        {
            string cloneName = DefinedAssetStore.SuggestCloneName(sourceName);
            PixelAssetDefinition definition = BuildDefinition();
            string fileKey = DefinedAssetStore.SaveAsset(new SavedAsset
            {
                Name = cloneName,
                Definition = definition,
            });
            assets.DefineOrReplace(cloneName, definition);
            RefreshAssetList();
            SelectAsset(fileKey, assets, persistPending: false);
            SetStatus($"Cloned to '{cloneName}'");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
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
        if (row < 0 || row >= _assetListVisibleRows)
        {
            return;
        }

        int index = _assetListScroll + row;
        if (index < 0 || index >= _savedNames.Count)
        {
            return;
        }

        SelectAsset(_savedNames[index], assets, persistPending: true);
    }

    private void HandleAssetListScroll()
    {
        if (_savedNames.Count <= _assetListVisibleRows)
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
        _assetListScroll = Math.Clamp(_assetListScroll, 0, _savedNames.Count - _assetListVisibleRows);
    }

    private void HandlePaletteInput()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mouse, _brushToolRect) ||
            Raylib.CheckCollisionPointRec(mouse, _selectToolRect))
        {
            return;
        }

        int x = (int)_paletteRect.X;
        int y = (int)_paletteRect.Y;
        const int swatch = 24;
        const int gap = 4;

        for (int i = 0; i < Palette.Length; i++)
        {
            var rect = new Rectangle(x + i * (swatch + gap), y, swatch, swatch);
            if (Raylib.CheckCollisionPointRec(mouse, rect))
            {
                _tool = EditorTool.Brush;
                CancelSelection(restoreLifted: true);
                _selectedColorIndex = i;
                _useEraser = false;
                _useCustomColor = false;
                _colorPicker.SetColor(Palette[i]);
                return;
            }
        }

        var eraserRect = new Rectangle(x + Palette.Length * (swatch + gap), y, swatch, swatch);
        if (Raylib.CheckCollisionPointRec(mouse, eraserRect))
        {
            _tool = EditorTool.Brush;
            CancelSelection(restoreLifted: true);
            _useEraser = true;
            _useCustomColor = false;
            return;
        }

        if (Raylib.CheckCollisionPointRec(mouse, _customSwatchRect))
        {
            _tool = EditorTool.Brush;
            CancelSelection(restoreLifted: true);
            _useEraser = false;
            _useCustomColor = true;
        }
    }

    private void HandleColorPickerInput()
    {
        bool allowInput = !_nameFocused && !_isPlacing;
        if (allowInput &&
            Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) &&
            _colorPicker.ContainsPoint(Raylib.GetMousePosition()))
        {
            _tool = EditorTool.Brush;
            CancelSelection(restoreLifted: true);
            _useEraser = false;
            _useCustomColor = true;
        }

        _colorPicker.Update(allowInput);
    }

    private void HandleToolButtons(AssetLibrary assets)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mouse, _brushToolRect))
        {
            _tool = EditorTool.Brush;
            CancelSelection(restoreLifted: true);
            return;
        }

        if (Raylib.CheckCollisionPointRec(mouse, _selectToolRect))
        {
            _tool = EditorTool.Select;
            return;
        }

        if (Raylib.CheckCollisionPointRec(mouse, _undoButtonRect))
        {
            Undo(assets);
        }
    }

    private void HandleUndoShortcut(AssetLibrary assets)
    {
        if (_nameFocused)
        {
            return;
        }

        bool modifier = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) ||
                        Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL) ||
                        Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER) ||
                        Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER);

        if (modifier && Raylib.IsKeyPressed(KeyboardKey.KEY_Z))
        {
            Undo(assets);
        }
    }

    private void HandleCanvasSelect(AssetLibrary assets)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        if (!TryGetCanvasPixel(mouse, out int px, out int py))
        {
            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (_isSelecting)
                {
                    FinishSelection();
                }
                else if (_isMovingSelection)
                {
                    CommitMove(assets);
                }
            }

            return;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            if (_hasSelection && !_isMovingSelection && PointInSelection(px, py))
            {
                BeginMove(px, py);
            }
            else
            {
                if (_isMovingSelection)
                {
                    CommitMove(assets);
                }
                else if (_hasSelection)
                {
                    CancelSelection(restoreLifted: false);
                }

                _isSelecting = true;
                _selectStartX = px;
                _selectStartY = py;
                _selectEndX = px;
                _selectEndY = py;
            }
        }

        if (_isSelecting && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _selectEndX = px;
            _selectEndY = py;
        }

        if (_isMovingSelection && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _moveX = px - _moveGrabOffsetX;
            _moveY = py - _moveGrabOffsetY;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
        {
            if (_isSelecting)
            {
                FinishSelection();
            }
            else if (_isMovingSelection)
            {
                CommitMove(assets);
            }
        }
    }

    private void BeginMove(int grabPx, int grabPy)
    {
        PushUndoSnapshot();
        _liftedPixels = CopyRegion(_selX, _selY, _selW, _selH);
        ClearRegion(_selX, _selY, _selW, _selH);
        _originalSelX = _selX;
        _originalSelY = _selY;
        _moveGrabOffsetX = grabPx - _selX;
        _moveGrabOffsetY = grabPy - _selY;
        _moveX = _selX;
        _moveY = _selY;
        _isMovingSelection = true;
        _pixelsDirty = true;
    }

    private void CommitMove(AssetLibrary assets)
    {
        if (_liftedPixels == null)
        {
            _isMovingSelection = false;
            return;
        }

        PasteRegion(_moveX, _moveY, _selW, _selH, _liftedPixels);
        _selX = _moveX;
        _selY = _moveY;
        _liftedPixels = null;
        _isMovingSelection = false;
        _hasSelection = _selW > 0 && _selH > 0;
        _pixelsDirty = true;
        PersistCurrent(assets);
    }

    private void FinishSelection()
    {
        _isSelecting = false;
        NormalizeRect(_selectStartX, _selectStartY, _selectEndX, _selectEndY, out _selX, out _selY, out _selW, out _selH);
        _hasSelection = _selW > 0 && _selH > 0;
    }

    private void CancelSelection(bool restoreLifted)
    {
        if (restoreLifted && _isMovingSelection && _liftedPixels != null)
        {
            PasteRegion(_originalSelX, _originalSelY, _selW, _selH, _liftedPixels);
            _pixelsDirty = true;
        }

        _hasSelection = false;
        _isSelecting = false;
        _isMovingSelection = false;
        _liftedPixels = null;
        _selW = 0;
        _selH = 0;
    }

    private bool PointInSelection(int px, int py) =>
        _hasSelection && px >= _selX && py >= _selY && px < _selX + _selW && py < _selY + _selH;

    private bool TryGetCanvasPixel(Vector2 mouse, out int px, out int py)
    {
        if (!Raylib.CheckCollisionPointRec(mouse, _canvasRect))
        {
            px = 0;
            py = 0;
            return false;
        }

        px = (int)((mouse.X - _canvasRect.X) / CellSize);
        py = (int)((mouse.Y - _canvasRect.Y) / CellSize);
        px = Math.Clamp(px, 0, _width - 1);
        py = Math.Clamp(py, 0, _height - 1);
        return true;
    }

    private static void NormalizeRect(int x1, int y1, int x2, int y2, out int x, out int y, out int w, out int h)
    {
        x = Math.Min(x1, x2);
        y = Math.Min(y1, y2);
        w = Math.Abs(x2 - x1) + 1;
        h = Math.Abs(y2 - y1) + 1;
    }

    private Color[] CopyRegion(int x, int y, int w, int h)
    {
        var copy = new Color[w * h];
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                copy[py * w + px] = _pixels[(y + py) * _width + (x + px)];
            }
        }

        return copy;
    }

    private void ClearRegion(int x, int y, int w, int h)
    {
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                int cx = x + px;
                int cy = y + py;
                if (cx < 0 || cy < 0 || cx >= _width || cy >= _height)
                {
                    continue;
                }

                _pixels[cy * _width + cx] = Color.BLANK;
            }
        }
    }

    private void PasteRegion(int destX, int destY, int w, int h, Color[] pixels)
    {
        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                int cx = destX + px;
                int cy = destY + py;
                if (cx < 0 || cy < 0 || cx >= _width || cy >= _height)
                {
                    continue;
                }

                Color color = pixels[py * w + px];
                if (color.A == 0)
                {
                    continue;
                }

                _pixels[cy * _width + cx] = color;
            }
        }
    }

    private void HandleCanvasPaint(AssetLibrary assets)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        if (!Raylib.CheckCollisionPointRec(mouse, _canvasRect))
        {
            return;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) ||
            Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT))
        {
            PushUndoSnapshot();
        }

        bool painting = Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT);
        bool erasing = Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT);
        if (!painting && !erasing)
        {
            return;
        }

        int px = (int)((mouse.X - _canvasRect.X) / CellSize);
        int py = (int)((mouse.Y - _canvasRect.Y) / CellSize);
        if (px < 0 || py < 0 || px >= _width || py >= _height)
        {
            return;
        }

        Color next = erasing || _useEraser
            ? Color.BLANK
            : _useCustomColor
                ? _colorPicker.Color
                : Palette[_selectedColorIndex];
        int index = py * _width + px;
        if (_pixels[index].Equals(next))
        {
            return;
        }

        _pixels[index] = next;
        _pixelsDirty = true;
    }

    private void PushUndoSnapshot()
    {
        _undoStack.Add(CaptureSnapshot());
        if (_undoStack.Count > MaxUndoSteps)
        {
            _undoStack.RemoveAt(0);
        }
    }

    private CanvasSnapshot CaptureSnapshot()
    {
        var copy = new Color[_pixels.Length];
        _pixels.CopyTo(copy, 0);
        return new CanvasSnapshot
        {
            Width = _width,
            Height = _height,
            Pixels = copy,
        };
    }

    private void RestoreSnapshot(CanvasSnapshot snapshot)
    {
        _width = snapshot.Width;
        _height = snapshot.Height;
        _pixels = new Color[snapshot.Pixels.Length];
        snapshot.Pixels.CopyTo(_pixels, 0);
        CancelSelection(restoreLifted: false);
    }

    private void Undo(AssetLibrary assets)
    {
        if (_undoStack.Count == 0)
        {
            SetStatus("Nothing to undo");
            return;
        }

        if (_isMovingSelection)
        {
            CancelSelection(restoreLifted: true);
        }

        RestoreSnapshot(_undoStack[^1]);
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _pixelsDirty = true;
        PersistCurrent(assets);
        SetStatus("Undone");
    }

    private void ClearUndoHistory() =>
        _undoStack.Clear();

    private void CommitNameChange(AssetLibrary assets)
    {
        string newName = _nameField.Text.Trim();
        if (DefinedAssetStore.SanitizeFileName(newName).Length == 0)
        {
            return;
        }

        string newFileKey = DefinedAssetStore.ToAssetFileName(newName);
        if (string.Equals(newFileKey, _savedFileKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PersistCurrent(assets);
        RefreshAssetList();
        _selectedFileKey = newFileKey;
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
        _savedFileKey = null;
        _selectedFileKey = null;
        _width = 16;
        _height = 16;
        _pixels = new Color[16 * 16];
        ClearCanvas();
        ClearUndoHistory();
        CancelSelection(restoreLifted: false);
        _pixelsDirty = false;
        _isPlacing = false;
        _nameFocused = false;
    }

    private void SelectAsset(string name, AssetLibrary? assets, bool persistPending)
    {
        if (persistPending && assets != null)
        {
            PersistCurrent(assets);
        }

        SavedAsset file = DefinedAssetStore.LoadAsset(name);
        string fileKey = DefinedAssetStore.ResolveAssetFileStem(name);
        PixelAssetDefinition definition = file.Definition;
        _nameField.SetText(file.Name);
        _savedFileKey = fileKey;
        _selectedFileKey = fileKey;
        ResizeCanvas(definition.Width, definition.Height);
        definition.Pixels.CopyTo(_pixels, 0);
        ClearUndoHistory();
        CancelSelection(restoreLifted: false);
        assets?.DefineOrReplace(file.Name, definition);
        _pixelsDirty = false;
        _isPlacing = false;
        EnsureSelectedAssetVisible();
    }

    private void EnsureSelectedAssetVisible()
    {
        if (_selectedFileKey == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < _savedNames.Count; i++)
        {
            if (string.Equals(_savedNames[i], _selectedFileKey, StringComparison.OrdinalIgnoreCase))
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
        else if (index >= _assetListScroll + _assetListVisibleRows)
        {
            _assetListScroll = index - _assetListVisibleRows + 1;
        }
    }

    private bool PersistCurrent(AssetLibrary assets, out string message) =>
        TryPersistCurrent(assets, out message);

    private void PersistCurrent(AssetLibrary assets) =>
        TryPersistCurrent(assets, out _);

    private bool TryPersistCurrent(AssetLibrary assets, out string message)
    {
        string displayName = _nameField.Text.Trim();
        if (DefinedAssetStore.SanitizeFileName(displayName).Length == 0)
        {
            message = "Enter a valid name";
            return false;
        }

        try
        {
            string newFileKey = DefinedAssetStore.ToAssetFileName(displayName);
            if (_savedFileKey != null && !string.Equals(newFileKey, _savedFileKey, StringComparison.OrdinalIgnoreCase))
            {
                DefinedAssetStore.DeleteAsset(_savedFileKey);
            }

            PixelAssetDefinition definition = BuildDefinition();
            string fileKey = DefinedAssetStore.SaveAsset(new SavedAsset
            {
                Name = displayName,
                Definition = definition,
            });
            assets.DefineOrReplace(displayName, definition);
            _nameField.SetText(displayName);
            _savedFileKey = fileKey;
            _selectedFileKey = fileKey;
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
        CancelSelection(restoreLifted: false);
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
