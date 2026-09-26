namespace Vellum.Platform;

internal static partial class NativeMethods
{
    // ============================================================
    // WINDOW INDEXES
    // ============================================================

    /// <summary>
    /// index used with GetWindowLongPtr / SetWindowLongPtr
    /// to access the extended window style
    /// </summary>
    internal const int GwlExStyle = -20;


    // ============================================================
    // WINDOW STYLES
    // ============================================================

    /// <summary>
    /// standard overlapped desktop window
    /// title bar, border, minimize/maximize buttons, etc.
    /// </summary>
    internal const uint WsOverlappedWindow = 0x00CF0000;


    // ============================================================
    // EXTENDED WINDOW STYLES
    // ============================================================

    /// <summary>
    /// enables a layered window
    /// </summary>
    internal const long WsExLayered = 0x00080000;

    /// <summary>
    /// enables clicktrought
    /// </summary>
    internal const long WsExTransparent = 0x00000020;

    /// <summary>
    /// hides window from the taskbar
    /// </summary>
    internal const long WsExToolWindow = 0x00000080;

    /// <summary>
    /// prevents windows from allocating a redirection surface
    /// for the window
    /// </summary>
    internal const long WsExNoRedirectionBitmap = 0x00400000;

    /// <summary>
    /// prevents the window from becoming activated/focused
    /// </summary>
    internal const long WsExNoActivate = 0x08000000;


    // ============================================================
    // WINDOW CLASS STYLES
    // ============================================================

    /// <summary>
    /// redraw the entire window when its width changes
    /// </summary>
    internal const uint CsHRedraw = 0x0002;

    /// <summary>
    /// redraw the entire window when its height changes
    /// </summary>
    internal const uint CsVRedraw = 0x0001;


    // ============================================================
    // WINDOW MESSAGES
    // ============================================================

    /// <summary>
    /// sent when the user requests that a window closes
    /// </summary>
    internal const uint WmClose = 0x0010;

    /// <summary>
    /// sent when a window is being destroyed
    /// </summary>
    internal const uint WmDestroy = 0x0002;


    // ============================================================
    // WINDOW CREATION / DISPLAY CONSTANTS
    // ============================================================

    /// <summary>
    /// lets windows choose the default position or size
    /// </summary>
    internal const int CwUseDefault = unchecked((int)0x80000000);

    /// <summary>
    /// activates and displays the window using its current size
    /// </summary>
    internal const int SwShow = 5;


    // ============================================================
    // WINDOW RELATION CONSTANTS
    // ============================================================

    /// <summary>
    /// used with GetWindow to retrieve a windows owner
    /// </summary>
    internal const uint GwOwner = 3;


    // ============================================================
    // CURSOR CONSTANTS
    // ============================================================

    /// <summary>
    /// standard Windows arrow cursor
    /// </summary>
    internal const int IdcArrow = 32512;


    // ============================================================
    // DWM CONSTANTS
    // ============================================================

    /// <summary>
    /// retrieves whether a window has been cloaked by DWM
    /// </summary>
    internal const uint DwmwaIsCloaked = 14;

    /// <summary>
    /// retrieves the visible window frame bounds
    /// </summary>
    internal const uint DwmwaExtendedFrameBounds = 9;

    internal const int DwmwaNcRenderingPolicy = 2;
    internal const int DwmNcRenderingPolicyEnabled = 2;


    // ============================================================
    // LAYERED WINDOW CONSTANTS
    // ============================================================

    /// <summary>
    /// SetLayeredWindowAttributes uses the alpha value
    /// </summary>
    internal const uint LwaAlpha = 0x00000002;


    // ============================================================
    // NATIVE CALLBACK DELEGATES
    // ============================================================

    /// <summary>
    /// callback used by EnumWindows
    /// returning false stops enumeration
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate bool EnumWindowsProc(
        IntPtr hWnd,
        IntPtr lParam
    );

    /// <summary>
    /// allback used as the native Win32 window procedure
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IntPtr WndProc(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam
    );


    // ============================================================
    // NATIVE STRUCTS
    // ============================================================

    /// <summary>
    /// native Win32 POINT structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
        internal int X;
        internal int Y;
    }

    /// <summary>
    /// native Win32 RECT structure
    /// stores left/top/right/bottom instead of x/y/width/height
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    /// <summary>
    /// native DWM MARGINS structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Margins
    {
        internal int Left;
        internal int Right;
        internal int Top;
        internal int Bottom;
    }

    /// <summary>
    /// native WNDCLASSEXW structure used when registering
    /// vellums Win32 window class
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WndClassEx
    {
        internal uint cbSize;
        internal uint style;

        // pointer to the Win32 WindowProc callback.
        internal IntPtr lpfnWndProc;

        internal int cbClsExtra;
        internal int cbWndExtra;

        internal IntPtr hInstance;
        internal IntPtr hIcon;
        internal IntPtr hCursor;
        internal IntPtr hbrBackground;

        internal IntPtr lpszMenuName;
        internal IntPtr lpszClassName;

        internal IntPtr hIconSm;
    }


    // ============================================================
    // KERNEL32
    // ============================================================

    /// <summary>
    /// retrieves the module handle for the current process/module
    /// </summary>
    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetModuleHandleW",
        StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    internal static partial IntPtr GetModuleHandle(
        string moduleName
    );


    // ============================================================
    // USER32 - WINDOW CLASS / CREATION
    // ============================================================

    /// <summary>
    /// registers a Win32 window class
    /// </summary>
    [LibraryImport(
        "user32.dll",
        EntryPoint = "RegisterClassExW",
        SetLastError = true)]
    internal static partial ushort RegisterClassEx(
        ref WndClassEx wndClass
    );

    /// <summary>
    /// creates a native Win32 window and returns its HWND
    /// </summary>
    [LibraryImport(
        "user32.dll",
        EntryPoint = "CreateWindowExW",
        StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    internal static partial IntPtr CreateWindowEx(
        uint exStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr param
    );

    /// <summary>
    /// provides the default windows implementation for messages
    /// </summary>
    [LibraryImport(
        "user32.dll",
        EntryPoint = "DefWindowProcW")]
    internal static partial IntPtr DefWindowProc(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam
    );


    // ============================================================
    // USER32 - WINDOW LIFETIME / VISIBILITY
    // ============================================================

    /// <summary>
    /// displays or changes the visibility state of a window
    /// </summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(
        IntPtr hWnd,
        int command
    );

    /// <summary>
    /// destroys a native Win32 window
    /// </summary>
    [LibraryImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(
        IntPtr hWnd
    );

    /// <summary>
    /// posts WM_QUIT to the current threads message queue
    /// </summary>
    [LibraryImport("user32.dll")]
    internal static partial void PostQuitMessage(
        int exitCode
    );


    // ============================================================
    // USER32 - WINDOW INFORMATION
    // ============================================================

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetWindowLongPtrW")]
    internal static partial IntPtr GetWindowLongPtr(
        IntPtr hWnd,
        int index
    );

    [LibraryImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtrW")]
    internal static partial IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int index,
        IntPtr newValue
    );

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetWindow(
        IntPtr hWnd,
        uint command
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(
        IntPtr hWnd
    );

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetWindowTextLengthW")]
    internal static partial int GetWindowTextLength(
        IntPtr hWnd
    );

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetWindowTextW",
        StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int GetWindowText(
        IntPtr hWnd,
        char[] buffer,
        int maxCount
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(
        IntPtr hWnd,
        out Win32Rect rect
    );


    // ============================================================
    // USER32 - WINDOW POSITION
    // ============================================================

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags
    );


    // ============================================================
    // USER32 - WINDOW ENUMERATION
    // ============================================================

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(
        EnumWindowsProc callback,
        IntPtr lParam
    );


    // ============================================================
    // USER32 - CURSOR
    // ============================================================

    [LibraryImport(
        "user32.dll",
        EntryPoint = "LoadCursorW",
        SetLastError = true)]
    internal static partial IntPtr LoadCursor(
        IntPtr instance,
        IntPtr cursorName
    );


    // ============================================================
    // USER32 - INPUT
    // ============================================================

    /// <summary>
    /// reads the current asynchronous state of a virtual key
    /// </summary>
    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(
        int virtualKey
    );


    // ============================================================
    // USER32 - MONITORS
    // ============================================================

    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromPoint(
        Win32Point point,
        uint flags
    );

    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromWindow(
        IntPtr hWnd,
        uint flags
    );


    // ============================================================
    // USER32 - LAYERED WINDOWS
    // ============================================================

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetLayeredWindowAttributes(
        IntPtr hWnd,
        uint colorKey,
        byte alpha,
        uint flags
    );


    // ============================================================
    // DWMAPI
    // ============================================================

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(
        IntPtr hWnd,
        uint attribute,
        out int value,
        int valueSize
    );

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(
        IntPtr hWnd,
        uint attribute,
        out Win32Rect rect,
        int valueSize
    );

    [LibraryImport(
        "dwmapi.dll",
        EntryPoint = "DwmExtendFrameIntoClientArea")]
    private static partial int DwmExtendFrameIntoClientAreaNative(
        IntPtr hWnd,
        ref Margins margins
    );


    // ============================================================
    // VELLUM HELPERS
    // ============================================================

    /// <summary>
    /// helper which converts a native Win32 RECT
    /// into vellums rect type
    /// </summary>
    internal static bool GetWindowRect(
        IntPtr hWnd,
        out Rect rect
    )
    {
        var success = GetWindowRect(
            hWnd,
            out Win32Rect nativeRect
        );

        rect = Rect.FromWin32Rect(nativeRect);

        return success;
    }

    /// <summary>
    /// helper which converts a DWM Win32 RECT
    /// into vellums rect type
    /// </summary>
    internal static int DwmGetWindowAttribute(
        IntPtr hWnd,
        uint attribute,
        out Rect rect,
        int valueSize
    )
    {
        var result = DwmGetWindowAttribute(
            hWnd,
            attribute,
            out Win32Rect nativeRect,
            valueSize
        );

        rect = Rect.FromWin32Rect(nativeRect);

        return result;
    }

    /// <summary>
    /// vellum friendly wrapper around DwmExtendFrameIntoClientArea
    /// </summary>
    internal static int DwmExtendFrameIntoClientArea(
        IntPtr hWnd,
        Rect rect
    )
    {
        var margins = new Margins
        {
            Left = (int)rect.X,
            Right = (int)rect.W,
            Top = (int)rect.Y,
            Bottom = (int)rect.H
        };

        return DwmExtendFrameIntoClientAreaNative(
            hWnd,
            ref margins
        );
    }
}