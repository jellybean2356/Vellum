namespace Vellum.Platform;

public static class WindowUtils
{
    private static List<IntPtr> _openWindows = [];
    private static List<IntPtr> _openWindowsOld = [];
    private static List<IntPtr> _windowRects = [];
    private static readonly Dictionary<IntPtr, uint> DisplayIds = [];
    private static uint _nextDisplayId = 1;
    
    // helper method to get window hwnd
    public static IntPtr GetHwnd(IntPtr window)
    {
        return SdlWindowBridge.GetHwnd(window);
    }

    // Vellum IDs for Win32 monitors, NOT SDL DISPLAY IDS
    public static uint[] GetScreenDisplayIds()
    {
        var monitors = GetMonitorHandles();
        foreach (var removed in DisplayIds.Keys.Where(m => !monitors.Contains(m)).ToArray())
            DisplayIds.Remove(removed);
        foreach (var monitor in monitors)
            if (!DisplayIds.ContainsKey(monitor)) DisplayIds.Add(monitor, checked(_nextDisplayId++));
        return [.. monitors.Select(m => DisplayIds[m])];
    }

    // native window handles
    public static IntPtr[] GetMonitorHandles()
    {
        var monitors = new List<IntPtr>();
        MonitorEnumProc callback = (monitor, _, ref _, _) =>
        {
            monitors.Add(monitor);
            return true;
        };
        Win32Window.Check(EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero), "EnumDisplayMonitors");
        GC.KeepAlive(callback);
        return [.. monitors];
    }

    // zero selects the primary monitor, explicit IDs come from GetScreenDisplayIds()
    internal static IntPtr GetNativeMonitor(uint displayId = 0)
    {
        if (displayId == 0)
            return MonitorFromPoint(new Win32Point(), 1);
        GetScreenDisplayIds(); // refresh disconnected displays before resolving the ID
        foreach (var display in DisplayIds.Where(display => display.Value == displayId)) return display.Key;
        throw new ArgumentException($"No connected Windows monitor for Vellum display ID {displayId}.", nameof(displayId));
    }

    // obtain bounds of the monitor in WH
    internal static Win32Rect GetNativeMonitorBounds(IntPtr monitor)
    {
        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        Win32Window.Check(GetMonitorInfo(monitor, ref info), "GetMonitorInfoW");
        return info.Monitor;
    }

    // helper function to get all windows
    public static List<IntPtr> GetWindowsHandles()
    {
        var visibleWindows = new List<IntPtr>();

        // getting primary monitor (multiple monitors will be added later)
        var primaryPoint = new Win32Point { X = 0, Y = 0 };
        var hPrimaryMonitor = MonitorFromPoint(primaryPoint, 2);

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true; // skip invisible windows
            if (GetWindowTitle(hWnd).Contains("MainWindowView", StringComparison.OrdinalIgnoreCase))
                return true; // skip main window

            // skips windows without title
            var titleLength = GetWindowTextLength(hWnd);
            if (titleLength == 0) return true;

            // skips windows that are excluded from toolbar
            var exStyle = GetWindowLongPtr(hWnd, GwlExStyle).ToInt64();
            if ((exStyle & WsExToolWindow) != 0) return true;

            // checking if window is on primary monitor
            var hWindowMonitor = MonitorFromWindow(hWnd, 2);
            if (hWindowMonitor != hPrimaryMonitor) return true;

            if (GetWindowRect(hWnd, out Rect rect))
            {
                if (rect.W <= 0 || rect.H <= 0) return true;
            }
            else
            {
                return true;
            }

            // skips windows that are children of other windows
            var owner = GetWindow(hWnd, GwOwner);
            if (owner != IntPtr.Zero && IsWindowVisible(owner)) return true;

            // skips windows that are cloaked (windows flagged as visible even though they are invisible)
            var hr = DwmGetWindowAttribute(hWnd, DwmwaIsCloaked, out int cloaked, sizeof(int));
            if (hr == 0 && cloaked != 0) return true;

            visibleWindows.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        return visibleWindows;
    }

    // helper to get window title, used for debug
    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0) return string.Empty;
        
        var buffer = new char[length + 1];
        var copied = GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer, 0, copied);

    }

    // helper to get window bounds
    public static Rect GetVisualWindowBounds(IntPtr hWnd)
    {
        var hr = DwmGetWindowAttribute(hWnd, DwmwaExtendedFrameBounds, out Rect rect, 16);
        if (hr != 0)
        {
            GetWindowRect(hWnd, out rect);
        }

        return rect;
    }

    // helper method to translate native window bounds into local coordinates
    public static Rect GetLocalWindowsBounds(IntPtr targetHwnd, IntPtr overlayHwnd)
    {
        var targetRect = GetVisualWindowBounds(targetHwnd);
        var origin = new Win32Point();
        Win32Window.Check(ClientToScreen(overlayHwnd, ref origin), "ClientToScreen (overlay bounds)");

        var localX = targetRect.X - origin.X;
        var localY = targetRect.Y - origin.Y;

        return new Rect(localX, localY, targetRect.W, targetRect.H);
    }
    
    // configure the window to actually become overlay
    internal static void ConfigureOverlay(Window window)
    {
        window.MakeLayered(); // click-through
        
        _openWindows = GetWindowsHandles();
        _openWindowsOld = GetWindowsHandles();
        _windowRects.AddRange(_openWindows);
    }

    
    // draw debug squares around windows + print when window enters overlay
    public static void DrawDebugWindows(Window window, Renderer renderer)
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
        
        SDL.SetRenderDrawColor(renderer.Handle, 255, 0, 0, 255);
        foreach (var drawBox in _windowRects.Select(hwnd => WindowUtils.GetLocalWindowsBounds(hwnd, window.Hwnd)))
        {
            SDL.RenderRect(renderer.Handle, drawBox);
        }
    }
}
