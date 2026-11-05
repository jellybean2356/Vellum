namespace Vellum.Platform;

// NOTE: AI USED INSIDE NATIVE METHODS AND MOST OF THE STUFF RELATED TO WIN32 API

// additional window interop, existing NativeMethods names stay compatible
internal static partial class NativeMethods
{
    internal const int GwlStyle = -16;
    internal const int GwlWndProc = -4;
    internal const uint WsPopup = 0x80000000;
    internal const uint WsThickFrame = 0x00040000;
    internal const uint WsMaximizeBox = 0x00010000;
    internal const uint WsClipChildren = 0x02000000;
    internal const uint WsClipSiblings = 0x04000000;
    internal const uint WsExTopmost = 0x00000008;
    internal const int SwHide = 0, SwMaximize = 3, SwMinimize = 6, SwShowNoActivate = 4, SwRestore = 9;
    internal const uint SwpNoSize = 1, SwpNoMove = 2, SwpNoZOrder = 4, SwpNoActivate = 0x10, SwpFrameChanged = 0x20;
    internal const uint WmDpiChanged = 0x02E0, WmDisplayChange = 0x007E, WmPrint = 0x0317;
    internal const uint WmMouseActivate = 0x0021;
    internal const uint WmGetMinMaxInfo = 0x0024;
    internal const uint WmNcCalcSize = 0x0083, WmNcHitTest = 0x0084;
    internal const uint WmPaint = 0x000F, WmSize = 0x0005, WmTimer = 0x0113;
    internal const uint WmEnterSizeMove = 0x0231, WmExitSizeMove = 0x0232;
    internal const uint PrfNonClient = 2, UlwAlpha = 2;
    internal const int VRefresh = 116;
    internal const int HtTransparent = -1, MaNoActivate = 3;
    internal static readonly IntPtr HwndTopmost = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Size { internal int Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        internal uint Size;
        internal Win32Rect Monitor, Work;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BitmapInfo
    {
        internal uint Size;
        internal int Width, Height;
        internal ushort Planes, BitCount;
        internal uint Compression, SizeImage;
        internal int XPelsPerMeter, YPelsPerMeter;
        internal uint ClrUsed, ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct BlendFunction
    {
        internal byte Operation, Flags, ConstantAlpha, AlphaFormat;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, ref Win32Rect rect, IntPtr data);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(IntPtr hwnd, out Win32Rect rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AdjustWindowRectExForDpi(ref Win32Rect rect, uint style,
        [MarshalAs(UnmanagedType.Bool)] bool menu, uint exStyle, uint dpi);

    [LibraryImport("user32.dll")]
    internal static partial uint GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    internal static partial uint GetDpiForSystem();

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowText(IntPtr hwnd, string title);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out Win32Point point);

    [DllImport("user32.dll", EntryPoint = "GetDC", SetLastError = true)]
    internal static extern IntPtr GetDc(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "ReleaseDC", SetLastError = true)]
    internal static extern int ReleaseDc(IntPtr hwnd, IntPtr dc);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ScreenToClient(IntPtr hwnd, ref Win32Point point);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ClientToScreen(IntPtr hwnd, ref Win32Point point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(IntPtr hwnd);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static partial IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcW")]
    internal static partial IntPtr CallWindowProc(IntPtr procedure, IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nuint SetTimer(IntPtr hwnd, nuint id, uint interval, IntPtr callback);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool KillTimer(IntPtr hwnd, nuint id);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destinationDc, in Win32Point destination,
        in Win32Size size, IntPtr sourceDc, in Win32Point source, uint colorKey, in BlendFunction blend, uint flags);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
    internal static extern IntPtr CreateCompatibleDc(IntPtr dc);

    [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps")]
    internal static extern int GetDeviceCaps(IntPtr dc, int index);

    [DllImport("gdi32.dll", EntryPoint = "CreateDIBSection", SetLastError = true)]
    internal static extern IntPtr CreateDibSection(IntPtr dc, in BitmapInfo info, uint usage,
        out IntPtr pixels, IntPtr section, uint offset);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial IntPtr SelectObject(IntPtr dc, IntPtr value);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(IntPtr value);

    [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteDc(IntPtr dc);
}
