namespace Vellum.Geometry;

public class Rect : IShape, IRenderable
{
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; }
    public float H { get; set; }
    
    public Window AssociatedWindow { get; set;}
    public Window LastDrawnWindow { get; set; }
    
    public Color Color { get; set; }

    public Rect(float x, float y, float w, float h, Color? color = null, Window associatedWindow = null)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
        Color = color ?? Color.White;
        AssociatedWindow = associatedWindow;
        
        Engine.Renderables.Add(this);
    }

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
    internal Win32Rect ToWin32Rect() =>
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
    internal static Rect FromWin32Rect(Win32Rect r) =>
        new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

    // check if a point is inside the rect (for basic UI interactions, used by Interactive<> generic wrapper)
    public bool ContainsPoint(float pointX, float pointY)
    {
        return pointX >= X && pointX <= X + W &&
               pointY >= Y && pointY <= Y + H;
    }

    void IRenderable.Render(Renderer renderer)
    {
        if (Color.A == 0 || AssociatedWindow == null) return;
        renderer.DrawFillRect(this, Color);
    }
}