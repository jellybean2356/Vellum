namespace Vellum.Core;

public class Engine : IDisposable
{
    // private variables
    private bool _isInitialized;
    private bool _isRunning;
    private bool _hasRun;
    
    internal static readonly List<IUpdatable> Updatables = [];
    internal static readonly List<IRenderable> Renderables = [];
    
    internal static readonly List<Window> Windows = [];
    
    public static int GlobalHoverCount { get; set; }

    public float DeltaTime { get; private set; }

    private Renderer _renderer;
    public Renderer Renderer
    {
        get => _renderer ?? (Window?.Renderer);
        set => _renderer = value;
    }

    private Window _window;
    public Window Window
    {
        get => _window ?? (Windows.Count > 0 ? Windows[0] : null);
        set => _window = value;
    }
    
    // initialize the engine
    public bool Initialize()
    {
        if (_isInitialized) return true;
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize! SDL_Error: {SDL.GetError()}");
            return false;
        }
        
        _isInitialized = true;
        return true;
    }

    public void Run()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Vellum Engine Error: Cannot execute Run() before Initialize() has completed successfully.");
        }
        
        _isRunning = true;
        _hasRun = true;
        
        while (Update())
        {
        }
    }

    // update loop
    public bool Update()
    {
        DeltaTime = GetDeltaTime();
        if (!_hasRun)
        {
            throw new InvalidOperationException("Vellum Engine Error: Run() was not called.");
        }

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
        Manager.UpdateStates(Window?.Handle ?? IntPtr.Zero);
        
        // update updatables, e.g., class events like OnClick
        for (var i = Updatables.Count - 1; i >= 0; i--)
        {
            Updatables[i].Update(DeltaTime);
        }
        
        foreach (var win in Windows.Where(win => win.Type == WindowType.Overlay))
        {
            win.SetClickThrough(win.HoverCount == 0);
        }
        
        // render shapes on screen
        foreach (var win in Windows.Where(win => win.Renderer != null))
        {
            win.Renderer.Clear(win.Type == WindowType.Overlay ? Color.Transparent : Color.Black);

            foreach (var renderable in Renderables.Where(renderable => renderable.AssociatedWindow == win))
            {
                renderable.Render(win.Renderer);
            }
            
            win.Renderer.Present();
        }

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
        if (_isInitialized && !_hasRun)
        {
            throw new InvalidOperationException("Vellum Engine Panic: Engine.Initialize() completed, but Engine.Run() was never invoked before the application terminated.");
        }
        
        while (Windows.Count > 0)
        {
            Windows[^1].Dispose();
        }
        
        SDL.Quit();
        GC.SuppressFinalize(this);
    }
}