using System.Numerics;
using Raylib_cs;

namespace FarmGame;

public sealed class ColorPickerUi
{
    private const int SvWidth = 216;
    private const int SvHeight = 72;
    private const int HueWidth = 14;
    private const int SliderHeight = 14;
    private const int SliderGap = 5;
    private const int LabelRowHeight = 16;

    private enum DragTarget
    {
        None,
        SaturationValue,
        Hue,
        Red,
        Green,
        Blue,
    }

    private float _hue;
    private float _saturation = 1f;
    private float _value = 1f;
    private DragTarget _dragTarget;
    private float _cachedSvHue = -1f;
    private Texture2D _svTexture;
    private Texture2D _hueTexture;
    private bool _texturesReady;
    private Rectangle _bounds;
    private Rectangle _svRect;
    private Rectangle _hueRect;
    private Rectangle _redSliderRect;
    private Rectangle _greenSliderRect;
    private Rectangle _blueSliderRect;

    public Color Color => HsvToColor(_hue, _saturation, _value);

    public void SetColor(Color color)
    {
        ColorToHsv(color, out _hue, out _saturation, out _value);
    }

    public void Update(bool allowInput)
    {
        if (!allowInput)
        {
            if (!Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
            {
                _dragTarget = DragTarget.None;
            }

            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();

        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            if (Raylib.CheckCollisionPointRec(mouse, _svRect))
            {
                _dragTarget = DragTarget.SaturationValue;
            }
            else if (Raylib.CheckCollisionPointRec(mouse, _hueRect))
            {
                _dragTarget = DragTarget.Hue;
            }
            else if (Raylib.CheckCollisionPointRec(mouse, _redSliderRect))
            {
                _dragTarget = DragTarget.Red;
            }
            else if (Raylib.CheckCollisionPointRec(mouse, _greenSliderRect))
            {
                _dragTarget = DragTarget.Green;
            }
            else if (Raylib.CheckCollisionPointRec(mouse, _blueSliderRect))
            {
                _dragTarget = DragTarget.Blue;
            }
        }

        if (!Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _dragTarget = DragTarget.None;
            return;
        }

        switch (_dragTarget)
        {
            case DragTarget.SaturationValue:
                ApplySaturationValue(mouse);
                break;
            case DragTarget.Hue:
                ApplyHue(mouse);
                break;
            case DragTarget.Red:
                ApplyChannel(mouse, _redSliderRect, c => new Color((byte)c, Color.G, Color.B, Color.A));
                break;
            case DragTarget.Green:
                ApplyChannel(mouse, _greenSliderRect, c => new Color(Color.R, (byte)c, Color.B, Color.A));
                break;
            case DragTarget.Blue:
                ApplyChannel(mouse, _blueSliderRect, c => new Color(Color.R, Color.G, (byte)c, Color.A));
                break;
        }
    }

    public void Draw(int x, int y)
    {
        EnsureTextures();

        _svRect = new Rectangle(x, y, SvWidth, SvHeight);
        _hueRect = new Rectangle(x + SvWidth + 6, y, HueWidth, SvHeight);
        float sliderY = y + SvHeight + 8;
        _redSliderRect = new Rectangle(x, sliderY, SvWidth + HueWidth + 6, SliderHeight);
        sliderY += SliderHeight + SliderGap;
        _greenSliderRect = new Rectangle(x, sliderY, SvWidth + HueWidth + 6, SliderHeight);
        sliderY += SliderHeight + SliderGap;
        _blueSliderRect = new Rectangle(x, sliderY, SvWidth + HueWidth + 6, SliderHeight);
        sliderY += SliderHeight + 8;
        _bounds = new Rectangle(x, y, SvWidth + HueWidth + 6, sliderY - y);

        Raylib.DrawTexture(_svTexture, (int)_svRect.X, (int)_svRect.Y, Color.WHITE);
        Raylib.DrawRectangleLinesEx(_svRect, 1f, new Color(70, 75, 90, 255));
        DrawSvCursor();

        Raylib.DrawTexture(_hueTexture, (int)_hueRect.X, (int)_hueRect.Y, Color.WHITE);
        Raylib.DrawRectangleLinesEx(_hueRect, 1f, new Color(70, 75, 90, 255));
        DrawHueCursor();

        Color current = Color;
        DrawChannelSlider(_redSliderRect, "R", current.R);
        DrawChannelSlider(_greenSliderRect, "G", current.G);
        DrawChannelSlider(_blueSliderRect, "B", current.B);

        string hsvText = $"H {Math.Round(_hue):0}  S {Math.Round(_saturation * 100):0}%  V {Math.Round(_value * 100):0}%";
        string hexText = $"#{current.R:X2}{current.G:X2}{current.B:X2}{current.A:X2}";
        UiText.DrawText(hsvText, x, (int)sliderY, 13, new Color(150, 155, 170, 255));
        UiText.DrawText(hexText, x + 168, (int)sliderY, 13, new Color(180, 185, 195, 255));
    }

    public int Height =>
        SvHeight + 8 + (SliderHeight + SliderGap) * 3 + 8 + LabelRowHeight;

    public bool ContainsPoint(Vector2 point) =>
        Raylib.CheckCollisionPointRec(point, _bounds);

    public void Unload()
    {
        if (!_texturesReady)
        {
            return;
        }

        Raylib.UnloadTexture(_svTexture);
        Raylib.UnloadTexture(_hueTexture);
        _texturesReady = false;
        _cachedSvHue = -1f;
    }

    private void ApplySaturationValue(Vector2 mouse)
    {
        _saturation = Math.Clamp((mouse.X - _svRect.X) / _svRect.Width, 0f, 1f);
        _value = Math.Clamp(1f - (mouse.Y - _svRect.Y) / _svRect.Height, 0f, 1f);
    }

    private void ApplyHue(Vector2 mouse)
    {
        _hue = Math.Clamp((mouse.Y - _hueRect.Y) / _hueRect.Height, 0f, 1f) * 360f;
    }

    private void ApplyChannel(Vector2 mouse, Rectangle sliderRect, Func<int, Color> apply)
    {
        int channel = (int)Math.Round(Math.Clamp((mouse.X - sliderRect.X - 18f) / (sliderRect.Width - 18f), 0f, 1f) * 255f);
        SetColor(apply(channel));
    }

    private void DrawSvCursor()
    {
        float cx = _svRect.X + _saturation * _svRect.Width;
        float cy = _svRect.Y + (1f - _value) * _svRect.Height;
        Raylib.DrawCircleLines((int)cx, (int)cy, 5f, Color.WHITE);
        Raylib.DrawCircleLines((int)cx, (int)cy, 6f, new Color(20, 22, 28, 255));
    }

    private void DrawHueCursor()
    {
        float cy = _hueRect.Y + _hue / 360f * _hueRect.Height;
        Raylib.DrawLineEx(
            new Vector2(_hueRect.X - 2, cy),
            new Vector2(_hueRect.X + _hueRect.Width + 2, cy),
            2f,
            Color.WHITE);
    }

    private void DrawChannelSlider(Rectangle rect, string label, byte value)
    {
        var track = new Rectangle(rect.X + 18, rect.Y + 3, rect.Width - 18, rect.Height - 6);
        Raylib.DrawRectangleRec(track, new Color(12, 13, 18, 255));
        Raylib.DrawRectangleLinesEx(track, 1f, new Color(70, 75, 90, 255));

        float fillW = track.Width * (value / 255f);
        Raylib.DrawRectangleRec(new Rectangle(track.X, track.Y, fillW, track.Height), ChannelColor(label, value));

        float knobX = track.X + fillW;
        Raylib.DrawCircleV(new Vector2(knobX, track.Y + track.Height * 0.5f), 5f, Color.WHITE);
        Raylib.DrawCircleLines((int)knobX, (int)(track.Y + track.Height * 0.5f), 5f, new Color(20, 22, 28, 255));

        UiText.DrawText(label, (int)rect.X, (int)rect.Y + 1, 13, new Color(150, 155, 170, 255));
        UiText.DrawText(value.ToString(), (int)(rect.X + rect.Width - 28), (int)rect.Y + 1, 13, new Color(180, 185, 195, 255));
    }

    private static Color ChannelColor(string label, byte value) => label switch
    {
        "R" => new Color(value, (byte)0, (byte)0, (byte)255),
        "G" => new Color((byte)0, value, (byte)0, (byte)255),
        "B" => new Color((byte)0, (byte)0, value, (byte)255),
        _ => new Color(value, value, value, (byte)255),
    };

    private void EnsureTextures()
    {
        if (!_texturesReady)
        {
            _hueTexture = BuildHueTexture();
            _texturesReady = true;
        }

        if (Math.Abs(_cachedSvHue - _hue) > 0.01f)
        {
            if (_cachedSvHue >= 0f)
            {
                Raylib.UnloadTexture(_svTexture);
            }

            _svTexture = BuildSvTexture(_hue);
            _cachedSvHue = _hue;
        }
    }

    private static Texture2D BuildSvTexture(float hue)
    {
        Image image = Raylib.GenImageColor(SvWidth, SvHeight, Color.BLANK);
        for (int y = 0; y < SvHeight; y++)
        {
            float value = 1f - y / (float)SvHeight;
            for (int x = 0; x < SvWidth; x++)
            {
                float saturation = x / (float)SvWidth;
                Raylib.ImageDrawPixel(ref image, x, y, HsvToColor(hue, saturation, value));
            }
        }

        Texture2D texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_POINT);
        return texture;
    }

    private static Texture2D BuildHueTexture()
    {
        Image image = Raylib.GenImageColor(HueWidth, SvHeight, Color.BLANK);
        for (int y = 0; y < SvHeight; y++)
        {
            float hue = y / (float)SvHeight * 360f;
            Color color = HsvToColor(hue, 1f, 1f);
            for (int x = 0; x < HueWidth; x++)
            {
                Raylib.ImageDrawPixel(ref image, x, y, color);
            }
        }

        Texture2D texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
        Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_POINT);
        return texture;
    }

    private static Color HsvToColor(float hue, float saturation, float value)
    {
        hue = (hue % 360f + 360f) % 360f;
        float chroma = value * saturation;
        float intermediate = chroma * (1f - Math.Abs(hue / 60f % 2f - 1f));
        float match = value - chroma;

        float red;
        float green;
        float blue;
        if (hue < 60f)
        {
            red = chroma; green = intermediate; blue = 0f;
        }
        else if (hue < 120f)
        {
            red = intermediate; green = chroma; blue = 0f;
        }
        else if (hue < 180f)
        {
            red = 0f; green = chroma; blue = intermediate;
        }
        else if (hue < 240f)
        {
            red = 0f; green = intermediate; blue = chroma;
        }
        else if (hue < 300f)
        {
            red = intermediate; green = 0f; blue = chroma;
        }
        else
        {
            red = chroma; green = 0f; blue = intermediate;
        }

        return new Color(
            (byte)Math.Clamp((int)((red + match) * 255f), 0, 255),
            (byte)Math.Clamp((int)((green + match) * 255f), 0, 255),
            (byte)Math.Clamp((int)((blue + match) * 255f), 0, 255),
            (byte)255);
    }

    private static void ColorToHsv(Color color, out float hue, out float saturation, out float value)
    {
        float red = color.R / 255f;
        float green = color.G / 255f;
        float blue = color.B / 255f;
        float max = Math.Max(red, Math.Max(green, blue));
        float min = Math.Min(red, Math.Min(green, blue));
        float delta = max - min;

        value = max;
        saturation = max <= 0f ? 0f : delta / max;

        if (delta <= 0f)
        {
            hue = 0f;
            return;
        }

        if (max == red)
        {
            hue = 60f * (((green - blue) / delta) % 6f);
        }
        else if (max == green)
        {
            hue = 60f * (((blue - red) / delta) + 2f);
        }
        else
        {
            hue = 60f * (((red - green) / delta) + 4f);
        }

        if (hue < 0f)
        {
            hue += 360f;
        }
    }
}
