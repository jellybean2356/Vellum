namespace Vellum.Core;

public class Engine : IDisposable
{
    // private variables
    private bool _isInitialized;
    private bool _isRunning;
    private bool _hasRun;
    private bool _hadWindows;
    private bool _drawingFrame;
    private int _mainThreadId;
    private static long _interactionSequence;
    
    internal static readonly List<IUpdatable> Updatables = [];
    internal static readonly List<IRenderable> Renderables = [];
    internal static readonly List<IUpdatable> AssetTickers = [];
    
    internal static readonly List<Window> Windows = [];
    
    public static int GlobalHoverCount { get; set; }
    public Action OnUpdate { get; set; }

    // frame rate settings
    private int _maxFrameRate;
    public int MaxFrameRate
    {
        get => _maxFrameRate;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum frame rate cannot be negative.");

            _maxFrameRate = value;
        }
    }

    public int TargetFrameRate
    {
        get => MaxFrameRate;
        set => MaxFrameRate = value;
    }

    public bool ClampToDisplayRefreshRate { get; set; } = true;
    public int DisplayRefreshRate { get; private set; } = 60;

    public float DeltaTime { get; private set; }

    // handles
    private Renderer _renderer;
    public Renderer Renderer
    {
        get => _renderer ?? (Window?.Renderer);
        set => _renderer = value;
    }

    private Window _window;
    public Window Window
    {
        get => _window is { IsValid: true } ? _window : (Windows.Count > 0 ? Windows[0] : null);
        set => _window = value;
    }
    
    // initialize the engine
    public bool Initialize()
    {
        if (_isInitialized) return true;
        if (!SdlWindowBridge.Initialize())
        {
            Console.Error.WriteLine($"Initializing SDL rendering failed: {SdlWindowBridge.Error}");
            return false;
        }
        
        _isInitialized = true;
        return true;
    }

    // run the engine
    public void Run()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Vellum Engine Error: Cannot execute Run() before Initialize() has completed successfully.");
        }
        
        if (_isRunning)
            throw new InvalidOperationException("Engine.Run() is already active.");
        if (Win32Window.RedrawRequested != null)
            throw new InvalidOperationException("Another engine is already running the native window loop.");
        
        // on start setup
        _isRunning = true;
        _hasRun = true;
        _hadWindows = Windows.Count > 0;
        _mainThreadId = Environment.CurrentManagedThreadId;
        _lastTime = System.Diagnostics.Stopwatch.GetTimestamp();
        DisplayRefreshRate = ResolveDisplayRefreshRate();
        var nextFrame = _lastTime;
        Win32Window.RedrawRequested = RedrawNativeWindow;

        try
        {
            while (_isRunning)
            {
                Update(); // separate other functions from frame pacing loop
                var frameRate = ResolveFrameRate();
                var frameInterval = frameRate > 0
                    ? (long)(System.Diagnostics.Stopwatch.Frequency / (double)frameRate)
                    : 0L;
                
                if (!_isRunning || frameInterval == 0) continue;

                nextFrame += frameInterval;
                var remainingTicks = nextFrame - System.Diagnostics.Stopwatch.GetTimestamp();
                if (remainingTicks <= 0)
                {
                    nextFrame = System.Diagnostics.Stopwatch.GetTimestamp();
                    continue;
                }

                var sleepMilliseconds = (int)(remainingTicks * 1000 / System.Diagnostics.Stopwatch.Frequency);
                if (sleepMilliseconds > 0)
                    Thread.Sleep(sleepMilliseconds);
            }
        }
        finally
        {
            _isRunning = false;
            Win32Window.RedrawRequested = null;
        }
    }

    // stop the engine
    public void Stop() => _isRunning = false;

    // update loop
    public bool Update()
    {
        DeltaTime = GetDeltaTime(); // updating delta-time
        if (!_hasRun)
        {
            throw new InvalidOperationException("Vellum Engine Error: Run() was not called.");
        }
        if (Windows.Count > 0) _hadWindows = true;
        else if (_hadWindows)
        {
            Stop();
            return false;
        }
        
        // native window lifetime and events cleanup
        if (!Win32Window.ProcessMessages())
        {
            Stop();
            return false;
        }
        foreach (var window in Windows.Where(w => w.IsCloseRequested).ToArray())
            window.Dispose();
        SdlWindowBridge.DiscardEvents();
        if (_hadWindows && Windows.Count == 0)
        {
            Stop();
            return false;
        }
        
        // update input
        Manager.UpdateStates(Window);
        
        OnUpdate?.Invoke();
        if (!_isRunning) return false;

        PrepareInteractiveInput();
        
        // update updatables, e.g., class events like OnClick
        for (var i = Updatables.Count - 1; i >= 0; i--)
        {
            Updatables[i].Update(DeltaTime);
        }
        
        foreach (var t in AssetTickers)
        {
            t.Update(DeltaTime);
        }

        foreach (var win in Windows.Where(win => win.Type == WindowType.Overlay))
        {
            win.SetClickThrough(win.HoverCount == 0 && !HasMouseCapture(win));
        }

        foreach (var win in Windows.Where(win => win.IsValid && win.Flags.HasFlag(WindowFlags.ForceTopmost)))
        {
            Win32Window.EnsureTopmost(win.Hwnd);
        }
        
        // render shapes on screen
        foreach (var win in Windows.Where(win => win.Renderer != null))
        {
            RenderWindow(win);
        }

        return true;
    }

    // helper function for Renderer.cs control
    private void RenderWindow(Window window)
    {
        if (_drawingFrame || window.IsCloseRequested || window.Renderer == null || !window.Renderer.IsValid ||
            !window.Renderer.BeginFrame())
            return;

        _drawingFrame = true;
        try
        {
            window.Renderer.Clear(window.Type == WindowType.Overlay || window.UsesLayeredPresentation
                ? Color.Transparent : Color.Black);
            var interactiveTargets = Updatables.OfType<IInteractiveTarget>().ToArray();
            var renderables = Renderables
                .Where(renderable => renderable.AssociatedWindow == window)
                .Select((renderable, index) => new
                {
                    Renderable = renderable,
                    Index = index,
                    Target = interactiveTargets.FirstOrDefault(target =>
                        ReferenceEquals(target.Shape, renderable))
                })
                .OrderBy(item => item.Target?.ZPriority ?? 0)
                .ThenBy(item => item.Target is { ZPriority: null }
                    ? item.Target.InteractionOrder
                    : 0)
                .ThenBy(item => item.Index);

            foreach (var item in renderables)
                item.Renderable.Render(window.Renderer);
            window.Renderer.Present();
        }
        finally
        {
            _drawingFrame = false;
        }
    }

    // redraw window
    private void RedrawNativeWindow(IntPtr hwnd)
    {
        if (!_isRunning || Environment.CurrentManagedThreadId != _mainThreadId) return;
        var window = Windows.FirstOrDefault(w => w.Hwnd == hwnd);
        if (window != null) RenderWindow(window);
    }

    // if mouse is focus on this window 
    private static bool HasMouseCapture(Window window)
    {
        return Updatables.Any(updatable =>
            updatable is IMouseCapture { IsMouseCaptured: true } capture && capture.CapturedWindow == window);
    }

    internal static void MarkInteraction(IInteractiveTarget target)
    {
        target.InteractionOrder = ++_interactionSequence;
    }

    private static void PrepareInteractiveInput()
    {
        var targets = Updatables.OfType<IInteractiveTarget>().ToArray();
        foreach (var target in targets)
            target.SetInputState(false, false);

        var pointerOverTargets = targets
            .Where(target => target.IsPointerOver())
            .ToHashSet();

        var capturedTarget = targets
            .Where(target => target.IsMouseCaptured)
            .OrderByDescending(GetInputZPriority)
            .ThenByDescending(GetInputRecency)
            .ThenByDescending(target => Windows.IndexOf(target.InteractionWindow))
            .FirstOrDefault();

        var inputTarget = capturedTarget ?? pointerOverTargets
            .OrderByDescending(GetInputZPriority)
            .ThenByDescending(GetInputRecency)
            .ThenByDescending(target => Windows.IndexOf(target.InteractionWindow))
            .FirstOrDefault();

        foreach (var target in targets)
        {
            var isInputTarget = ReferenceEquals(target, inputTarget);
            target.SetInputState(isInputTarget, isInputTarget && pointerOverTargets.Contains(target));
        }
    }

    private static int GetInputZPriority(IInteractiveTarget target) => target.ZPriority ?? 0;

    private static long GetInputRecency(IInteractiveTarget target) =>
        target.ZPriority.HasValue ? 0 : target.InteractionOrder;

    // what frame rate should the program use
    private int ResolveFrameRate()
    {
        if (!ClampToDisplayRefreshRate)
            return MaxFrameRate;

        return MaxFrameRate <= 0 ? DisplayRefreshRate : Math.Min(MaxFrameRate, DisplayRefreshRate);
    }

    private int ResolveDisplayRefreshRate()
    {
        var window = Window;
        if (window is not { IsValid: true }) return 60;
        var refreshRate = Win32Window.GetRefreshRate(window.Hwnd);
        return refreshRate > 0 ? refreshRate : 60;
    }
    
    // calculating delta time (time between frames)
    private long _lastTime = System.Diagnostics.Stopwatch.GetTimestamp();
    private float GetDeltaTime()
    {
        var currentTime = System.Diagnostics.Stopwatch.GetTimestamp();
        var dt = (float)System.Diagnostics.Stopwatch.GetElapsedTime(_lastTime, currentTime).TotalSeconds;
        _lastTime = currentTime;
        return dt;
    }
    
    // dispose of the engine resources
    public void Dispose()
    {
        Stop();
        
        while (Windows.Count > 0)
        {
            Windows[^1].Dispose();
        }
        Updatables.Clear();
        Renderables.Clear();
        AssetTickers.Clear();
        GlobalHoverCount = 0;
        _interactionSequence = 0;
        
        SdlWindowBridge.Quit();
        _isInitialized = false;
        GC.SuppressFinalize(this);
    }

    // add asset ticker during runtime
    public static void RegisterAssetTicker(IUpdatable ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        AssetTickers.Add(ticker);
    }
}
