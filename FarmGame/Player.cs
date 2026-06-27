using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class Player
{
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const int ActionFrameSize = 48;
    private const float MoveSpeed = 96f;
    private const float WalkFrameDuration = 0.1f;
    private const float ActionDuration = 0.6f;
    private static readonly int[] ActionFrameSequence = [0, 1];
    private static readonly float ActionFrameDuration = ActionDuration / ActionFrameSequence.Length;

    private static readonly int[] WalkFrameCycle = [0, 1, 2, 3, 4, 5];

    private readonly Texture2D _texture;
    private readonly Texture2D _actionsTexture;

    public Vector2 WorldPosition { get; private set; }

    private Vector2 _velocity;
    private Facing _facing = Facing.Down;
    private bool _flipX;
    private int _animFrame;
    private float _animTimer;

    private bool _isUsingTool;
    private PlayerTool _activeTool;
    private float _actionTimer;
    private bool _strikeApplied;
    private bool _pendingStrike;

    public Texture2D WalkTexture => _texture;
    public Texture2D ActionsTexture => _actionsTexture;

    public Player(string spritePath, string actionsPath, Vector2 startWorldPosition)
    {
        _texture = Raylib.LoadTexture(spritePath);
        _actionsTexture = Raylib.LoadTexture(actionsPath);
        Raylib.SetTextureFilter(_texture, TextureFilter.TEXTURE_FILTER_POINT);
        Raylib.SetTextureFilter(_actionsTexture, TextureFilter.TEXTURE_FILTER_POINT);
        WorldPosition = startWorldPosition;
    }

    public void Update(float deltaTime, float worldWidth, float worldHeight, PlayerTool selectedTool)
    {
        if (_isUsingTool)
        {
            _actionTimer += deltaTime;
            int sequenceIndex = Math.Min((int)(_actionTimer / ActionFrameDuration), ActionFrameSequence.Length - 1);
            if (_activeTool == PlayerTool.Hoe && sequenceIndex >= 1 && !_strikeApplied)
            {
                _strikeApplied = true;
                _pendingStrike = true;
            }

            if (_actionTimer >= ActionDuration)
            {
                _isUsingTool = false;
            }

            _velocity = Vector2.Zero;
            return;
        }

        if (TryStartToolUse(selectedTool))
        {
            return;
        }

        Vector2 input = ReadMoveInput();
        bool moving = input != Vector2.Zero;

        if (moving)
        {
            _velocity = Vector2.Normalize(input) * MoveSpeed;
            UpdateFacing(input);
        }
        else
        {
            _velocity = Vector2.Zero;
        }

        WorldPosition += _velocity * deltaTime;

        float halfW = FrameWidth * 0.5f;
        float halfH = FrameHeight * 0.5f;
        WorldPosition = new Vector2(
            Math.Clamp(WorldPosition.X, halfW, Math.Max(halfW, worldWidth - halfW)),
            Math.Clamp(WorldPosition.Y, halfH, Math.Max(halfH, worldHeight - halfH)));

        UpdateAnimation(deltaTime, moving);
    }

    public void Draw(float mapScale, Vector2 mapOffset)
    {
        if (_isUsingTool)
        {
            DrawToolAction(mapScale, mapOffset);
            return;
        }

        DrawWalkSprite(mapScale, mapOffset);
    }

    public bool ConsumeToolStrike()
    {
        if (!_pendingStrike)
        {
            return false;
        }

        _pendingStrike = false;
        return true;
    }

    public (int X, int Y) GetTargetTile(float mapScale, TileMap map)
    {
        (int tileX, int tileY) = TileMap.WorldToTile(WorldPosition, mapScale, map.TileWidth, map.TileHeight);
        (int dx, int dy) = GetFacingOffset();
        return (tileX + dx, tileY + dy);
    }

    public void Unload()
    {
        Raylib.UnloadTexture(_texture);
        Raylib.UnloadTexture(_actionsTexture);
    }

    private bool TryStartToolUse(PlayerTool selectedTool)
    {
        if (!PlayerToolInfo.HasAction(selectedTool))
        {
            return false;
        }

        bool usePressed = Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE)
            || (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) && !IsMouseOverHotbar());

        if (!usePressed)
        {
            return false;
        }

        _isUsingTool = true;
        _activeTool = selectedTool;
        _actionTimer = 0f;
        _strikeApplied = false;
        _pendingStrike = false;
        _velocity = Vector2.Zero;
        return true;
    }

    private void DrawWalkSprite(float mapScale, Vector2 mapOffset)
    {
        bool moving = _velocity != Vector2.Zero;
        int row = (moving ? 3 : 0) + (int)_facing;
        int frame = moving ? WalkFrameCycle[_animFrame] : 0;

        var src = new Rectangle(frame * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
        if (_flipX)
        {
            src.X += FrameWidth;
            src.Width = -FrameWidth;
        }

        DrawSprite(_texture, src, FrameWidth, FrameHeight, mapScale, mapOffset);
    }

    private void DrawToolAction(float mapScale, Vector2 mapOffset)
    {
        int sideRow = PlayerToolInfo.ActionSideRow(_activeTool);
        int row = sideRow + ActionFacingOffset(_facing);
        int sequenceIndex = Math.Min((int)(_actionTimer / ActionFrameDuration), ActionFrameSequence.Length - 1);
        int column = ActionFrameSequence[sequenceIndex];
        if (_facing == Facing.Side && _flipX)
        {
            column = ActionFrameSequence[ActionFrameSequence.Length - 1 - sequenceIndex];
        }

        var src = new Rectangle(column * ActionFrameSize, row * ActionFrameSize, ActionFrameSize, ActionFrameSize);
        if (_facing == Facing.Side && _flipX)
        {
            src.X += ActionFrameSize;
            src.Width = -ActionFrameSize;
        }

        DrawSprite(_actionsTexture, src, ActionFrameSize, ActionFrameSize, mapScale, mapOffset);
    }

    private void DrawSprite(Texture2D texture, Rectangle src, int frameW, int frameH, float mapScale, Vector2 mapOffset)
    {
        float destW = frameW * mapScale;
        float destH = frameH * mapScale;

        Vector2 screenPos = mapOffset + WorldPosition;
        float destX = screenPos.X - destW * 0.5f;
        float destY = screenPos.Y - destH * 0.5f;
        var dest = new Rectangle(destX, destY, destW, destH);

        Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0f, Color.WHITE);
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Y))
        {
            _facing = Facing.Side;
            _flipX = direction.X < 0f;
        }
        else
        {
            _flipX = false;
            _facing = direction.Y < 0f ? Facing.Up : Facing.Down;
        }
    }

    private void UpdateAnimation(float deltaTime, bool moving)
    {
        if (!moving)
        {
            _animFrame = 0;
            _animTimer = 0f;
            return;
        }

        _animTimer += deltaTime;
        if (_animTimer < WalkFrameDuration)
        {
            return;
        }

        _animTimer -= WalkFrameDuration;
        _animFrame = (_animFrame + 1) % WalkFrameCycle.Length;
    }

    private static bool IsMouseOverHotbar()
    {
        const int barHeight = 72;
        const int barPadding = 12;
        return Raylib.GetMouseY() >= Raylib.GetScreenHeight() - barHeight - barPadding * 2;
    }

    private static Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
        {
            y -= 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
        {
            y += 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
        {
            x -= 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
        {
            x += 1f;
        }

        if (x == 0f && y == 0f)
        {
            return Vector2.Zero;
        }

        return Vector2.Normalize(new Vector2(x, y));
    }

    private (int X, int Y) GetFacingOffset() => _facing switch
    {
        Facing.Down => (0, 1),
        Facing.Up => (0, -1),
        Facing.Side when _flipX => (-1, 0),
        Facing.Side => (1, 0),
        _ => (0, 1),
    };

    private static int ActionFacingOffset(Facing facing) => facing switch
    {
        Facing.Side => 0,
        Facing.Down => 1,
        Facing.Up => 2,
        _ => 1,
    };

    private enum Facing
    {
        Down = 0,
        Side = 1,
        Up = 2,
    }
}
