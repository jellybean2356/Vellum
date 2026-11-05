namespace Vellum.Platform;

public class Window : IDisposable
{
    private IntPtr _monitor;

    // handle is a wrapped window from SDL3, Hwnd is a native Win32 window
    public IntPtr Handle { get; private set; }
    public IntPtr Hwnd { get; private set; }
    
    internal bool UsesLayeredPresentation { get; private set; }
    internal bool IsCloseRequested => Win32Window.IsCloseRequested(Hwnd);
    
    public WindowType Type { get; private set; }
    public string Title => WindowUtils.GetWindowTitle(Hwnd);
    public bool IsValid => Handle != IntPtr.Zero;
    public WindowFlags Flags { get; }
    public int HoverCount { get; set; }

    
    // renderer handle
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

    private Window(IntPtr handle, IntPtr hwnd, WindowFlags flags, WindowType type, IntPtr monitor)
    {
        Handle = handle;
        Hwnd = hwnd;
        Flags = flags;
        Type = type;
        _monitor = monitor;
        UsesLayeredPresentation = flags.HasFlag(WindowFlags.Transparent);
        Engine.Windows.Add(this);
    }

    // create native window with SDL rendering
    public static Window Create(string title, int width, int height, WindowFlags flags = WindowFlags.Resizable)
    {
        if (Engine.Windows.Any(w => w.Type == WindowType.Overlay))
            throw new InvalidOperationException("Cannot create a Standard window while an Overlay window exists.");
        return CreateCore(title, width, height, flags, WindowType.Standard, 0);
    }

    // convenience overload for CreateOverlay
    public static Window CreateOverlay(string title, uint displayId = 0)
        => CreateOverlay(title, WindowFlags.DefaultOverlay, displayId);

    // create a native window preconfigured to be an overlay on primary monitor or a display ID
    public static Window CreateOverlay(string title, WindowFlags flags, uint displayId = 0)
    {
        if (Engine.Windows.Any(w => w.Type == WindowType.Standard))
            throw new InvalidOperationException("Cannot create an Overlay window while a Standard window exists.");

        flags |= WindowFlags.DefaultOverlay;
        return CreateCore(title, 1, 1, flags, WindowType.Overlay, displayId);
    }

    // the base window creation
    private static Window CreateCore(string title, int width, int height, WindowFlags flags, WindowType type, uint displayId)
    {
        if (!SdlWindowBridge.IsInitialized)
            throw new InvalidOperationException("Call Engine.Initialize() before creating a window.");

        var monitor = flags.HasFlag(WindowFlags.Fullscreen) ? WindowUtils.GetNativeMonitor(displayId) : IntPtr.Zero;
        Win32Rect? bounds = monitor != IntPtr.Zero ? WindowUtils.GetNativeMonitorBounds(monitor) : null;
        
        var hwnd = Win32Window.Create(title, width, height, flags, type, bounds);
        Win32Window.TrackWindow(hwnd);
        
        var wrapper = IntPtr.Zero;
        Window window = null;
        
        try
        {
            // wrap the native window inside SDL
            wrapper = SdlWindowBridge.Wrap(hwnd, flags);
            Win32Window.AttachMessageHandler(hwnd);
            if (!flags.HasFlag(WindowFlags.Fullscreen))
                Win32Window.SetSize(hwnd, width, height);
            window = new Window(wrapper, hwnd, flags, type, monitor);
            window.Renderer = Renderer.Create(window);

            if (SdlWindowBridge.GetHwnd(wrapper) != hwnd)
                throw new InvalidOperationException("The SDL renderer wrapper no longer references the original native HWND.");
            if (type == WindowType.Overlay)
                WindowUtils.ConfigureOverlay(window);
            if (!flags.HasFlag(WindowFlags.Hidden))
                window.Show();
            if (flags.HasFlag(WindowFlags.AlwaysOnTop) || flags.HasFlag(WindowFlags.ForceTopmost))
                Win32Window.EnsureTopmost(hwnd);
            return window;
        }
        catch
        {
            if (window != null) window.Dispose();
            else
            {
                if (wrapper != IntPtr.Zero)
                {
                    Win32Window.DetachMessageHandler(hwnd);
                    SdlWindowBridge.Destroy(wrapper);
                }
                Win32Window.Destroy(hwnd);
            }
            throw;
        }
    }

    // window control functions
    public void Hide() => ChangeVisibility(SwHide);
    public void Show() => ChangeVisibility(Flags.HasFlag(WindowFlags.NotFocusable) ? SwShowNoActivate : SwShow);
    public void Minimize() => ChangeVisibility(SwMinimize);

    public void Maximize()
    {
        if (Flags.HasFlag(WindowFlags.Fullscreen))
        {
            ObjectDisposedException.ThrowIf(!IsValid, this);
            FitMonitor();
            Show();
        }
        else ChangeVisibility(SwMaximize);
    }

    public void Restore()
    {
        ChangeVisibility(Flags.HasFlag(WindowFlags.NotFocusable) ? SwShowNoActivate : SwRestore);
        if (Flags.HasFlag(WindowFlags.Fullscreen)) FitMonitor();
    }

    private void ChangeVisibility(int command)
    {
        ObjectDisposedException.ThrowIf(!IsValid, this);
        if (command != SwHide) SdlWindowBridge.PrepareToShow(Handle, Flags);
        ShowWindow(Hwnd, command); // return value is previous visibility, not success
    }

    public void SetPosition(int x, int y)
    {
        ObjectDisposedException.ThrowIf(!IsValid, this);
        Win32Window.SetPosition(Hwnd, x, y);
    }

    public void SetSize(int width, int height)
    {
        ObjectDisposedException.ThrowIf(!IsValid, this);
        Win32Window.SetSize(Hwnd, width, height);
    }

    public void SetTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        ObjectDisposedException.ThrowIf(!IsValid, this);
        Win32Window.Check(SetWindowText(Hwnd, title), "SetWindowTextW");
    }

    // stretch the window to fill the whole monitor
    internal void FitMonitor()
    {
        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (_monitor == IntPtr.Zero || !GetMonitorInfo(_monitor, ref info))
            _monitor = MonitorFromWindow(Hwnd, 2);
        var bounds = WindowUtils.GetNativeMonitorBounds(_monitor);
        Win32Window.Check(SetWindowPos(Hwnd, IntPtr.Zero, bounds.Left, bounds.Top,
            bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, SwpNoZOrder | SwpNoActivate), "SetWindowPos (monitor bounds)");
    }

    // allow per pixel opacity
    public void MakeLayered()
    {
        ObjectDisposedException.ThrowIf(!IsValid, this);
        var style = GetWindowLongPtr(Hwnd, GwlExStyle).ToInt64();
        Win32Window.SetExtendedStyle(Hwnd, style | WsExLayered | WsExTransparent | WsExToolWindow);
        Win32Window.SetClickThroughState(Hwnd, true);
        UsesLayeredPresentation = true;
    }

    // window click trough state management
    public void SetClickThrough(bool enabled)
    {
        ObjectDisposedException.ThrowIf(!IsValid, this);
        var style = GetWindowLongPtr(Hwnd, GwlExStyle).ToInt64();
        if (((style & WsExTransparent) != 0) != enabled)
            Win32Window.SetExtendedStyle(Hwnd, enabled ? style | WsExTransparent : style & ~WsExTransparent);
        Win32Window.SetClickThroughState(Hwnd, enabled);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            Engine.Windows.Remove(this);
            Renderer?.Dispose();
            Win32Window.DetachMessageHandler(Hwnd);
            SdlWindowBridge.Destroy(Handle);
            Handle = IntPtr.Zero;
        }

        if (Hwnd != IntPtr.Zero)
        {
            Win32Window.Destroy(Hwnd);
            Hwnd = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
