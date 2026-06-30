using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class ProduceEditorUi
{
    private const int RowHeight = 22;
    private const int FrameVisibleRows = 5;
    private const int AssetPickerVisibleRows = 6;

    private readonly SimpleTextField _nameField = new("new_produce");
    private readonly List<string> _frames = [];
    private readonly List<Rectangle> _frameUpRects = [];
    private readonly List<Rectangle> _frameDownRects = [];
    private readonly List<Rectangle> _frameRemoveRects = [];

    private IReadOnlyList<string> _produceNames = [];
    private IReadOnlyList<string> _assetNames = [];
    private string? _savedFileKey;
    private string? _selectedFileKey;
    private bool _nameFocused;
    private bool _wasNameFocused;
    private bool _dirty;
    private int _produceListScroll;
    private int _frameListScroll;
    private int _assetPickerScroll;
    private int _produceListVisibleRows = 6;
    private float _leftColumnX;
    private float _editorColumnX;
    private float _editorColumnWidth;
    private Rectangle _panelRect;
    private Rectangle _produceListRect;
    private Rectangle _newProduceButtonRect;
    private Rectangle _deleteProduceButtonRect;
    private Rectangle _produceNameFieldRect;
    private Rectangle _frameListRect;
    private Rectangle _assetPickerRect;
    private Action<string>? _setStatus;

    public bool IsNameFieldFocused => _nameFocused;

    public int ContentHeight =>
        18 + 34 + 18 + FrameVisibleRows * RowHeight + 18 + AssetPickerVisibleRows * RowHeight + 12;

    public void SetStatusHandler(Action<string> setStatus) =>
        _setStatus = setStatus;

    public void OnOpen()
    {
        RefreshLists();
        if (_selectedFileKey != null && _produceNames.Contains(_selectedFileKey, StringComparer.OrdinalIgnoreCase))
        {
            SelectProduce(_selectedFileKey, persistPending: false);
            return;
        }

        if (_produceNames.Count > 0)
        {
            SelectProduce(_produceNames[0], persistPending: false);
        }
        else
        {
            BeginNewProduce();
        }
    }

    public void OnClose() =>
        TryPersistCurrent(out _);

    public void Layout(
        Rectangle panelRect,
        float leftColumnX,
        float editorColumnX,
        float editorColumnWidth,
        float contentY,
        int produceListVisibleRows)
    {
        _panelRect = panelRect;
        _leftColumnX = leftColumnX;
        _editorColumnX = editorColumnX;
        _editorColumnWidth = editorColumnWidth;
        _produceListVisibleRows = produceListVisibleRows;

        _newProduceButtonRect = new Rectangle(_leftColumnX, contentY + 18, 78, 22);
        _deleteProduceButtonRect = new Rectangle(_leftColumnX + 84, contentY + 18, 78, 22);

        float listY = contentY + 48;
        float listHeight = _panelRect.Y + _panelRect.Height - 16 - 24 - listY;
        _produceListRect = new Rectangle(_leftColumnX, listY, 168, listHeight);

        _produceNameFieldRect = new Rectangle(_editorColumnX, contentY + 18, _editorColumnWidth, 28);
        float frameListY = _produceNameFieldRect.Y + 34 + 18;
        _frameListRect = new Rectangle(_editorColumnX, frameListY, _editorColumnWidth, FrameVisibleRows * RowHeight);
        float pickerY = _frameListRect.Y + _frameListRect.Height + 18;
        _assetPickerRect = new Rectangle(_editorColumnX, pickerY, _editorColumnWidth, AssetPickerVisibleRows * RowHeight);
    }

    public void Update(bool allowInput)
    {
        _nameField.Update(_nameFocused);

        if (_wasNameFocused && !_nameFocused)
        {
            CommitNameChange();
        }

        _wasNameFocused = _nameFocused;

        if (!allowInput || !Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        HandleProduceListInput(mouse);
        HandleProduceButtons(mouse);
        HandleFrameListInput(mouse);
        HandleAssetPickerInput(mouse);
    }

    public void HandleFocus(Vector2 mouse) =>
        _nameFocused = Raylib.CheckCollisionPointRec(mouse, _produceNameFieldRect);

    public void DrawLeftColumn(float contentY)
    {
        int x = (int)_leftColumnX;
        UiText.DrawText("Produce", x, (int)contentY, 14, new Color(150, 155, 170, 255));
        DrawButton(_newProduceButtonRect, "New", false);
        DrawButton(_deleteProduceButtonRect, "Delete", false);

        Raylib.DrawRectangleRec(_produceListRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_produceListRect, 1f, new Color(70, 75, 90, 255));

        if (_produceNames.Count == 0)
        {
            UiText.DrawText("(none yet)", (int)_produceListRect.X + 8, (int)_produceListRect.Y + 6, 14, new Color(110, 115, 130, 255));
            return;
        }

        int maxScroll = Math.Max(0, _produceNames.Count - _produceListVisibleRows);
        _produceListScroll = Math.Clamp(_produceListScroll, 0, maxScroll);

        for (int row = 0; row < _produceListVisibleRows; row++)
        {
            int index = _produceListScroll + row;
            if (index >= _produceNames.Count)
            {
                break;
            }

            string name = _produceNames[index];
            bool selected = string.Equals(name, _selectedFileKey, StringComparison.OrdinalIgnoreCase);
            var rowRect = new Rectangle(_produceListRect.X, _produceListRect.Y + row * RowHeight, _produceListRect.Width, RowHeight);
            Raylib.DrawRectangleRec(rowRect, selected ? new Color(58, 62, 78, 255) : new Color(32, 35, 42, 255));
            if (selected)
            {
                Raylib.DrawRectangleLinesEx(rowRect, 1f, new Color(240, 200, 80, 255));
            }

            UiText.DrawText(name, (int)rowRect.X + 8, (int)rowRect.Y + 3, 14, selected ? Color.WHITE : Color.LIGHTGRAY);
        }
    }

    public void DrawEditorColumn(float contentY)
    {
        int x = (int)_editorColumnX;

        UiText.DrawText("Name", x, (int)contentY, 14, new Color(150, 155, 170, 255));
        _nameField.Draw(_produceNameFieldRect, _nameFocused);

        UiText.DrawText("Frames", x, (int)(_frameListRect.Y - 18), 14, new Color(150, 155, 170, 255));
        DrawFrameList();

        UiText.DrawText("Add frame", x, (int)(_assetPickerRect.Y - 18), 14, new Color(150, 155, 170, 255));
        DrawAssetPicker();
    }

    public void HandleProduceListScroll(Vector2 mouse)
    {
        if (_produceNames.Count <= _produceListVisibleRows || !Raylib.CheckCollisionPointRec(mouse, _produceListRect))
        {
            return;
        }

        float wheel = Raylib.GetMouseWheelMove();
        if (wheel == 0f)
        {
            return;
        }

        _produceListScroll -= (int)wheel;
        _produceListScroll = Math.Clamp(_produceListScroll, 0, _produceNames.Count - _produceListVisibleRows);
    }

    public void HandleFrameListScroll(Vector2 mouse)
    {
        if (_frames.Count <= FrameVisibleRows || !Raylib.CheckCollisionPointRec(mouse, _frameListRect))
        {
            return;
        }

        float wheel = Raylib.GetMouseWheelMove();
        if (wheel == 0f)
        {
            return;
        }

        _frameListScroll -= (int)wheel;
        _frameListScroll = Math.Clamp(_frameListScroll, 0, _frames.Count - FrameVisibleRows);
    }

    public void HandleAssetPickerScroll(Vector2 mouse)
    {
        if (_assetNames.Count <= AssetPickerVisibleRows || !Raylib.CheckCollisionPointRec(mouse, _assetPickerRect))
        {
            return;
        }

        float wheel = Raylib.GetMouseWheelMove();
        if (wheel == 0f)
        {
            return;
        }

        _assetPickerScroll -= (int)wheel;
        _assetPickerScroll = Math.Clamp(_assetPickerScroll, 0, _assetNames.Count - AssetPickerVisibleRows);
    }

    private void DrawFrameList()
    {
        Raylib.DrawRectangleRec(_frameListRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_frameListRect, 1f, new Color(70, 75, 90, 255));

        _frameUpRects.Clear();
        _frameDownRects.Clear();
        _frameRemoveRects.Clear();

        if (_frames.Count == 0)
        {
            UiText.DrawText("(no frames yet)", (int)_frameListRect.X + 8, (int)_frameListRect.Y + 6, 14, new Color(110, 115, 130, 255));
            return;
        }

        int maxScroll = Math.Max(0, _frames.Count - FrameVisibleRows);
        _frameListScroll = Math.Clamp(_frameListScroll, 0, maxScroll);

        for (int row = 0; row < FrameVisibleRows; row++)
        {
            int index = _frameListScroll + row;
            if (index >= _frames.Count)
            {
                break;
            }

            float rowY = _frameListRect.Y + row * RowHeight;
            var rowRect = new Rectangle(_frameListRect.X, rowY, _frameListRect.Width, RowHeight);
            Raylib.DrawRectangleRec(rowRect, new Color(32, 35, 42, 255));

            UiText.DrawText($"{index + 1}. {_frames[index]}", (int)rowRect.X + 8, (int)rowY + 3, 14, Color.LIGHTGRAY);

            var removeRect = new Rectangle(rowRect.X + rowRect.Width - 22, rowY + 3, 18, 16);
            var downRect = new Rectangle(removeRect.X - 22, rowY + 3, 18, 16);
            var upRect = new Rectangle(downRect.X - 22, rowY + 3, 18, 16);
            _frameUpRects.Add(upRect);
            _frameDownRects.Add(downRect);
            _frameRemoveRects.Add(removeRect);

            DrawSmallButton(upRect, "↑");
            DrawSmallButton(downRect, "↓");
            DrawSmallButton(removeRect, "×");
        }
    }

    private void DrawAssetPicker()
    {
        Raylib.DrawRectangleRec(_assetPickerRect, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(_assetPickerRect, 1f, new Color(70, 75, 90, 255));

        if (_assetNames.Count == 0)
        {
            UiText.DrawText("(create assets first)", (int)_assetPickerRect.X + 8, (int)_assetPickerRect.Y + 6, 14, new Color(110, 115, 130, 255));
            return;
        }

        int maxScroll = Math.Max(0, _assetNames.Count - AssetPickerVisibleRows);
        _assetPickerScroll = Math.Clamp(_assetPickerScroll, 0, maxScroll);

        for (int row = 0; row < AssetPickerVisibleRows; row++)
        {
            int index = _assetPickerScroll + row;
            if (index >= _assetNames.Count)
            {
                break;
            }

            string assetName = _assetNames[index];
            var rowRect = new Rectangle(_assetPickerRect.X, _assetPickerRect.Y + row * RowHeight, _assetPickerRect.Width, RowHeight);
            Raylib.DrawRectangleRec(rowRect, new Color(32, 35, 42, 255));
            UiText.DrawText(assetName, (int)rowRect.X + 8, (int)rowRect.Y + 3, 14, Color.LIGHTGRAY);
        }
    }

    private void HandleProduceListInput(Vector2 mouse)
    {
        if (!Raylib.CheckCollisionPointRec(mouse, _produceListRect))
        {
            return;
        }

        int row = (int)((mouse.Y - _produceListRect.Y) / RowHeight);
        if (row < 0 || row >= _produceListVisibleRows)
        {
            return;
        }

        int index = _produceListScroll + row;
        if (index < 0 || index >= _produceNames.Count)
        {
            return;
        }

        SelectProduce(_produceNames[index], persistPending: true);
    }

    private void HandleProduceButtons(Vector2 mouse)
    {
        if (Raylib.CheckCollisionPointRec(mouse, _newProduceButtonRect))
        {
            BeginNewProduce();
            SetStatus("New produce");
            return;
        }

        if (Raylib.CheckCollisionPointRec(mouse, _deleteProduceButtonRect))
        {
            DeleteCurrentProduce();
        }
    }

    private void HandleFrameListInput(Vector2 mouse)
    {
        for (int i = 0; i < _frameUpRects.Count; i++)
        {
            int index = _frameListScroll + i;
            if (Raylib.CheckCollisionPointRec(mouse, _frameUpRects[i]) && index > 0)
            {
                SwapFrames(index, index - 1);
                return;
            }

            if (Raylib.CheckCollisionPointRec(mouse, _frameDownRects[i]) && index < _frames.Count - 1)
            {
                SwapFrames(index, index + 1);
                return;
            }

            if (Raylib.CheckCollisionPointRec(mouse, _frameRemoveRects[i]))
            {
                _frames.RemoveAt(index);
                _dirty = true;
                PersistIfDirty();
                return;
            }
        }
    }

    private void HandleAssetPickerInput(Vector2 mouse)
    {
        if (!Raylib.CheckCollisionPointRec(mouse, _assetPickerRect))
        {
            return;
        }

        int row = (int)((mouse.Y - _assetPickerRect.Y) / RowHeight);
        if (row < 0 || row >= AssetPickerVisibleRows)
        {
            return;
        }

        int index = _assetPickerScroll + row;
        if (index < 0 || index >= _assetNames.Count)
        {
            return;
        }

        string assetId = ResolveAssetId(_assetNames[index]);
        _frames.Add(assetId);
        _dirty = true;
        PersistIfDirty();
        SetStatus($"Added frame '{assetId}'");
    }

    private static string ResolveAssetId(string fileStem)
    {
        try
        {
            SavedAsset asset = DefinedAssetStore.LoadAsset(fileStem);
            return asset.Name;
        }
        catch (FileNotFoundException)
        {
            return fileStem;
        }
    }

    private void SwapFrames(int a, int b)
    {
        (_frames[a], _frames[b]) = (_frames[b], _frames[a]);
        _dirty = true;
        PersistIfDirty();
    }

    private void BeginNewProduce()
    {
        TryPersistCurrent(out _);
        string name = ProduceDefinitionStore.SuggestNewName();
        _nameField.SetText(name);
        _frames.Clear();
        _savedFileKey = null;
        _selectedFileKey = null;
        _frameListScroll = 0;
        _dirty = false;
    }

    private void SelectProduce(string fileKey, bool persistPending)
    {
        if (persistPending)
        {
            TryPersistCurrent(out _);
        }

        ProduceDefinition definition = ProduceDefinitionStore.Load(fileKey);
        _nameField.SetText(definition.Name);
        _frames.Clear();
        _frames.AddRange(definition.Frames);
        _savedFileKey = ProduceDefinitionStore.ResolveFileStem(fileKey);
        _selectedFileKey = _savedFileKey;
        _frameListScroll = 0;
        _dirty = false;
        EnsureSelectedProduceVisible();
    }

    private void DeleteCurrentProduce()
    {
        string? fileKey = _selectedFileKey ?? _savedFileKey;
        if (fileKey == null)
        {
            SetStatus("Select produce to delete");
            return;
        }

        try
        {
            ProduceDefinition definition = ProduceDefinitionStore.Load(fileKey);
            ProduceDefinitionStore.Delete(fileKey);
            RefreshLists();

            if (_produceNames.Count > 0)
            {
                SelectProduce(_produceNames[0], persistPending: false);
            }
            else
            {
                BeginNewProduce();
            }

            SetStatus($"Deleted '{definition.Name}'");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void CommitNameChange()
    {
        if (_savedFileKey == null)
        {
            PersistIfDirty();
            return;
        }

        string newName = _nameField.Text.Trim();
        string newFileKey = ProduceDefinitionStore.ToFileName(newName);
        if (string.Equals(newFileKey, _savedFileKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PersistIfDirty();
    }

    private void PersistIfDirty()
    {
        if (_dirty || _savedFileKey == null)
        {
            TryPersistCurrent(out _);
        }
    }

    private bool TryPersistCurrent(out string message)
    {
        string displayName = _nameField.Text.Trim();
        if (ProduceDefinitionStore.ToFileName(displayName).Length == 0)
        {
            message = "Enter a valid produce name";
            return false;
        }

        try
        {
            string newFileKey = ProduceDefinitionStore.ToFileName(displayName);
            if (_savedFileKey != null && !string.Equals(newFileKey, _savedFileKey, StringComparison.OrdinalIgnoreCase))
            {
                ProduceDefinitionStore.Delete(_savedFileKey);
            }

            string fileKey = ProduceDefinitionStore.Save(new ProduceDefinition
            {
                Name = displayName,
                Frames = _frames.ToArray(),
            });

            _nameField.SetText(displayName);
            _savedFileKey = fileKey;
            _selectedFileKey = fileKey;
            RefreshLists();
            EnsureSelectedProduceVisible();
            _dirty = false;
            message = "";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    private void RefreshLists()
    {
        _produceNames = ProduceDefinitionStore.ListNames();
        _assetNames = DefinedAssetStore.ListAssetNames();
    }

    private void EnsureSelectedProduceVisible()
    {
        if (_selectedFileKey == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < _produceNames.Count; i++)
        {
            if (string.Equals(_produceNames[i], _selectedFileKey, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        if (index < _produceListScroll)
        {
            _produceListScroll = index;
        }
        else if (index >= _produceListScroll + _produceListVisibleRows)
        {
            _produceListScroll = index - _produceListVisibleRows + 1;
        }
    }

    private void SetStatus(string message) =>
        _setStatus?.Invoke(message);

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
        UiText.DrawText(label, (int)rect.X + 4, (int)rect.Y + 1, 12, Color.WHITE);
    }
}
