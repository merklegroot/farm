using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class Player
{
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const float MoveSpeed = 96f;
    private const float WalkFrameDuration = 0.1f;

    private static readonly int[] WalkFrameCycle = [0, 1, 2, 3, 4, 5];

    private readonly Texture2D _texture;

    public Vector2 WorldPosition { get; private set; }

    private Vector2 _velocity;
    private Facing _facing = Facing.Down;
    private bool _flipX;
    private int _animFrame;
    private float _animTimer;

    public Player(string spritePath, Vector2 startWorldPosition)
    {
        _texture = Raylib.LoadTexture(spritePath);
        Raylib.SetTextureFilter(_texture, TextureFilter.TEXTURE_FILTER_POINT);
        WorldPosition = startWorldPosition;
    }

    public void Update(float deltaTime, float worldWidth, float worldHeight)
    {
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
        bool moving = _velocity != Vector2.Zero;
        int row = (moving ? 3 : 0) + (int)_facing;
        int frame = moving ? WalkFrameCycle[_animFrame] : 0;

        var src = new Rectangle(frame * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
        float destW = FrameWidth * mapScale;
        float destH = FrameHeight * mapScale;

        Vector2 screenPos = mapOffset + WorldPosition;
        float destX = screenPos.X - destW * 0.5f;
        float destY = screenPos.Y - destH * 0.5f;
        var dest = new Rectangle(destX, destY, _flipX ? -destW : destW, destH);

        Raylib.DrawTexturePro(_texture, src, dest, Vector2.Zero, 0f, Color.WHITE);
    }

    public void Unload()
    {
        Raylib.UnloadTexture(_texture);
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

    private enum Facing
    {
        Down = 0,
        Side = 1,
        Up = 2
    }
}
