using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public class Game
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string Title = "Farm";

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, Title);
        Raylib.SetTargetFPS(60);
        UiText.Load();

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RAYWHITE);

            const int fontSize = 64;
            Vector2 size = UiText.MeasureTextSize(Title, fontSize);
            float x = (ScreenWidth - size.X) * 0.5f;
            float y = (ScreenHeight - size.Y) * 0.5f;
            UiText.DrawText(Title, (int)x, (int)y, fontSize, Color.DARKGRAY);

            Raylib.EndDrawing();
        }

        UiText.Unload();
        Raylib.CloseWindow();
    }
}
