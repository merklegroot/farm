using Raylib_cs;

namespace FarmGame;

public sealed class SimpleTextField
{
    private readonly int _maxLength;
    private readonly System.Text.StringBuilder _text = new();

    public SimpleTextField(string initial, int maxLength = 32)
    {
        _maxLength = maxLength;
        SetText(initial);
    }

    public string Text => _text.ToString();

    public void SetText(string value)
    {
        _text.Clear();
        foreach (char c in value)
        {
            if (_text.Length >= _maxLength)
            {
                break;
            }

            if (IsAllowed(c))
            {
                _text.Append(c);
            }
        }
    }

    public void Update(bool focused)
    {
        if (!focused)
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE) && _text.Length > 0)
        {
            _text.Remove(_text.Length - 1, 1);
        }

        int ch;
        while ((ch = Raylib.GetCharPressed()) > 0)
        {
            char c = (char)ch;
            if (!IsAllowed(c) || _text.Length >= _maxLength)
            {
                continue;
            }

            _text.Append(c);
        }
    }

    public void Draw(Rectangle bounds, bool focused)
    {
        Raylib.DrawRectangleRec(bounds, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(bounds, focused ? 2f : 1f, focused ? new Color(240, 200, 80, 255) : new Color(70, 75, 90, 255));

        string display = Text;
        if (focused && (int)(Raylib.GetTime() * 2) % 2 == 0)
        {
            display += "|";
        }

        if (display.Length == 0)
        {
            UiText.DrawText("name", (int)bounds.X + 8, (int)bounds.Y + 6, 16, new Color(100, 105, 120, 255));
            return;
        }

        UiText.DrawText(display, (int)bounds.X + 8, (int)bounds.Y + 6, 16, Color.WHITE);
    }

    private static bool IsAllowed(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '-' or ' ' or '(' or ')';
}
