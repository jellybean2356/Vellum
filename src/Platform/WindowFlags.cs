namespace Vellum.Platform;

[Flags]
public enum WindowFlags
{
    None         = 0,
    Hidden       = 1 << 0,
    Resizable    = 1 << 1,
    Borderless   = 1 << 2,
    Fullscreen   = 1 << 3,
    AlwaysOnTop  = 1 << 4,
    Transparent  = 1 << 5,
    NotFocusable = 1 << 6, 

    // preset for overlays
    DefaultOverlay = Transparent | Borderless | Fullscreen | AlwaysOnTop | NotFocusable
}