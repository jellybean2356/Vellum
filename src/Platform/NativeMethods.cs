using System.Runtime.InteropServices;
using Vellum.Geometry;

namespace Vellum.Platform;

internal partial class NativeMethods
{
        // constants
        internal const int GwlExstyle = -20; // extended window style
        internal const uint GwOwner = 3;
        internal const uint DwmwaIsCloaked = 14;
        internal const long WsExLayered = 0x00080000; // layered window
        internal const long WsExTransparent = 0x00000020; // transparent window
        internal const long WsExToolWindow = 0x00000080; // hides the window icon from toolbar
        internal const uint DwmwaExtendedFrameBounds = 9;
        
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // importing user32.dll functions
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        internal static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
            
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        internal static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial void EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindowVisible(IntPtr hWnd);
        
        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
        internal static partial int GetWindowTextLength(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out int pvAttribute, int cbAttribute);

        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out Win32Rect pvAttribute, int cbAttribute);
        
        [LibraryImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea")]
        private static partial int DwmExtendFrameIntoClientAreaNative(IntPtr hWnd, ref Margins pMarInset);
        
        [LibraryImport("user32.dll")]
        internal static partial IntPtr MonitorFromPoint(Win32Point pt, uint dwFlags);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        [LibraryImport("user32.dll")]
        internal static partial short GetAsyncKeyState(int vKey);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetWindowRect(IntPtr hWnd, out Win32Rect lpRect);
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { internal int X; internal int Y; }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Rect { internal int Left; internal int Top; internal int Right; internal int Bottom; }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct Margins { public int Left; public int Right; public int Top; public int Bottom; }
        
        internal static int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out Rect pvAttribute, int cbAttribute)
        {
                var hr = DwmGetWindowAttribute(hwnd, dwAttribute, out Win32Rect nativeRect, cbAttribute);
                pvAttribute = Rect.FromWin32Rect(nativeRect);
                return hr;
        }

        internal static bool GetWindowRect(IntPtr hWnd, out Rect lpRect)
        {
                var result = GetWindowRect(hWnd, out Win32Rect nativeRect);
                lpRect = Rect.FromWin32Rect(nativeRect);
                return result;
        }
        
        internal static int DwmExtendFrameIntoClientArea(IntPtr hWnd, Rect rect)
        {
                var nativeMargins = new Margins
                {
                        Left = (int)rect.X,
                        Right = (int)rect.W,
                        Top = (int)rect.Y,
                        Bottom = (int)rect.H
                };
    
                return DwmExtendFrameIntoClientAreaNative(hWnd, ref nativeMargins);
        }
}