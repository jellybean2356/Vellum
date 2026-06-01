using SDL3;
using static Vellum.NativeMethods;
using System.Runtime.InteropServices;

namespace Vellum;

public class Window
{
    public const SDL.WindowFlags DefaultOverlayFlags = SDL.WindowFlags.Transparent |
                                                       SDL.WindowFlags.Borderless |
                                                       SDL.WindowFlags.Fullscreen |
                                                       SDL.WindowFlags.AlwaysOnTop |
                                                       SDL.WindowFlags.NotFocusable;

    private static bool _isInitialized = false;
    private static bool _clickThrough = false;
    
    private static List<IntPtr> _openWindows = [];
    private static List<IntPtr> _openWindowsOld = [];
    private static readonly List<IntPtr> WindowRects = [];

    // helper method to obrain hwnd
    public static IntPtr GetHwnd(IntPtr window)
    {
        var props = SDL.GetWindowProperties(window);
        return SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
    }

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

    // partial click-through handler (NOT REQUIRED)
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
            if (localX >= interactivePart.X && localX <= interactivePart.X + interactivePart.W &&
                localY >= interactivePart.Y && localY <= interactivePart.Y + interactivePart.H)
            {
                overInteractive = true;
                break;
            }
        }

        var wantClickTrough = !overInteractive;
        if (wantClickTrough == _clickThrough)
            return;

        _clickThrough = wantClickTrough;

        // toggles transperent exstyle based on click-through state
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

    // helper function to get all visible windows
    public static List<IntPtr> GetWindowsHandles()
    {
        List<IntPtr> visibleWindows = new List<IntPtr>();

        // getting primary monitor for now, support for other monitors later
        Win32Point primaryPoint = new Win32Point { X = 0, Y = 0 };
        IntPtr hPrimaryMonitor = MonitorFromPoint(primaryPoint, 2);

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true; // skip invisible windows
            if (GetWindowTitle(hWnd).Contains("MainWindowView", StringComparison.OrdinalIgnoreCase))
                return true; // skip main window

            // skips windows without title
            int titleLength = GetWindowTextLength(hWnd);
            if (titleLength == 0) return true;

            // skips windows that are excluded from toolbar
            long exStyle = GetWindowLongPtr(hWnd, GwlExstyle).ToInt64();
            if ((exStyle & WsExToolWindow) != 0) return true;

            // checking if window is on primary monitor
            IntPtr hWindowMonitor = MonitorFromWindow(hWnd, 2);
            if (hWindowMonitor != hPrimaryMonitor) return true;

            if (GetWindowRect(hWnd, out Win32Rect rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0) return true;
            }
            else
            {
                return true;
            }

            // skips windows that are childs of other windows
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

    // helper to obrain window title, used for deubug
    public static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length > 0)
        {
            char[] buffer = new char[length + 1];
            GetWindowText(hWnd, buffer, buffer.Length);
            return new string(buffer);
        }

        return string.Empty;
    }

    // helper to get window bounds
    public static Win32Rect GetVisualWindowBounds(IntPtr hWnd)
    {
        int hr = DwmGetWindowAttribute(hWnd, 9, out Win32Rect rect, Marshal.SizeOf<Rect>());
        if (hr != 0)
        {
            GetWindowRect(hWnd, out rect);
        }

        return rect;
    }

    // helper method to translate window rect to FRect
    public static SDL.FRect GetWindowFRect(IntPtr targetHwnd, IntPtr overlayHwnd)
    {
        Win32Rect targetRect = GetVisualWindowBounds(targetHwnd);
        Win32Rect ovverlayRect = GetVisualWindowBounds(overlayHwnd);

        float localX = targetRect.Left - ovverlayRect.Left;
        float localY = targetRect.Top - ovverlayRect.Top;

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

    public static (IntPtr Window, IntPtr Renderer) CreateOverlay()
    {
        // create window
        var window = SDL.CreateWindow("Vellum", 0, 0, DefaultOverlayFlags);
        if (window == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create window! SDL_Error: {SDL.GetError()}");
        }
        
        MakeLayered(window);

        // create renderer
        var renderer = SDL.CreateRenderer(window, "software");
        if (renderer == IntPtr.Zero)
        {
            SDL.LogError(SDL.LogCategory.Application, $"SDL could not create renderer! SDL_Error: {SDL.GetError()}");
        }

        return (window, renderer);
    }

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
        WindowRects.AddRange(_openWindows);
        
        _isInitialized = true;
        return true;
    }
    
    public static bool ProcessEvents()
    {
        while (SDL.PollEvent(out var e))
        {
            if ((SDL.EventType)e.Type == SDL.EventType.Quit)
            {
                return false;
            }
        }
        return true;
    }
    
    public static void DebugWindows(nint window, nint renderer)
    {
        _openWindows = Window.GetWindowsHandles();
        var difference = _openWindows.Except(_openWindowsOld).ToList();
        foreach (var hwnd in difference)
        {
            Console.WriteLine($"found HWND: 0x{hwnd.ToString("X")} | Application: {Window.GetWindowTitle(hwnd)}");
            if (!WindowRects.Contains(hwnd))
            {
                WindowRects.Add(hwnd); 
            }
        }
        _openWindowsOld = _openWindows;
        WindowRects.RemoveAll(hwnd => !_openWindows.Contains(hwnd));
        
        SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
        foreach (IntPtr hwnd in WindowRects)
        {
            SDL.FRect drawBox = Window.GetWindowFRect(hwnd, window);
            SDL.RenderRect(renderer, drawBox);
        }
    }
}