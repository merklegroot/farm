using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public class Game
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string Title = "Farm";
    private const string MapTmxRelativePath = "../../../../tiled/tilemap.tmx";
    private const string PlayerSpriteRelativePath = "../../../Assets/cute-fantasy-free/Cute_Fantasy_Free/Player/Player.png";

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, Title);
        Raylib.SetTargetFPS(60);

        string mapPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MapTmxRelativePath));
        TileMap map = TileMap.LoadFromTmx(mapPath);

        float scale = ComputeIntegerScale(map.PixelWidth, map.PixelHeight, ScreenWidth, ScreenHeight);
        Vector2 offset = ComputeCenteredOffset(map.PixelWidth, map.PixelHeight, scale, ScreenWidth, ScreenHeight);

        float worldW = map.PixelWidth * scale;
        float worldH = map.PixelHeight * scale;
        Vector2 startPos = new Vector2(worldW * 0.5f, worldH * 0.5f);

        string playerSpritePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, PlayerSpriteRelativePath));
        var player = new Player(playerSpritePath, startPos);

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            player.Update(dt, worldW, worldH);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(24, 26, 32, 255));

            map.Draw(scale, offset);
            player.Draw(scale, offset);

            Raylib.EndDrawing();
        }

        player.Unload();
        map.Unload();
        Raylib.CloseWindow();
    }

    private static float ComputeIntegerScale(float mapPixelW, float mapPixelH, int screenW, int screenH)
    {
        int scaleX = Math.Max(1, (int)MathF.Floor(screenW / mapPixelW));
        int scaleY = Math.Max(1, (int)MathF.Floor(screenH / mapPixelH));
        return Math.Min(scaleX, scaleY);
    }

    private static Vector2 ComputeCenteredOffset(float mapPixelW, float mapPixelH, float scale, int screenW, int screenH)
    {
        float worldW = mapPixelW * scale;
        float worldH = mapPixelH * scale;
        return new Vector2((screenW - worldW) * 0.5f, (screenH - worldH) * 0.5f);
    }
}
