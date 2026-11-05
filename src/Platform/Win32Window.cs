namespace Vellum.Platform;

// NOTE: AI USED INSIDE NATIVE METHODS AND MOST OF THE STUFF RELATED TO WIN32 API

using System.ComponentModel;
using System.Runtime.InteropServices;

internal static class Win32Window
{
    internal const string ClassName = "Vellum.NativeWindow";
    private static bool _registered;
    private static readonly HashSet<IntPtr> ManagedWindows = [];
    private static readonly HashSet<IntPtr> ClickThroughWindows = [];
    private const nuint ResizeTimerId = 0x56454C4C; // separate timer from SDL
    internal static Action<IntPtr> RedrawRequested;
    private static readonly HashSet<IntPtr> CloseRequests = [];
    private static readonly Dictionary<IntPtr, IntPtr> SdlMessageHandlers = [];
    private static Exception _callbackFailure;
    private static readonly WndProc OuterWindowProcDelegate = OuterWindowProc;
    private static readonly IntPtr OuterWindowProcPointer = Marshal.GetFunctionPointerForDelegate(OuterWindowProcDelegate);
    
    private static readonly WndProc WindowProcDelegate =
        WindowProc;
    
    private static readonly IntPtr WindowProcPointer =
        Marshal.GetFunctionPointerForDelegate(WindowProcDelegate);
    
    // registers vellums native Win32 window class
    // this must happen before CreateWindowEx can create a window using Vellum.NativeWindow
    
    internal static void RegisterClass()
    {
        // registering the same class repeatedly is unnecessary
        if (_registered)
            return;


        // get the HINSTANCE of the current executable.
        var instance =  GetModuleHandle(null);

        if (instance == IntPtr.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "Vellum failed to retrieve the current module handle."
            );
        }


        // WNDCLASSEX requires pointers for string fields so allocate a temporary native UTF-16 class name
        var classNamePtr =
            Marshal.StringToHGlobalUni(ClassName);

        try
        {
            var windowClass = new WndClassEx
            {
                cbSize =
                    (uint)Marshal.SizeOf<WndClassEx>(),

                // redraw when the windows dimensions change
                style =
                     CsHRedraw |
                     CsVRedraw,
                
                lpfnWndProc =
                    WindowProcPointer,
                
                cbClsExtra = 0,
                cbWndExtra = 0,
                
                hInstance =
                    instance,
                
                hIcon = IntPtr.Zero,
                
                hCursor =
                     LoadCursor(
                        IntPtr.Zero,
                        new IntPtr( IdcArrow)
                    ),

                hbrBackground = IntPtr.Zero,
                
                lpszMenuName = IntPtr.Zero,

                lpszClassName =
                    classNamePtr,
                
                hIconSm = IntPtr.Zero
            };


            // register the class with Windows
            var atom =
                 RegisterClassEx(
                    ref windowClass
                );


            // zero means registration failed
            if (atom == 0)
            {
                throw new Win32Exception(
                    Marshal.GetLastPInvokeError(),
                    "Vellum failed to register its native window class."
                );
            }


            _registered = true;
        }
        finally
        {
            // RegisterClassEx has already copied/processed the class information, so the temporary string can go
            Marshal.FreeHGlobal(classNamePtr);
        }
    }


    // create hidden so wrapping/render initialization cannot flash a blank window
    // dimensions are client pixels matching Window.SetSize and SDL creation
    
    internal static IntPtr Create(string title, int width, int height, WindowFlags flags,
        WindowType type, Win32Rect? monitorBounds = null)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        const WindowFlags supported = WindowFlags.Hidden | WindowFlags.Resizable | WindowFlags.Borderless |
            WindowFlags.Fullscreen | WindowFlags.AlwaysOnTop | WindowFlags.Transparent | WindowFlags.NotFocusable |
            WindowFlags.ForceTopmost;
        if ((flags & ~supported) != 0)
            throw new ArgumentOutOfRangeException(nameof(flags), "Unknown window flags.");
        
        // make flags work
        RegisterClass();
        var style = flags.HasFlag(WindowFlags.Borderless) || flags.HasFlag(WindowFlags.Fullscreen)
            ? WsPopup : WsOverlappedWindow;
        if (flags.HasFlag(WindowFlags.Borderless) && flags.HasFlag(WindowFlags.Resizable) && !flags.HasFlag(WindowFlags.Fullscreen))
            style |= WsThickFrame;
        if (!flags.HasFlag(WindowFlags.Resizable))
            style &= ~(WsThickFrame | WsMaximizeBox);
        style |= WsClipChildren | WsClipSiblings;
        uint exStyle = 0;
        if (flags.HasFlag(WindowFlags.AlwaysOnTop) || flags.HasFlag(WindowFlags.ForceTopmost))
            exStyle |= WsExTopmost;
        if (flags.HasFlag(WindowFlags.Transparent)) exStyle |= (uint)WsExLayered;
        if (flags.HasFlag(WindowFlags.NotFocusable)) exStyle |= (uint)WsExNoActivate;
        if (type == WindowType.Overlay) exStyle |= (uint)(WsExToolWindow | WsExTransparent);

        // other window configuration
        int x = CwUseDefault, y = CwUseDefault;
        var outer = new Win32Rect { Right = width, Bottom = height };
        if (monitorBounds.HasValue)
        {
            var bounds = monitorBounds.Value;
            x = bounds.Left; y = bounds.Top;
            width = bounds.Right - bounds.Left; height = bounds.Bottom - bounds.Top;
        }
        else
        {
            Check(AdjustWindowRectExForDpi(ref outer, style, false, exStyle,
                GetDpiForSystem()), "AdjustWindowRectExForDpi");
            width = checked(outer.Right - outer.Left); height = checked(outer.Bottom - outer.Top);
        }
        var instance = GetModuleHandle(null);
        if (instance == IntPtr.Zero) Check(false, "GetModuleHandleW");
        var hwnd = CreateWindowEx(exStyle, ClassName, title, style, x, y,
            width, height, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
        if (hwnd == IntPtr.Zero) Check(false, $"CreateWindowExW for '{title}'");
        return hwnd;
    }

    // helper function to see if something went wrong
    internal static void Check(bool success, string operation)
    {
        if (success) return;
        var error = Marshal.GetLastPInvokeError();
        throw new Win32Exception(error, $"{operation} failed: {new Win32Exception(error).Message}");
    }

    // change window position
    internal static void SetPosition(IntPtr hwnd, int x, int y) => Check(
        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            SwpNoSize | SwpNoZOrder | SwpNoActivate), "SetWindowPos (position)");

    // making window above other fullscreen windows
    internal static void EnsureTopmost(IntPtr hwnd) => Check(
         SetWindowPos(hwnd,  HwndTopmost, 0, 0, 0, 0,
             SwpNoSize |  SwpNoMove |  SwpNoActivate),
        "SetWindowPos (topmost)");

    // obtain monitor refresh rate
    internal static int GetRefreshRate(IntPtr hwnd)
    {
        var dc = GetDc(hwnd);
        if (dc == IntPtr.Zero) return 0;

        try
        {
            var refreshRate = GetDeviceCaps(dc, VRefresh);
            return refreshRate > 1 ? refreshRate : 0;
        }
        finally
        {
            ReleaseDc(hwnd, dc);
        }
    }

    // change window size
    internal static void SetSize(IntPtr hwnd, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var style = unchecked((uint)GetWindowLongPtr(hwnd, GwlStyle).ToInt64());
        var exStyle = unchecked((uint)GetWindowLongPtr(hwnd, GwlExStyle).ToInt64());
        var rect = new Win32Rect { Right = width, Bottom = height };
        if ((style & WsPopup) == 0)
            Check(AdjustWindowRectExForDpi(ref rect, style, false, exStyle,
                GetDpiForWindow(hwnd)), "AdjustWindowRectExForDpi");
        Check(SetWindowPos(hwnd, IntPtr.Zero, 0, 0, checked(rect.Right - rect.Left),
            checked(rect.Bottom - rect.Top), SwpNoMove | SwpNoZOrder | SwpNoActivate),
            "SetWindowPos (size)");
    }

    // apply extended style for window expanted window manipulation
    internal static void SetExtendedStyle(IntPtr hwnd, long style)
    {
        var previous =  SetWindowLongPtr(hwnd,  GwlExStyle, new IntPtr(style));
        if (previous == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
            Check(false, "SetWindowLongPtrW");
        Check( SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
             SwpNoSize |  SwpNoMove |  SwpNoZOrder |
             SwpNoActivate |  SwpFrameChanged), "SetWindowPos (styles)");
    }

    // make window clicktrough
    internal static void SetClickThroughState(IntPtr hwnd, bool enabled)
    {
        if (enabled) ClickThroughWindows.Add(hwnd);
        else ClickThroughWindows.Remove(hwnd);
    }
    
    // processes pending messages on the thread that created the window
    // returns false on WM_QUIT otherwise returns without waiting
    internal static bool ProcessMessages()
    {
        // a null HWND includes every window and thread message on this thread
        while ( PeekMessage(
                   out var message,
                   IntPtr.Zero,
                   0,
                   0,
                    PmRemove))
        {
            // WM_QUIT belongs to the thread so never dispatch it to a window
            if (message.message ==  WmQuit)
                return false;

            TranslateMessage(in message);
            DispatchMessage(in message);
        }

        if (_callbackFailure == null) return true;
        
        var error = _callbackFailure;
        _callbackFailure = null;
        throw new InvalidOperationException("Processing a native window message failed.", error);
    }

    // engine closes integrated windows after returning from native dispatch
    // keep their HWND alive until their renderer and SDL wrapper are disposed
    internal static void TrackWindow(IntPtr hwnd) => ManagedWindows.Add(hwnd);
    internal static bool IsCloseRequested(IntPtr hwnd) => CloseRequests.Contains(hwnd);

    internal static void AttachMessageHandler(IntPtr hwnd)
    {
        var sdlProcedure =  GetWindowLongPtr(hwnd,  GwlWndProc);
        if (sdlProcedure == IntPtr.Zero) Check(false, "GetWindowLongPtrW (SDL procedure)");
        var previous =  SetWindowLongPtr(hwnd,  GwlWndProc, OuterWindowProcPointer);
        if (previous == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
            Check(false, "SetWindowLongPtrW (native procedure)");
        SdlMessageHandlers.Add(hwnd, sdlProcedure);
    }

    internal static void DetachMessageHandler(IntPtr hwnd)
    {
        if (!SdlMessageHandlers.TryGetValue(hwnd, out var procedure)) return;
        if ( IsWindow(hwnd))
        {
            var previous =  SetWindowLongPtr(hwnd,  GwlWndProc, procedure);
            if (previous == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
                Check(false, "SetWindowLongPtrW (restore SDL procedure)");
        }
        SdlMessageHandlers.Remove(hwnd);
    }

    private static IntPtr OuterWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            // SDL fixes the min/max size of windows without SDL_WINDOW_RESIZABLE
            // let Win32 handle tracking limits including programmatic SetWindowPos
            if (message == WmGetMinMaxInfo)
                return  DefWindowProc(hwnd, message, wParam, lParam);
            var exStyle = unchecked((uint) GetWindowLongPtr(hwnd,  GwlExStyle).ToInt64());
            if (message == WmMouseActivate &&
                (ClickThroughWindows.Contains(hwnd) || (exStyle & (uint) WsExNoActivate) != 0))
                return new IntPtr(MaNoActivate);
            if (message == WmNcHitTest && ClickThroughWindows.Contains(hwnd))
                return new IntPtr(HtTransparent);
            var style = unchecked((uint)GetWindowLongPtr(hwnd,  GwlStyle).ToInt64());
            if (message == WmNcCalcSize && (style & WsPopup) != 0)
                return IntPtr.Zero; // entire proposed rectangle is client area, SDL must notd pin it to its old size
            if (message == WmNcHitTest && (style & (WsPopup | WsThickFrame)) ==
                (WsPopup | WsThickFrame))
            {
                Check(GetWindowRect(hwnd, out Win32Rect rect), "GetWindowRect (resize hit test)");
                int x = unchecked((short)(lParam.ToInt64() & 0xFFFF));
                int y = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
                var edge = Math.Max(1, (int)(8 *  GetDpiForWindow(hwnd) / 96));
                bool left = x >= rect.Left && x < rect.Left + edge, right = x < rect.Right && x >= rect.Right - edge;
                bool top = y >= rect.Top && y < rect.Top + edge, bottom = y < rect.Bottom && y >= rect.Bottom - edge;
                var hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
                if (hit != 0) return new IntPtr(hit);
            }
            if (message == WmClose)
            {
                CloseRequests.Add(hwnd);
                return IntPtr.Zero;
            }
            if (message == WmEnterSizeMove)
            {
                if ( SetTimer(hwnd, ResizeTimerId, 16, IntPtr.Zero) == 0)
                    Check(false, "SetTimer (live resize)");
            }
            else if (message == WmExitSizeMove)
                 KillTimer(hwnd, ResizeTimerId);
            else if (message == WmTimer && unchecked((nuint)wParam) == ResizeTimerId)
            {
                RequestRedraw(hwnd);
                return IntPtr.Zero;
            }

            // let SDL update its renderer surface dimensions before Vellum draws
            var result = SdlMessageHandlers.TryGetValue(hwnd, out var procedure)
                ?  CallWindowProc(procedure, hwnd, message, wParam, lParam)
                :  DefWindowProc(hwnd, message, wParam, lParam);
            if (message is  WmPaint or  WmSize or  WmExitSizeMove)
                RequestRedraw(hwnd);
            return result;
        }
        catch (Exception error)
        {
            _callbackFailure ??= error;
            return IntPtr.Zero;
        }
    }

    private static void RequestRedraw(IntPtr hwnd)
    {
        if (_callbackFailure == null) RedrawRequested?.Invoke(hwnd);
    }

    internal static void Destroy(IntPtr hwnd)
    {
         KillTimer(hwnd, ResizeTimerId);
        if ( IsWindow(hwnd) && ! DestroyWindow(hwnd))
        {
            var error = Marshal.GetLastPInvokeError();
            throw new Win32Exception(error,
                $"DestroyWindow failed for HWND 0x{hwnd.ToInt64():X}: {new Win32Exception(error).Message}");
        }
        ManagedWindows.Remove(hwnd);
        ClickThroughWindows.Remove(hwnd);
        CloseRequests.Remove(hwnd);
        SdlMessageHandlers.Remove(hwnd);
    }
    
    // receives messages sent by windows to Vellum windows
    // handles close and destroy, forwarding other messages to windows default implementation
    private static IntPtr WindowProc(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam)
    {
        try
        {
            return HandleMessage(hWnd, message, wParam, lParam);
        }
        catch (Exception error)
        {
            // never unwind a managed exception through a native window callback
            _callbackFailure ??= error;
            return IntPtr.Zero;
        }
    }

    private static IntPtr HandleMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case  WmClose:
                if (ManagedWindows.Contains(hWnd))
                {
                    CloseRequests.Add(hWnd);
                    return IntPtr.Zero;
                }
                DestroyWindow(hWnd);
                return IntPtr.Zero;

            case  WmDestroy:
                // engine manages integrated windows without quitting the thread when one of several windows is closed.
                if (!ManagedWindows.Contains(hWnd))
                     PostQuitMessage(0);
                else
                    CloseRequests.Add(hWnd);
                return IntPtr.Zero;

            case  WmDpiChanged:
                var dpiWindow = Engine.Windows.FirstOrDefault(w => w.Hwnd == hWnd);
                if (dpiWindow != null && dpiWindow.Flags.HasFlag(WindowFlags.Fullscreen))
                {
                    dpiWindow.FitMonitor();
                    return IntPtr.Zero;
                }

                if (lParam == IntPtr.Zero) return IntPtr.Zero;
                var rect = Marshal.PtrToStructure<Win32Rect>(lParam);
                Check(SetWindowPos(hWnd, IntPtr.Zero, rect.Left, rect.Top,
                    rect.Right - rect.Left, rect.Bottom - rect.Top,
                    SwpNoZOrder | SwpNoActivate), "SetWindowPos (DPI)");
                return IntPtr.Zero;

            case  WmDisplayChange:
                var window = Engine.Windows.FirstOrDefault(w => w.Hwnd == hWnd);
                if (window != null && window.Flags.HasFlag(WindowFlags.Fullscreen))
                    window.FitMonitor();
                break;
        }

        return  DefWindowProc(
            hWnd,
            message,
            wParam,
            lParam
        );
    }
}
