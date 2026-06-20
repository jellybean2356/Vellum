namespace Vellum.Graphics;

public class Renderer : IDisposable
{
    internal IntPtr Handle{get; private set;}
    public bool IsValid => Handle != IntPtr.Zero;
    public Window Window { get; internal set; }

    private Renderer(IntPtr handle)
    {
        Handle = handle;
    }

    // create renderer
    public static Renderer Create(Window window, bool? gpuAccelerated = true)
    {
        var handle = IntPtr.Zero;
        // WORKING drivers, there are issues with direct3d drivers that i didnt manage to fix
        string[] preferredDrivers = ["vulkan", "opengl", "opengles2"];
    
        // try each driver to get at least one working renderer

        if (gpuAccelerated == true)
        {
            foreach (var driver in preferredDrivers)
            {
                handle = SDL.CreateRenderer(window.Handle, driver);
                if (handle != IntPtr.Zero)
                {
                    SDL.Log($"Renderer subsystem successfully initialized using driver: {driver}");
                    break;
                }
            }
        }

        // fallback to CPU-bound software rendering if hardware initialization fails or if gpuAccelerated is false
        if (handle == IntPtr.Zero)
        {
            SDL.LogWarn(SDL.LogCategory.Application, "Preferred hardware drivers failed. Falling back to software renderer.");
            handle = SDL.CreateRenderer(window.Handle, "software");
        }

        if (handle == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create any renderer context! SDL_Error: {SDL.GetError()}");
        }
        
        var renderer = new Renderer(handle);
        renderer.Window = window;
        return renderer;
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
        rect.LastDrawnWindow = this.Window;
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderFillRect(Handle, rect);
    }
    
    // draw function for full circle
    public void DrawFillCircle(Circle circle, Color color)
    {
        circle.LastDrawnWindow = this.Window;
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