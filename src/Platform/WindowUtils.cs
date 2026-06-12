namespace Vellum.Platform;

public class WindowUtils
{
    private static List<IntPtr> _openWindows = [];
    private static List<IntPtr> _openWindowsOld = [];
    private static List<IntPtr> _windowRects = [];
    
    // helper method to get hwnd
    public static IntPtr GetHwnd(IntPtr window)
    {
        var props = SDL.GetWindowProperties(window);
        return SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
    }

    // helper function to get screen display ids
    public static uint[] GetScreenDisplayIds()
    {
        var displays = SDL.GetDisplays(out _);
        return displays;
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
            var exStyle = GetWindowLongPtr(hWnd, GwlExstyle).ToInt64();
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
            IntPtr owner = GetWindow(hWnd, GwOwner);
            if (owner != IntPtr.Zero && IsWindowVisible(owner)) return true;

            // skips windows that are cloaked (windows flagged as visible even though they are invisible)
            int hr = DwmGetWindowAttribute(hWnd, DwmwaIsCloaked, out int cloaked, sizeof(int));
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
        GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer);

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
        var overlyRect = GetVisualWindowBounds(overlayHwnd);

        float localX = targetRect.X - overlyRect.X;
        float localY = targetRect.Y - overlyRect.Y;

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
        foreach (var hwnd in _windowRects)
        {
            var drawBox = WindowUtils.GetLocalWindowsBounds(hwnd, window.Handle);
            SDL.RenderRect(renderer.Handle, drawBox);
        }
    }
}