namespace Vellum.Core;

public class Engine : IDisposable
{
    // private variables
    private bool _isInitialized;
    private bool _isRunning;
    
    public Renderer Renderer { get; private set; }
    public Window Window { get; private set; }
    
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
        
        Window = Window.CreateOverlay("Vellum Engine");
        Renderer = Renderer.Create(Window);

        if (Window.Type == WindowType.Overlay)
        {
            WindowUtils.ConfigureOverlay(Window);
            DwmExtendFrameIntoClientArea(Window.Hwnd, new Rect(-1, -1, -1, -1)); 
        }
        
        Renderer.Clear(Color.Transparent);
        Renderer.Present();
        
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
        Input.Manager.UpdateStates(Window.Handle);
        Renderer.Clear(Color.Transparent);
        
        // update updatables, e.g., class events like OnClick
        for (int i = Updatables.Count - 1; i >= 0; i--)
        {
            Updatables[i].Update(DeltaTime);
        }
        
        if (Window.Type == WindowType.Overlay) Window.SetClickThrough(GlobalHoverCount == 0);

        return true;
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
        if (Renderer.Handle != IntPtr.Zero) SDL.DestroyRenderer(Renderer.Handle);
        if (Window.Handle != IntPtr.Zero) SDL.DestroyWindow(Window.Handle);
        
        SDL.Quit();
        GC.SuppressFinalize(this);
    }
}