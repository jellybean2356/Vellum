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

public enum Key
{
    None = 0x00,
    
    // Modifier Keys
    LeftShift = 0xA0,
    RightShift = 0xA1,
    LeftCtrl = 0xA2,
    RightCtrl = 0xA3,
    LeftAlt = 0xA4,
    RightAlt = 0xA5,
    
    // Controls
    Backspace = 0x08,
    Tab = 0x09,
    Return = 0x0D,
    Escape = 0x1B,
    Space = 0x20,
    
    // Navigation
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    
    // Numbers
    Num0 = 0x30, Num1 = 0x31, Num2 = 0x32, Num3 = 0x33, Num4 = 0x34,
    Num5 = 0x35, Num6 = 0x36, Num7 = 0x37, Num8 = 0x38, Num9 = 0x39,
    
    // Letters
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
    H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
    O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
    V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A
}

public enum MouseButton
{
    None = 0x00,
    Left = 0x01,    
    Right = 0x02,    
    Middle = 0x04,  
    X1 = 0x05,      
    X2 = 0x06        
}