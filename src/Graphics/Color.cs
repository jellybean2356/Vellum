namespace Vellum.Graphics;

public struct Color(byte r, byte g, byte b, byte a = 255) // basic RGBA color struct + default colors
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;
    
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Black => new(0, 0, 0);
    public static Color White => new(255, 255, 255);
    public static Color Gray => new(128, 128, 128);

    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color Blue => new(0, 0, 255);

    public static Color Yellow => new(255, 255, 0);
    public static Color Orange => new(255, 130, 0);
    public static Color Cyan => new(0, 255, 255);
    public static Color Magenta => new(255, 0, 255);
}