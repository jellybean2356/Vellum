namespace Vellum.Platform;

using System.ComponentModel;
using System.Runtime.InteropServices;

internal static class Win32Window
{
    internal const string ClassName = "Vellum.NativeWindow";
    private static bool _registered;
    
    private static readonly NativeMethods.WndProc WindowProcDelegate =
        WindowProc;
    
    private static readonly IntPtr WindowProcPointer =
        Marshal.GetFunctionPointerForDelegate(WindowProcDelegate);


    // ============================================================
    // WINDOW CLASS REGISTRATION
    // ============================================================

    /// <summary>
    /// registers vellums native Win32 window class
    ///
    /// this must happen before CreateWindowEx can create
    /// a window using Vellum.NativeWindow
    /// </summary>
    internal static void RegisterClass()
    {
        // registering the same class repeatedly is unnecessary
        if (_registered)
            return;


        // get the HINSTANCE of the current executable.
        IntPtr instance = NativeMethods.GetModuleHandle(null);

        if (instance == IntPtr.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "Vellum failed to retrieve the current module handle."
            );
        }


        // WNDCLASSEX requires pointers for string fields
        // so allocate a temporary native UTF-16 class name
        IntPtr classNamePtr =
            Marshal.StringToHGlobalUni(ClassName);

        try
        {
            var windowClass = new NativeMethods.WndClassEx
            {
                cbSize =
                    (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),

                // redraw when the windows dimensions change
                style =
                    NativeMethods.CsHRedraw |
                    NativeMethods.CsVRedraw,
                
                lpfnWndProc =
                    WindowProcPointer,
                
                cbClsExtra = 0,
                cbWndExtra = 0,
                
                hInstance =
                    instance,
                
                hIcon = IntPtr.Zero,
                
                hCursor =
                    NativeMethods.LoadCursor(
                        IntPtr.Zero,
                        new IntPtr(NativeMethods.IdcArrow)
                    ),

                hbrBackground = IntPtr.Zero,
                
                lpszMenuName = IntPtr.Zero,

                lpszClassName =
                    classNamePtr,
                
                hIconSm = IntPtr.Zero
            };


            // register the class with Windows.
            ushort atom =
                NativeMethods.RegisterClassEx(
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
            // RegisterClassEx has already copied/processed the
            // class information, so the temporary string can go
            Marshal.FreeHGlobal(classNamePtr);
        }
    }


    // ============================================================
    // NATIVE WINDOW PROCEDURE
    // ============================================================

    /// <summary>
    /// receives messages sent by windows to vellum windows
    ///
    /// we currently handle nothing ourselves and forward every
    /// message to windows default implementation
    /// </summary>
    private static IntPtr WindowProc(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam)
    {
        return NativeMethods.DefWindowProc(
            hWnd,
            message,
            wParam,
            lParam
        );
    }
}