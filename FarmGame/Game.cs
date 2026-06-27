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
    private const string PlayerActionsRelativePath = "../../../Assets/cute-fantasy-free/Cute_Fantasy_Free/Player/Player_Actions.png";

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, Title);
        Raylib.SetTargetFPS(60);
        UiText.Load();

        string mapPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MapTmxRelativePath));
        TileMap map = TileMap.LoadFromTmx(mapPath);

        float scale = ComputeIntegerScale(map.PixelWidth, map.PixelHeight, ScreenWidth, ScreenHeight);
        Vector2 offset = ComputeCenteredOffset(map.PixelWidth, map.PixelHeight, scale, ScreenWidth, ScreenHeight);

        float worldW = map.PixelWidth * scale;
        float worldH = map.PixelHeight * scale;
        Vector2 startPos = new Vector2(worldW * 0.5f, worldH * 0.5f);

        string assetsBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets/cute-fantasy-free/Cute_Fantasy_Free/Player"));
        var player = new Player(
            Path.Combine(assetsBase, "Player.png"),
            Path.Combine(assetsBase, "Player_Actions.png"),
            startPos);

        var hotbar = new Hotbar(player.WalkTexture, player.ActionsTexture);

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            hotbar.Update();
            player.Update(dt, worldW, worldH, hotbar.SelectedTool);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(24, 26, 32, 255));

            map.Draw(scale, offset);
            player.Draw(scale, offset);
            hotbar.Draw(ScreenWidth, ScreenHeight);

            Raylib.EndDrawing();
        }

        player.Unload();
        map.Unload();
        UiText.Unload();
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
