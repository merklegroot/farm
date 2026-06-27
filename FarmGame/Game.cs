using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public class Game
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int MapScale = 3;
    private const string Title = "Farm";
    private const string MapTmxRelativePath = "../../../../tiled/tilemap.tmx";

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, Title);
        Raylib.SetTargetFPS(60);
        UiText.Load();

        string mapPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MapTmxRelativePath));
        TileMap map = TileMap.LoadFromTmx(mapPath);

        float scale = MapScale;
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

            if (player.ConsumeToolStrike())
            {
                (int tileX, int tileY) = player.GetTargetTile(scale, map);
                map.TryHoe(tileX, tileY);
            }

            Vector2 offset = ComputeCameraOffset(map.PixelWidth, map.PixelHeight, scale, ScreenWidth, ScreenHeight, player.WorldPosition);

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

    private static Vector2 ComputeCameraOffset(float mapPixelW, float mapPixelH, float scale, int screenW, int screenH, Vector2 focusWorldPos)
    {
        float worldW = mapPixelW * scale;
        float worldH = mapPixelH * scale;

        float offsetX = screenW * 0.5f - focusWorldPos.X;
        float offsetY = screenH * 0.5f - focusWorldPos.Y;

        if (worldW <= screenW)
        {
            offsetX = (screenW - worldW) * 0.5f;
        }
        else
        {
            offsetX = Math.Clamp(offsetX, screenW - worldW, 0f);
        }

        if (worldH <= screenH)
        {
            offsetY = (screenH - worldH) * 0.5f;
        }
        else
        {
            offsetY = Math.Clamp(offsetY, screenH - worldH, 0f);
        }

        return new Vector2(offsetX, offsetY);
    }
}
