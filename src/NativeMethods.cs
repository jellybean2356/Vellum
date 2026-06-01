using System.Runtime.InteropServices;

namespace Vellum;

public partial class NativeMethods
{
        // constants
        public const int GwlExstyle = -20; // extended window style
        public const uint GwOwner = 3;
        public const uint DwmwaIsCloaked = 14;
        public const long WsExLayered = 0x00080000; // layered window
        public const long WsExTransparent = 0x00000020; // transparent window
        public const long WsExToolWindow = 0x00000080; // hides the window icon from toolbar
        
        public const uint DwmwaExtendedFrameBounds = 9;
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        // importing user32.dll functions
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
            
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial void EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindowVisible(IntPtr hWnd);
        
        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial void GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
        public static partial int GetWindowTextLength(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        public static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        
        [LibraryImport("dwmapi.dll")]
        public static partial int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out Win32Rect pvAttribute, int cbAttribute);
        
        [LibraryImport("dwmapi.dll")]
        public static partial int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out int pvAttribute, int cbAttribute);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Point { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Rect { public int Left; public int Top; public int Right; public int Bottom; }
        
        [LibraryImport("user32.dll")]
        public static partial IntPtr MonitorFromPoint(Win32Point pt, uint dwFlags);

        [LibraryImport("user32.dll")]
        public static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetWindowRect(IntPtr hWnd, out Win32Rect lpRect);
}