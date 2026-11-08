# Vellum.Platform

### `Window`

`Window` represents a Vellum window. It owns a native Win32 `HWND`, an SDL wrapper, and a `Renderer`.

Standard windows and overlay windows cannot be mixed in the same engine instance. Create either standard windows or overlays for a given run.

#### Properties

| Member | Type | Description |
| --- | --- | --- |
| `Handle` | `IntPtr` | SDL's wrapped window handle. This is not the native `HWND`. |
| `Hwnd` | `IntPtr` | The native Win32 window handle. |
| `Type` | `WindowType` | Whether this is a standard window or an overlay. |
| `Title` | `string` | Current native window title. |
| `IsValid` | `bool` | `true` while the SDL and native window resources are alive. |
| `Flags` | `WindowFlags` | Flags used when the window was created. Read-only. |
| `HoverCount` | `int` | Number of interactive objects currently hovered in this window. Normally maintained by Vellum. |
| `Renderer` | `Renderer` | Renderer associated with this window. Assigning a new renderer disposes the old one. |

#### Creating windows

| Signature | Description |
| --- | --- |
| `static Window Create(string title, int width, int height, WindowFlags flags = WindowFlags.Resizable)` | Creates a standard native window. The default is a resizable window. |
| `static Window CreateOverlay(string title, uint displayId = 0)` | Creates a fullscreen overlay on the primary monitor by default. |
| `static Window CreateOverlay(string title, WindowFlags flags, uint displayId = 0)` | Creates an overlay using additional flags. `DefaultOverlay` is always added to the supplied flags. |

Call `engine.Initialize()` before creating a window. A window is automatically registered with the engine and gets a software `Renderer`.

#### Window control

| Signature | Description |
| --- | --- |
| `void Hide()` | Hides the window. |
| `void Show()` | Shows the window. Non-focusable windows are shown without activation. |
| `void Minimize()` | Minimizes the window. |
| `void Maximize()` | Maximizes a standard window. For a fullscreen window, restores its monitor-sized bounds and shows it. |
| `void Restore()` | Restores a minimized/maximized window. Fullscreen windows are fitted back to their monitor. |
| `void SetPosition(int x, int y)` | Sets the top-left position in desktop coordinates. |
| `void SetSize(int width, int height)` | Sets the window size in pixels. |
| `void SetTitle(string title)` | Changes the native title bar text. |
| `void MakeLayered()` | Enables per-pixel transparent layered presentation and click-through behavior. Useful for turning a normal window into an overlay-style window. |
| `void SetClickThrough(bool enabled)` | Controls whether mouse input passes through the window. |
| `void Dispose()` | Releases the renderer, SDL wrapper, and native window. Safe to call through `using`. |

### `WindowFlags`

`WindowFlags` is a `[Flags]` enum, so values can be combined with `|`.

| Value | Meaning |
| --- | --- |
| `None` | No special behavior. |
| `Hidden` | Create the window hidden. |
| `Resizable` | Allow resizing. |
| `Borderless` | Remove the normal title bar and frame. |
| `Fullscreen` | Size the window to a monitor. |
| `AlwaysOnTop` | Request normal topmost behavior. |
| `Transparent` | Enable transparent/layered presentation. |
| `NotFocusable` | Prevent the window from taking focus when shown. |
| `ForceTopmost` | Reapply topmost placement during updates. Useful when another desktop window repeatedly changes z-order. |
| `DefaultOverlay` | Preset combining `Transparent`, `Borderless`, `AlwaysOnTop`, `NotFocusable`, and `Fullscreen`. |

Example:

```csharp
using var window = Window.Create(
    "Tool window",
    500,
    300,
    WindowFlags.Borderless | WindowFlags.AlwaysOnTop);
```

### `WindowType`

| Value | Meaning |
| --- | --- |
| `Standard` | Ordinary native Vellum window. |
| `Overlay` | Overlay window created through `CreateOverlay`. |

### `WindowUtils`

Static helpers for inspecting desktop windows and monitors. These methods expose native handles as `IntPtr`; they do not create or own the returned handles.

| Signature | Description |
| --- | --- |
| `static IntPtr GetHwnd(IntPtr window)` | Gets the native `HWND` represented by an SDL window handle. |
| `static uint[] GetScreenDisplayIds()` | Returns Vellum's stable IDs for currently connected monitors. These are Vellum IDs, not SDL display IDs. |
| `static IntPtr[] GetMonitorHandles()` | Enumerates connected monitor handles. |
| `static List<IntPtr> GetWindowsHandles()` | Gets visible, titled top-level windows on the primary monitor, excluding tool windows, owned windows, cloaked windows, and Vellum's own main window. |
| `static string GetWindowTitle(IntPtr hWnd)` | Reads a native window title. Returns an empty string when there is no title. |
| `static Rect GetVisualWindowBounds(IntPtr hWnd)` | Gets the visible frame bounds of a native window, including extended frame margins when available. |
| `static Rect GetLocalWindowsBounds(IntPtr targetHwnd, IntPtr overlayHwnd)` | Converts a target window's desktop bounds into coordinates relative to an overlay client area. |
| `static void DrawDebugWindows(Window window, Renderer renderer)` | Draws red rectangles around tracked desktop windows and logs newly detected handles. This is a development/debug helper. |

Display IDs can be passed to `CreateOverlay`:

```csharp
var displays = WindowUtils.GetScreenDisplayIds();
var secondDisplay = displays.Length > 1 ? displays[1] : 0;

using var overlay = Window.CreateOverlay("Overlay", secondDisplay);
```
