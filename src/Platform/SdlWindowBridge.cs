namespace Vellum.Platform;

// wraps the native HWND in SDL without transferring window ownership to SDL
internal static class SdlWindowBridge
{
    internal static bool Initialize() => SDL.Init(SDL.InitFlags.Video);
    internal static bool IsInitialized => SDL.WasInit(SDL.InitFlags.Video) != 0;
    internal static string Error => SDL.GetError();
    internal static void Quit() => SDL.Quit();

    // set SDL window with properties
    internal static IntPtr Wrap(IntPtr hwnd, WindowFlags flags)
    {
        var properties = SDL.CreateProperties();
        if (properties == 0)
            throw new InvalidOperationException($"SDL_CreateProperties failed: {Error}");
        try
        {
            if (!SDL.SetBooleanProperty(properties, SDL.Props.WindowCreateHiddenBoolean, true) ||
                !SDL.SetBooleanProperty(properties, SDL.Props.WindowCreateFocusableBoolean, !flags.HasFlag(WindowFlags.NotFocusable)) ||
                !SDL.SetBooleanProperty(properties, SDL.Props.WindowCreateAlwaysOnTopBoolean,
                    flags.HasFlag(WindowFlags.AlwaysOnTop) || flags.HasFlag(WindowFlags.ForceTopmost)) ||
                !SDL.SetBooleanProperty(properties, SDL.Props.WindowCreateResizableBoolean, flags.HasFlag(WindowFlags.Resizable)) ||
                !SDL.SetBooleanProperty(properties, SDL.Props.WindowCreateTransparentBoolean, flags.HasFlag(WindowFlags.Transparent)) ||
                !SDL.SetPointerProperty(properties, SDL.Props.WindowCreateWin32HWNDPointer, hwnd))
                throw new InvalidOperationException($"Setting SDL renderer window properties failed: {Error}");

            var window = SDL.CreateWindowWithProperties(properties);
            if (window == IntPtr.Zero)
                throw new InvalidOperationException($"Wrapping HWND 0x{hwnd.ToInt64():X} for SDL rendering failed: {Error}");
            return window;
        }
        finally
        {
            SDL.DestroyProperties(properties);
        }
    }

    internal static IntPtr GetHwnd(IntPtr window) => window == IntPtr.Zero ? IntPtr.Zero :
        SDL.GetPointerProperty(SDL.GetWindowProperties(window), SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);

    internal static void PrepareToShow(IntPtr window, WindowFlags flags)
    {
        if (flags.HasFlag(WindowFlags.NotFocusable) &&
            (SDL.GetWindowFlags(window) & SDL.WindowFlags.Hidden) != 0 && !SDL.ShowWindow(window))
            throw new InvalidOperationException($"Synchronizing the renderer wrapper visibility failed: {Error}");
    }

    internal static void Destroy(IntPtr window) => SDL.DestroyWindow(window);

    // SDL may queue copies of native events while maintaining its renderer surface
    internal static void DiscardEvents() => SDL.FlushEvents(0, 0xFFFF);
}
