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
    private const int DemoPlotTileX = 14;
    private const int DemoCornTileX = 15;
    private const int DemoPlotTileY = 10;
    private const int DemoPlayerTileX = 14;
    private const int DemoPlayerTileY = 11;

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
        Vector2 startPos = TileCenterWorld(DemoPlayerTileX, DemoPlayerTileY, map, scale);

        var player = new Player(
            Path.Combine(assetsBase, "Player", "Player.png"),
            Path.Combine(assetsBase, "Player", "Player_Actions.png"),
            startPos);

        var inventory = new Inventory();
        var hotbar = new Hotbar(inventory, player.WalkTexture, player.ActionsTexture, decorTexture);
        var inventoryUi = new InventoryUi(player.WalkTexture, player.ActionsTexture, decorTexture);
        var assets = new AssetLibrary();
        CropDefinitionStore.LoadAll();
        LoadSavedAssets(assets);
        SetupDemoPlot(map, crops, assets);
        var assetEditor = new AssetEditorUi();

        float worldW = map.PixelWidth * scale;
        float worldH = map.PixelHeight * scale;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsKeyPressed(KeyboardKey.KEY_TAB))
            {
                assetEditor.Toggle(assets);
            }

            assetEditor.Update(ScreenWidth, ScreenHeight, assets);

            Vector2 mouse = Raylib.GetMousePosition();
            bool inventoryOpen = inventoryUi.IsOpen;
            bool blockMouseActions = InventoryUi.BlocksGameplayInput(
                mouse, ScreenWidth, ScreenHeight, inventoryOpen, inventoryUi.IsMouseOverPanel(mouse));
            bool allowGameplay = !assetEditor.IsOpen && !inventoryOpen;

            Vector2 offset = ComputeCameraOffset(map.PixelWidth, map.PixelHeight, scale, ScreenWidth, ScreenHeight, player.WorldPosition);

            if (allowGameplay)
            {
                hotbar.Update();
            }

            if (!assetEditor.IsOpen)
            {
                inventoryUi.Update(ScreenWidth, ScreenHeight, inventory);
            }

            if (allowGameplay)
            {
                player.Update(dt, worldW, worldH, hotbar.SelectedTool, allowActions: true, blockMouseActions);

                if (player.ConsumeToolStrike())
                {
                    (int tileX, int tileY) = player.GetTargetTile(scale, map);
                    map.TryHoe(tileX, tileY);
                }

                if (player.ConsumePlantRequest(out PlayerTool seedTool) &&
                    inventory.HotbarHasUsable(hotbar.SelectedIndex, seedTool))
                {
                    CropType? cropType = CropTypeInfo.FromTool(seedTool);
                    if (cropType != null)
                    {
                        (int tileX, int tileY) = player.GetTargetTile(scale, map);
                        if (crops.TryPlant(tileX, tileY, cropType.Value, map, assets))
                        {
                            inventory.TryConsumeFromHotbar(hotbar.SelectedIndex, seedTool);
                        }
                    }
                }
            }
            else if (!assetEditor.IsOpen)
            {
                player.Update(dt, worldW, worldH, hotbar.SelectedTool, allowActions: false, blockMouseActions: true);
            }
            else
            {
                assetEditor.TryPlaceAtScreen(Raylib.GetMousePosition(), assets, scale, offset);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(24, 26, 32, 255));

            map.Draw(scale, offset);
            crops.Draw(map, scale, offset, assets);
            assets.Draw(scale, offset);
            player.Draw(scale, offset);
            assetEditor.DrawPlacementPreview(scale, offset);
            hotbar.Draw(ScreenWidth, ScreenHeight);
            inventoryUi.Draw(ScreenWidth, ScreenHeight, inventory, hotbar.SelectedIndex);
            assetEditor.Draw(ScreenWidth, ScreenHeight);

            GameplayHints.Draw(ScreenWidth, ScreenHeight, inventoryUi.IsOpen, assetEditor.IsOpen);

            Raylib.EndDrawing();

            crops.Update(dt);
        }

        player.Unload();
        assets.Unload();
        Raylib.UnloadTexture(decorTexture);
        map.Unload();
        UiText.Unload();
        Raylib.CloseWindow();
    }

    private static void SetupDemoPlot(TileMap map, CropField crops, AssetLibrary assets)
    {
        map.TryHoe(DemoPlotTileX, DemoPlotTileY);
        map.TryHoe(DemoCornTileX, DemoPlotTileY);
        crops.TryPlant(DemoPlotTileX, DemoPlotTileY, CropType.Tomato, map, assets);
        crops.TryPlant(DemoCornTileX, DemoPlotTileY, CropType.Corn, map, assets);
    }

    private static Vector2 TileCenterWorld(int tileX, int tileY, TileMap map, float scale) =>
        new Vector2((tileX + 0.5f) * map.TileWidth * scale, (tileY + 0.5f) * map.TileHeight * scale);

    private static void LoadSavedAssets(AssetLibrary assets)
    {
        DefinedAssetStore.EnsureDirectoryExists();

        foreach (string name in DefinedAssetStore.ListAssetNames())
        {
            SavedAsset file = DefinedAssetStore.LoadAsset(name);
            assets.DefineOrReplace(file.Name, file.Definition);
        }

        foreach (SavedPlacementEntry placement in DefinedAssetStore.LoadPlacements())
        {
            try
            {
                assets.Place(placement.Asset, new Vector2(placement.X, placement.Y));
            }
            catch (KeyNotFoundException)
            {
                // Skip placements whose asset file was removed.
            }
        }
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
