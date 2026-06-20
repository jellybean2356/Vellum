namespace Vellum.Geometry;

public class Rect(float x, float y, float w, float h) : IShape
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float W { get; set; } = w;
    public float H { get; set; } = h;
    
    public Vellum.Platform.Window LastDrawnWindow { get; set; }

    // ===================================
    // CONVERT TO
    // ===================================

    // convert Rect to SDL.FRect
    public static implicit operator SDL.FRect(Rect r) =>
        new() { X = r.X, Y = r.Y, W = r.W, H = r.H };
    
    // convert Rect to SDL.Rect
    public static implicit operator SDL.Rect(Rect r) =>
        new() { X = (int)r.X,  Y = (int)r.Y, W = (int)r.W, H = (int)r.H };
    
    // converting Win32Rect to Rect while keeping it internal (1st part of the logic, second in NativeMethods.cs)
    internal NativeMethods.Win32Rect ToWin32Rect() =>
        new() { Left = (int)X, Top = (int)Y, Right = (int)(X + W), Bottom = (int)(Y + H) };
    
    // ===================================
    // CONVERT FROM
    // ===================================
    
    // convert SDL.FRect to Rect
    public static implicit operator Rect(SDL.FRect sdl) =>
        new(sdl.X, sdl.Y, sdl.W, sdl.H);
    
    // convert SDL.Rect to Rect
    public static implicit operator Rect(SDL.Rect sdl) =>
        new(sdl.X, sdl.Y, sdl.W, sdl.H);
    
    // converting Rect to Win32Rect while keeping it internal (1st part of the logic, second in NativeMethods.cs)
    internal static Rect FromWin32Rect(NativeMethods.Win32Rect r) =>
        new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

    // check if a point is inside the rect (for basic UI interactions, used by Interactive<> generic wrapper)
    public bool ContainsPoint(float pointX, float pointY)
    {
        return pointX >= X && pointX <= X + W &&
               pointY >= Y && pointY <= Y + H;
    }
}