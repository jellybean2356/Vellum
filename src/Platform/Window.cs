namespace Vellum.Platform;

public class Window : IDisposable
{
    private bool _clickThrough;
    public IntPtr Handle { get; private set; }
    public IntPtr Hwnd => WindowUtils.GetHwnd(Handle);
    public WindowType Type { get; private set; }
    public string Title => SDL.GetWindowTitle(Handle);
    public bool IsValid => Handle != IntPtr.Zero;
    public WindowFlags Flags { get; }
    public int HoverCount { get; set; }

    public Renderer Renderer
    {
        get;
        set
        {
            if (field == value) return;
            field?.Dispose();
            field = value;
        }
    }

    private Window(IntPtr handle, WindowFlags flags, WindowType type)
    {
        Flags = flags;
        Handle = handle;
        Type = type;

        Engine.Windows.Add(this);
    }

    // create window
    public static Window Create(string title, int width, int height, WindowFlags flags)
    {
        if (Engine.Windows.Any(w => w.Type == WindowType.Overlay))
        {
            throw new InvalidOperationException(
                "Engine Constraint Violation: Cannot create a Standard window because an Overlay window already exists. " +
                "The engine cannot run standard windows and overlays simultaneously.");
        }
        
        SDL.WindowFlags sdlFlags = MapFlags(flags);
        IntPtr handle = SDL.CreateWindow(title, width, height, sdlFlags);
        var window = new Window(handle, flags, WindowType.Standard);
        window.Renderer = Renderer.Create(window);
        
        return window;
    }
    public static Window CreateOverlay(string title, uint displayId = 0)
    {
        if (Engine.Windows.Any(w => w.Type == WindowType.Standard))
        {
            throw new InvalidOperationException(
                "Engine Constraint Violation: Cannot create an Overlay window because a Standard window already exists. " +
                "The engine cannot run standard windows and overlays simultaneously.");
        }
        
        if (displayId == 0) displayId = WindowUtils.GetScreenDisplayIds()[0];
        SDL.GetDisplayBounds(displayId, out var rect);
    
        var flags = WindowFlags.DefaultOverlay;
        SDL.WindowFlags sdlFlags = MapFlags(flags);
        
        IntPtr handle = SDL.CreateWindow(title, rect.W, rect.H + 1, sdlFlags);
        var window = new Window(handle, flags, WindowType.Overlay);
        window.MakeLayered();
        window.Renderer = Renderer.Create(window);
    
        window.SetPosition(rect.X, rect.Y);
        WindowUtils.ConfigureOverlay(window);
        
        DwmExtendFrameIntoClientArea(window.Hwnd, new Rect(-1, -1, -1, -1)); 
    
        return window;
    }
    
    // map SDL window functions to Window
    public void Hide() => SDL.HideWindow(Handle);
    public void Show() => SDL.ShowWindow(Handle);
    public void Maximize() => SDL.MaximizeWindow(Handle);
    public void Minimize() => SDL.MinimizeWindow(Handle);
    public void Restore() => SDL.RestoreWindow(Handle);
    public void SetPosition(int x, int y) => SDL.SetWindowPosition(Handle, x, y);
    public void SetSize(int width, int height) => SDL.SetWindowSize(Handle, width, height);
    public void SetTitle(string title) => SDL.SetWindowTitle(Handle, title);
    
    // make window layered and transparent (click-through)
    public void MakeLayered()
    {
        // set ExStyle to layered and transparent
        var exStyle = GetWindowLongPtr(Hwnd, GwlExstyle).ToInt64();
        _ = SetWindowLongPtr(Hwnd, GwlExstyle, new IntPtr(exStyle | WsExLayered | WsExTransparent | WsExToolWindow));
        SetLayeredWindowAttributes(Hwnd, 0, 255, LwaAlpha);
        
        // force windows os to redraw the window
        SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
        
        _clickThrough = true;
    }
    
    // change click-through (true, false) for window
    public void SetClickThrough(bool enabled)
    {
        if (enabled == _clickThrough) return;
    
        var exStyle = GetWindowLongPtr(Hwnd, GwlExstyle).ToInt64();
        
        if (enabled)
            exStyle |= WsExTransparent;
        else
            exStyle &= ~WsExTransparent;
    
        _ = SetWindowLongPtr(Hwnd, GwlExstyle, new IntPtr(exStyle));
        
        SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0017);
        DwmExtendFrameIntoClientArea(Hwnd, new Rect(-1, -1, -1, -1));
    
        _clickThrough = enabled;
    }
    
    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            Engine.Windows.Remove(this);
            Renderer?.Dispose();
            SDL.DestroyWindow(Handle);
            Handle = IntPtr.Zero;
        }
        
        GC.SuppressFinalize(this);
    }

    // map WindowFlags to SDL_WindowFlags
    private static SDL.WindowFlags MapFlags(WindowFlags flags)
    {
        SDL.WindowFlags sdlFlags = 0;
        if (flags.HasFlag(WindowFlags.Hidden))       sdlFlags |= SDL.WindowFlags.Hidden;
        if (flags.HasFlag(WindowFlags.Resizable))    sdlFlags |= SDL.WindowFlags.Resizable;
        if (flags.HasFlag(WindowFlags.Borderless))   sdlFlags |= SDL.WindowFlags.Borderless;
        if (flags.HasFlag(WindowFlags.Fullscreen))   sdlFlags |= SDL.WindowFlags.Fullscreen;
        if (flags.HasFlag(WindowFlags.AlwaysOnTop))  sdlFlags |= SDL.WindowFlags.AlwaysOnTop;
        if (flags.HasFlag(WindowFlags.Transparent))  sdlFlags |= SDL.WindowFlags.Transparent;
        if (flags.HasFlag(WindowFlags.NotFocusable)) sdlFlags |= SDL.WindowFlags.NotFocusable;

        return sdlFlags;
    }
}