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
        var crops = new CropField(map.Width, map.Height);

        string assetsBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Assets/cute-fantasy-free/Cute_Fantasy_Free"));
        Texture2D decorTexture = Raylib.LoadTexture(Path.Combine(assetsBase, "Outdoor decoration", "Outdoor_Decor_Free.png"));
        Raylib.SetTextureFilter(decorTexture, TextureFilter.TEXTURE_FILTER_POINT);

        float scale = MapScale;
        float worldW = map.PixelWidth * scale;
        float worldH = map.PixelHeight * scale;
        Vector2 startPos = new Vector2(worldW * 0.5f, worldH * 0.5f);

        var player = new Player(
            Path.Combine(assetsBase, "Player", "Player.png"),
            Path.Combine(assetsBase, "Player", "Player_Actions.png"),
            startPos);

        var hotbar = new Hotbar(player.WalkTexture, player.ActionsTexture, decorTexture);
        var assets = new AssetLibrary();
        var assetEditor = new AssetEditorUi();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsKeyPressed(KeyboardKey.KEY_TAB))
            {
                assetEditor.Toggle();
            }

            assetEditor.Update(ScreenWidth, ScreenHeight);

            Vector2 offset = ComputeCameraOffset(map.PixelWidth, map.PixelHeight, scale, ScreenWidth, ScreenHeight, player.WorldPosition);

            if (!assetEditor.IsOpen)
            {
                hotbar.Update();
                player.Update(dt, worldW, worldH, hotbar.SelectedTool);
                crops.Update(dt);

                if (player.ConsumeToolStrike())
                {
                    (int tileX, int tileY) = player.GetTargetTile(scale, map);
                    map.TryHoe(tileX, tileY);
                }

                if (player.ConsumePlantRequest(out PlayerTool seedTool))
                {
                    CropType? cropType = CropTypeInfo.FromTool(seedTool);
                    if (cropType != null)
                    {
                        (int tileX, int tileY) = player.GetTargetTile(scale, map);
                        crops.TryPlant(tileX, tileY, cropType.Value, map);
                    }
                }
            }
            else
            {
                crops.Update(dt);
                assetEditor.TryPlaceAtScreen(Raylib.GetMousePosition(), assets, scale, offset);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(24, 26, 32, 255));

            map.Draw(scale, offset);
            crops.Draw(map, scale, offset, decorTexture);
            assets.Draw(scale, offset);
            player.Draw(scale, offset);
            assetEditor.DrawPlacementPreview(scale, offset);
            hotbar.Draw(ScreenWidth, ScreenHeight);
            assetEditor.Draw(ScreenWidth, ScreenHeight);

            if (!assetEditor.IsOpen)
            {
                UiText.DrawText("Tab: Asset Editor", 12, 10, 16, new Color(150, 155, 170, 255));
            }

            Raylib.EndDrawing();
        }

        player.Unload();
        assets.Unload();
        Raylib.UnloadTexture(decorTexture);
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
