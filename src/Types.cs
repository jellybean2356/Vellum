namespace Vellum;

public struct Rect(float x, float y, float w, float h)
{
    public float X = x;
    public float Y = y;
    public float Width = w;
    public float Height = h;

    internal SDL3.SDL.FRect ToSdl() => new() { X = X, Y = Y, W = Width, H = Height };
}

public struct Color(byte r, byte g, byte b, byte a = 255)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;
}