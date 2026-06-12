using Vellum.Platform;

namespace Vellum.Graphics;

public class Renderer : IDisposable
{
    private static List<IntPtr> _openWindows = [];
    private static List<IntPtr> _openWindowsOld = [];
    private static List<IntPtr> _windowRects = [];
    
    internal IntPtr Handle{get; private set;}
    public bool IsValid => Handle != IntPtr.Zero;

    public Renderer(Window window)
    {
        // WORKING drivers, there are issues with direct3d drivers that i didnt manage to fix
        string[] preferredDrivers = ["vulkan", "opengl", "opengles2"];
    
        // try each driver to get at least one working renderer
        foreach (var driver in preferredDrivers)
        {
            Handle = SDL.CreateRenderer(window.Handle, driver);
            if (Handle != IntPtr.Zero)
            {
                SDL.Log($"Renderer subsystem successfully initialized using driver: {driver}");
                break;
            }
        }

        // fallback to CPU-bound software rendering if hardware initialization fails
        if (Handle == IntPtr.Zero)
        {
            SDL.LogWarn(SDL.LogCategory.Application, "Preferred hardware drivers failed. Falling back to software renderer.");
            Handle = SDL.CreateRenderer(window.Handle, "software");
        }

        if (Handle == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create any renderer context! SDL_Error: {SDL.GetError()}");
        }
        
        // configure stuff
        ConfigureOverlay(window, Handle);
        DwmExtendFrameIntoClientArea(window.Hwnd, new Rect(-1, -1, -1, -1)); // setting margins to -1 to force window to fill client area which triggers a "glass" like effect
    }

    // clear the buffer
    public void Clear(Color color)
    {
        // cache blend mode and clear buffer with color
        SDL.GetRenderDrawBlendMode(Handle, out var blendMode);
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.None);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderClear(Handle);
        
        // restore blend mode
        SDL.SetRenderDrawBlendMode(Handle, blendMode);
    }
    
    // send the buffer to the screen
    public void Present()
    {
        SDL.RenderPresent(Handle);
    }
    
    // draw function for full rectangle
    public void DrawFillRect(Rect rect, Color color)
    {
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderFillRect(Handle, rect);
    }
    
    // draw function for full circle
    public void DrawFillCircle(Circle circle, Color color)
    {
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);

        var cx = (int)circle.X;
        var cy = (int)circle.Y;
        var r = (int)circle.Radius;

        // scan vertically and draw horizontal lines across the width of the circle
        for (var y = -r; y <= r; y++)
        {
            var width = (int)MathF.Sqrt(r * r - y * y);
            SDL.RenderLine(Handle, cx - width, cy + y, cx + width, cy + y);
        }
    }
    
    // configure the window to actually become overlay
    internal void ConfigureOverlay(Window window, IntPtr renderer)
    {
        window.MakeLayered(); // click-through
        
        _openWindows = WindowUtils.GetWindowsHandles();
        _openWindowsOld = WindowUtils.GetWindowsHandles();
        _windowRects.AddRange(_openWindows);
    }
    
    
    // draw debug squares around windows + print when window enters overlay
    public void DrawDebugWindows(Window window)
    {
        _openWindows = WindowUtils.GetWindowsHandles();
        var difference = _openWindows.Except(_openWindowsOld).ToList();
        foreach (var hwnd in difference)
        {
            Console.WriteLine($"found HWND: 0x{hwnd.ToString("X")} | Application: {WindowUtils.GetWindowTitle(hwnd)}");
            if (!_windowRects.Contains(hwnd))
            {
                _windowRects.Add(hwnd); 
            }
        }
        _openWindowsOld = _openWindows;
        _windowRects.RemoveAll(hwnd => !_openWindows.Contains(hwnd));
        
        SDL.SetRenderDrawColor(Handle, 255, 0, 0, 255);
        foreach (var hwnd in _windowRects)
        {
            var drawBox = WindowUtils.GetLocalWindowsBounds(hwnd, window.Handle);
            SDL.RenderRect(Handle, drawBox);
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyRenderer(Handle);
            Handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}