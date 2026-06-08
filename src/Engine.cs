using SDL3;
namespace Vellum;

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
        SDL.SetHint(SDL.Hints.RenderDriver, "software");
        
        // create window
        Window = SDL.CreateWindow("Vellum", 0, 0, Vellum.Window.DefaultOverlayFlags);
        if (Window == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create window! SDL_Error: {SDL.GetError()}");
        }

        // create renderer
        Renderer = SDL.CreateRenderer(Window, "software");
        if (Renderer == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create renderer! SDL_Error: {SDL.GetError()}");
        }
        
        Vellum.Window.ConfigureOverlay(Window, Renderer);
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
        Input.UpdateStates(Window);
        
        // update updatables, e.g., class events like OnClick
        for (int i = Updatables.Count - 1; i >= 0; i--)
        {
            Updatables[i].Update(DeltaTime);
        }
        
        Vellum.Window.SetClickThrough(Window, GlobalHoverCount == 0);
        
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
        
        Vellum.Window.ClearActiveHandles();
        
        SDL.Quit();
        GC.SuppressFinalize(this);
    }
}