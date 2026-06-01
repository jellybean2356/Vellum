using SDL3;
using static Vellum.NativeMethods;
using System.Runtime.InteropServices;

namespace Vellum;

public class Window
{
    // default flags for invisible overlay window
    public const SDL.WindowFlags DefaultOverlayFlags = SDL.WindowFlags.Transparent |
                                                       SDL.WindowFlags.Borderless |
                                                       SDL.WindowFlags.Fullscreen |
                                                       SDL.WindowFlags.AlwaysOnTop |
                                                       SDL.WindowFlags.NotFocusable;

    // private variables
    private static bool _isInitialized;
    private static bool _clickThrough;
    
    private static IntPtr _activeWindow;
    private static IntPtr _activeRenderer;
    
    private static List<IntPtr> _openWindows = [];
    private static List<IntPtr> _openWindowsOld = [];
    private static List<IntPtr> _windowRects = [];

    // helper method to get hwnd
    public static IntPtr GetHwnd(IntPtr window)
    {
        var props = SDL.GetWindowProperties(window);
        return SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
    }

    // make window layered and transparent (click-through)
    public static void MakeLayered(IntPtr window)
    {
        // get hwnd
        var hwnd = GetHwnd(window);
        if (hwnd == IntPtr.Zero)
            return;

        // set ExStyle to layered and transparent
        var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
        _ = SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(exStyle | WsExLayered | WsExTransparent | WsExToolWindow));
        _clickThrough = true;
    }

    // partial click-through handler (for overlays)
    public static void UpdateInput(IntPtr window, SDL.FRect[] interactiveParts)
    {
        // get hwnd
        var hwnd = GetHwnd(window);
        if (hwnd == IntPtr.Zero)
            return;

        _ = SDL.GetGlobalMouseState(out var gx, out var gy); // global cursor position
        SDL.GetWindowPosition(window, out var wx, out var wy); // window position

        // getting window-local coordinates
        var localX = gx - wx;
        var localY = gy - wy;

        // check if cursor is over interactive areas
        var overInteractive = false;
        foreach (var interactivePart in interactiveParts)
        {
            if (!(localX >= interactivePart.X) || !(localX <= interactivePart.X + interactivePart.W) ||
                !(localY >= interactivePart.Y) || !(localY <= interactivePart.Y + interactivePart.H)) continue;
            overInteractive = true;
            break;
        }

        var wantClickTrough = !overInteractive;
        if (wantClickTrough == _clickThrough)
            return;

        _clickThrough = wantClickTrough;

        // toggles transparent exstyle based on click-through state
        var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
        if (wantClickTrough)
            exStyle |= WsExTransparent;
        else
            exStyle &= ~WsExTransparent;

        _ = SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(exStyle));
    }

    // helper function to get screen display ids
    public static uint[]? GetScreenDisplayIds()
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

            if (GetWindowRect(hWnd, out var rect))
            {
                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0) return true;
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
    public static Win32Rect GetVisualWindowBounds(IntPtr hWnd)
    {
        var hr = DwmGetWindowAttribute(hWnd, DwmwaExtendedFrameBounds, out Win32Rect rect, Marshal.SizeOf<Win32Rect>());
        if (hr != 0)
        {
            GetWindowRect(hWnd, out rect);
        }

        return rect;
    }

    // helper method to translate window rect to FRect
    public static SDL.FRect GetWindowFRect(IntPtr targetHwnd, IntPtr overlayHwnd)
    {
        var targetRect = GetVisualWindowBounds(targetHwnd);
        var overlyRect = GetVisualWindowBounds(overlayHwnd);

        float localX = targetRect.Left - overlyRect.Left;
        float localY = targetRect.Top - overlyRect.Top;

        float width = targetRect.Right - targetRect.Left;
        float height = targetRect.Bottom - targetRect.Top;

        return new SDL.FRect
        {
            X = localX,
            Y = localY,
            W = width,
            H = height
        };
    }

    // make the actual invisible overlay
    public static (IntPtr Window, IntPtr Renderer) CreateOverlay()
    {
        // create window
        _activeWindow = SDL.CreateWindow("Vellum", 0, 0, DefaultOverlayFlags);
        if (_activeWindow == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create window! SDL_Error: {SDL.GetError()}");
        }
        
        MakeLayered(_activeWindow);

        // create renderer
        _activeRenderer = SDL.CreateRenderer(_activeWindow, "software");
        if (_activeRenderer == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create renderer! SDL_Error: {SDL.GetError()}");
        }

        return (_activeWindow, _activeRenderer);
    }

    // initialize SDL
    public static bool Initialize()
    {
        if (_isInitialized) return true;
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize! SDL_Error: {SDL.GetError()}");
            return false;
        }
        
        SDL.SetHint(SDL.Hints.RenderDriver, "software");
        
        _openWindows = GetWindowsHandles();
        _openWindowsOld = GetWindowsHandles();
        _windowRects.AddRange(_openWindows);
        
        _isInitialized = true;
        return true;
    }
    
    // draw debug squares around windows + print when window enters overlay
    public static void DrawDebugWindows()
    {
        _openWindows = Window.GetWindowsHandles();
        var difference = _openWindows.Except(_openWindowsOld).ToList();
        foreach (var hwnd in difference)
        {
            Console.WriteLine($"found HWND: 0x{hwnd.ToString("X")} | Application: {Window.GetWindowTitle(hwnd)}");
            if (!_windowRects.Contains(hwnd))
            {
                _windowRects.Add(hwnd); 
            }
        }
        _openWindowsOld = _openWindows;
        _windowRects.RemoveAll(hwnd => !_openWindows.Contains(hwnd));
        
        SDL.SetRenderDrawColor(_activeRenderer, 255, 0, 0, 255);
        foreach (IntPtr hwnd in _windowRects)
        {
            var drawBox = Window.GetWindowFRect(hwnd, _activeWindow);
            SDL.RenderRect(_activeRenderer, drawBox);
        }
    }

    // clear variables to prevent memory bugs
    public static void ClearActiveHandles()
    {
        if (_activeWindow != IntPtr.Zero) SDL.DestroyWindow(_activeWindow);
        if (_activeRenderer != IntPtr.Zero) SDL.DestroyRenderer(_activeRenderer);
    }
}