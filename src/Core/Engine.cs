namespace Vellum.Core;

public class Engine : IDisposable
{
    // private variables
    internal static IntPtr Window;
    internal static IntPtr Renderer;
    private bool _isInitialized;
    private bool _isRunning;
    
    internal static readonly List<IUpdatable> Updatables = new();
    public static int GlobalHoverCount { get; set; }

    public float DeltaTime { get; private set; }


    // initialize the engine
    public bool Initialize()
    {
        if (_isInitialized) return true;
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize! SDL_Error: {SDL.GetError()}");
            return false;
        }
        
        // create window
        Window = SDL.CreateWindow("Vellum", 0, 0, Vellum.Platform.Window.DefaultOverlayFlags);
        if (Window == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create window! SDL_Error: {SDL.GetError()}");
            return false;
        }
        
        // WORKING drivers, there are issues with direct3d drivers that i didnt manage to fix
        string[] preferredDrivers = ["vulkan", "opengl", "opengles2"];
    
        // try each driver to get at least one working renderer
        foreach (var driver in preferredDrivers)
        {
            Renderer = SDL.CreateRenderer(Window, driver);
            if (Renderer != IntPtr.Zero)
            {
                SDL.Log($"Successfully initialized renderer using driver: {driver}");
                break;
            }
        }

        // fallback to software driver (CPU rendering)
        if (Renderer == IntPtr.Zero)
        {
            SDL.LogWarn(SDL.LogCategory.Application, "Preferred hardware drivers failed. Falling back to software.");
            Renderer = SDL.CreateRenderer(Window, "software");
        }

        if (Renderer == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create any renderer! SDL_Error: {SDL.GetError()}");
            return false;
        }
        
        // configure stuff
        IntPtr hwnd = Vellum.Platform.Window.GetHwnd(Window);
        Vellum.Platform.Window.ConfigureOverlay(Window, Renderer);
        NativeMethods.DwmExtendFrameIntoClientArea(hwnd, new Rect(-1, -1, -1, -1)); // setting margins to -1 to force window to fill client area which triggers a "glass" like effect
        
        _isInitialized = true;
        _isRunning = true;
        return true;
    }

    // update loop
    public bool Update()
    {
        DeltaTime = GetDeltaTime();
        if (!_isRunning) return false;

        // poll events
        while (SDL.PollEvent(out var e))
        {
            if ((SDL.EventType)e.Type == SDL.EventType.Quit)
            {
                _isRunning = false;
                return false;
            }
        }
        
        // update input
        Input.Input.UpdateStates(Window);
        
        // update updatables, e.g., class events like OnClick
        for (int i = Updatables.Count - 1; i >= 0; i--)
        {
            Updatables[i].Update(DeltaTime);
        }
        
        Vellum.Platform.Window.SetClickThrough(Window, GlobalHoverCount == 0);
        
        // clear the screen
        SDL.SetRenderDrawBlendMode(Renderer, SDL.BlendMode.None);
        SDL.SetRenderDrawColor(Renderer, 0, 0, 0, 0);
        SDL.RenderClear(Renderer);
        
        return true;
    }

    // draw function for full rectangle
    public void DrawFillRect(Rect rect, Color color)
    {
        SDL.SetRenderDrawBlendMode(Renderer, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Renderer, color.R, color.G, color.B, color.A);
        SDL.RenderFillRect(Renderer, rect);
    }

    // send the buffer to the screen
    public void Present()
    {
        SDL.RenderPresent(Renderer);
    }
    
    // calculating delta time (time between frames)
    private ulong _lastTime = SDL.GetTicks();
    private float GetDeltaTime()
    {
        var currentTime = SDL.GetTicks();
        var dt = (currentTime - _lastTime) / 1000f;
        _lastTime = currentTime;
        return dt;
    }
    
    // dispose of the engine resources
    public void Dispose()
    {
        if (Renderer != IntPtr.Zero) SDL.DestroyRenderer(Renderer);
        if (Window != IntPtr.Zero) SDL.DestroyWindow(Window);
        
        Vellum.Platform.Window.ClearActiveHandles();
        
        SDL.Quit();
        GC.SuppressFinalize(this);
    }
}