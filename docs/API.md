# Vellum public API

This is the user-facing reference for the current Vellum DLL. It documents public types and members that can be used from another project.

The implementation has more code than this: Win32 P/Invoke declarations, SDL window bridging, native callbacks, and layered-window helpers are intentionally internal and are not part of this reference.

All examples assume these namespaces when needed:

```csharp
using Vellum.Core;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Input;
using Vellum.Platform;
using Vellum.UI;
```

## the normal lifecycle

Most applications follow this order:

```csharp
using var engine = new Engine();

if (!engine.Initialize())
    return;

using var window = Window.Create("My window", 800, 450);
using var rectangle = new Rect(40, 40, 180, 100, Color.Cyan, window);

engine.Run();
```

`Engine` owns the update and render loop. `using` makes sure windows, renderers, textures, and interaction objects are released when the application exits.

## `Vellum.Core`

### `Engine`

`Engine` initializes SDL, owns the Vellum windows, polls input, updates registered objects, renders them, and processes native window messages.

The default constructor is public and parameterless.

#### Properties

| Member | Type | Description |
| --- | --- | --- |
| `GlobalHoverCount` | `static int` | Number of currently hovered interactive objects. It can be read or changed, but is normally maintained by Vellum. |
| `OnUpdate` | `Action` | Optional callback invoked once per engine update, before registered interactive objects are updated. |
| `MaxFrameRate` | `int` | Maximum update rate. `0` means no manual cap; with display clamping enabled, that follows the display refresh rate. Negative values throw `ArgumentOutOfRangeException`. |
| `TargetFrameRate` | `int` | Alias for `MaxFrameRate`. |
| `ClampToDisplayRefreshRate` | `bool` | When `true`, the effective cap cannot exceed the detected display refresh rate. Defaults to `true`. |
| `DisplayRefreshRate` | `int` | Detected refresh rate used by the frame limiter. It is updated when `Run()` starts. |
| `DeltaTime` | `float` | Seconds elapsed since the previous update. Read-only. |
| `Renderer` | `Renderer` | Gets or sets the preferred renderer. If unset, the renderer from `Window` is used. |
| `Window` | `Window` | Gets or sets the preferred window. If unset, the first Vellum window is used. |

#### Methods

| Signature | Description |
| --- | --- |
| `bool Initialize()` | Initializes SDL. Returns `false` if SDL rendering initialization fails. Call this before creating windows. |
| `void Run()` | Starts the blocking engine loop. Throws if the engine was not initialized or another engine loop is already active. |
| `void Stop()` | Requests that the running loop stop. |
| `bool Update()` | Runs one update/render iteration. Useful for a custom loop, but `Run()` must have been called first. Returns `false` when the engine should stop. |
| `void Dispose()` | Stops the loop, disposes all windows and renderers, clears registered objects, and shuts down SDL. |
| `static void RegisterAssetTicker(IUpdatable ticker)` | Registers an object that should receive `Update(deltaTime)` every frame. Throws for `null`. |

`Run()` is the easiest option. Use `Update()` only when another application owns the main loop and you can still call it from the thread that created the native windows.

### `IUpdatable`

Implement this interface when an object needs a per-frame callback.

```csharp
public interface IUpdatable
{
    void Update(float deltaTime);
}
```

`deltaTime` is measured in seconds. `Texture`, `Interactive<TShape>`, and `Draggable<TShape>` use this contract internally.

## `Vellum.Platform`

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

## `Vellum.Graphics`

### `Color`

`Color` stores red, green, blue, and alpha channels as bytes from `0` to `255`.

```csharp
var translucentPurple = new Color(170, 80, 255, 180);
```

#### Public members

| Member | Type | Description |
| --- | --- | --- |
| `R` | `byte` | Red channel. |
| `G` | `byte` | Green channel. |
| `B` | `byte` | Blue channel. |
| `A` | `byte` | Alpha channel. `0` is transparent and `255` is opaque. |
| `Color(byte r, byte g, byte b, byte a = 255)` | constructor | Creates a color. |
| `Transparent` | `static Color` | Fully transparent black. |
| `Black` | `static Color` | Opaque black. |
| `White` | `static Color` | Opaque white. |
| `Gray` | `static Color` | Opaque 128/128/128 gray. |
| `Red`, `Green`, `Blue` | `static Color` | Common primary colors. |
| `Yellow`, `Orange`, `Cyan`, `Magenta` | `static Color` | Common accent colors. |

### `Renderer`

`Renderer` draws into a `Window`. A renderer is normally created automatically by `Window.Create`, but it can also be created directly.

| Member | Description |
| --- | --- |
| `bool IsValid` | Indicates whether the native renderer is still alive. |
| `Window Window` | Window that owns this renderer. Read-only from consuming projects. |
| `static Renderer Create(Window window)` | Creates a software renderer for a valid window. |
| `void Clear(Color color)` | Clears the current frame using the supplied color. |
| `void Present()` | Presents the current frame. Layered windows are copied through the transparent presentation path. |
| `void DrawFillRect(Rect rect, Color color)` | Draws a filled rectangle and records the window in `rect.LastDrawnWindow`. |
| `void DrawFillCircle(Circle circle, Color color)` | Draws a filled circle and records the window in `circle.LastDrawnWindow`. |
| `void Dispose()` | Releases the renderer and any render targets it owns. |

The engine calls `Clear`, renders registered objects, and calls `Present` for you. Direct renderer use is most useful when implementing a custom `IRenderable`.

### `Texture`

`Texture` loads a static image or GIF and registers itself for rendering. GIF frames are decoded when the texture is created and uploaded as the animation advances.

Supported constructor:

```csharp
Texture(
    string path,
    float x,
    float y,
    Window window,
    float? w = null,
    float? h = null,
    float animationSpeed = 1f)
```

`w` and `h` default to the source image dimensions. `animationSpeed` only affects animated GIFs:

- `1f` — original playback speed;
- `0.5f` — half speed;
- `2f` — double speed;
- `0f` — pause the GIF.

#### Properties and methods

| Member | Description |
| --- | --- |
| `IntPtr Handle` | SDL texture handle. Read-only from consuming projects. |
| `Rect DstRect` | Destination rectangle. The `X`, `Y`, `W`, and `H` properties proxy to it. |
| `float OriginalWidth` / `OriginalHeight` | Source image dimensions. |
| `float X` / `Y` | Destination position. |
| `float W` / `H` | Destination size. |
| `Window AssociatedWindow` | Window used for rendering this texture. |
| `Window LastDrawnWindow` | Window in which the texture was most recently rendered. |
| `float AnimationSpeed` | GIF playback multiplier. It can be set during object initialization and must be finite and non-negative. |
| `bool ContainsPoint(float pointX, float pointY)` | Tests whether a point is inside the destination rectangle. |
| `void Update(float deltaTime)` | Advances a GIF by the supplied number of seconds. The engine calls this automatically. |
| `void Render(Renderer renderer)` | Draws the texture through the supplied renderer. |
| `void Dispose()` | Removes the texture from engine registries and releases its SDL texture. |

Example:

```csharp
using var gif = new Texture(
    "assets\animation.gif",
    100,
    80,
    window,
    w: 256,
    h: 256,
    animationSpeed: 1.25f);
```

## `Vellum.Geometry`

### `IShape`

Implement `IShape` to make a custom object usable with `Interactive<TShape>` or `Draggable<TShape>`.

| Member | Description |
| --- | --- |
| `float X { get; set; }` | Horizontal position in the local window coordinate system. |
| `float Y { get; set; }` | Vertical position in the local window coordinate system. |
| `Window LastDrawnWindow { get; set; }` | Window used by the most recent render. Interaction uses this to choose the correct coordinate space. |
| `bool ContainsPoint(float pointX, float pointY)` | Returns whether local coordinates are inside the shape. |

### `Rect`

`Rect` is a basic filled rectangle and an `IShape` implementation.

```csharp
var rectangle = new Rect(20, 30, 200, 80, Color.Orange, window);
```

| Member | Description |
| --- | --- |
| `float X`, `Y`, `W`, `H` | Position and size. |
| `Color Color` | Fill color. Alpha `0` makes it invisible. |
| `Window AssociatedWindow` | Window in which the rectangle is rendered. |
| `Window LastDrawnWindow` | Most recent render target. |
| `Rect(float x, float y, float w, float h, Color? color = null, Window associatedWindow = null)` | Creates a rectangle. Defaults to white and optionally registers it with the supplied window. |
| `bool ContainsPoint(float pointX, float pointY)` | Tests whether a local point is inside the rectangle. |
| `implicit operator SDL.FRect` | Converts to an SDL floating-point rectangle. |
| `implicit operator SDL.Rect` | Converts to an SDL integer rectangle. |
| `implicit operator Rect(SDL.FRect)` | Creates a Vellum rectangle from an SDL floating-point rectangle. |
| `implicit operator Rect(SDL.Rect)` | Creates a Vellum rectangle from an SDL integer rectangle. |

### `Circle`

`Circle` is a basic filled circle and an `IShape` implementation.

| Member | Description |
| --- | --- |
| `float X` / `Y` | Center position. |
| `float Radius` | Circle radius. |
| `Color Color` | Fill color. |
| `Window AssociatedWindow` | Window in which the circle is rendered. |
| `Window LastDrawnWindow` | Most recent render target. |
| `Circle(float x, float y, float radius, Color? color = null, Window associatedWindow = null)` | Creates a circle. Defaults to white and registers it with the engine. |
| `bool ContainsPoint(float pointX, float pointY)` | Tests whether a point is inside the circle. |

## `Vellum.Input`

### `Manager`

`Manager` exposes the current global mouse position and polled keyboard/mouse state. Input state is refreshed by `Engine.Update()` or `Engine.Run()`.

#### Mouse position

| Member | Description |
| --- | --- |
| `MouseX` / `MouseY` | Mouse position in the selected Vellum window's client coordinates. Read-only. |
| `GlobalMouseX` / `GlobalMouseY` | Mouse position in desktop screen coordinates. Read-only. |
| `static (float X, float Y) GetLocalMouseState(Window window)` | Converts the global cursor position into a window's client coordinates. If the window is `null` or invalid, returns the global position. |

#### Keyboard and mouse state

| Signature | Description |
| --- | --- |
| `static bool IsKeyHeld(Key key)` | `true` every frame while the key is down. |
| `static bool WasKeyPressed(Key key)` | `true` only on the frame where the key changes from up to down. |
| `static bool IsMouseHeld(MouseButton button)` | `true` every frame while the button is down. |
| `static bool WasMousePressed(MouseButton button)` | `true` only on the frame where the button changes from up to down. |
| `static bool WasMouseReleased(MouseButton button)` | `true` on the frame where the button changes from down to up. |
| `static bool WasMouseClicked(MouseButton button)` | Same one-frame release event used by click interactions. |

Example:

```csharp
engine.OnUpdate = () =>
{
    if (Manager.IsKeyHeld(Key.LeftCtrl) && Manager.WasKeyPressed(Key.C))
        Console.WriteLine("Ctrl+C was pressed");

    if (Manager.WasMouseClicked(MouseButton.Left))
        Console.WriteLine("clicked");
};
```

### `MouseButton`

| Value | Meaning |
| --- | --- |
| `None` | No button. |
| `Left` | Left mouse button. |
| `Right` | Right mouse button. |
| `Middle` | Middle mouse button. |
| `X1` | First side/thumb button. |
| `X2` | Second side/thumb button. |

### `Key`

`Key` contains the Windows virtual-key values used by `Manager`. It includes:

- modifiers: `LeftShift`, `RightShift`, `LeftCtrl`, `RightCtrl`, `LeftAlt`, `RightAlt`, `LeftWin`, `RightWin`;
- controls: `Backspace`, `Tab`, `Return`, `Escape`, `Space`, `CapsLock`, `NumLock`, `ScrollLock`;
- navigation: `Left`, `Up`, `Right`, `Down`, `Insert`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`, `PrintScreen`, `Pause`, `ContextMenu`;
- numbers: `Num0` through `Num9`;
- letters: `A` through `Z`;
- function keys: `F1` through `F24`;
- numpad keys: `Numpad0` through `Numpad9`, `NumpadMultiply`, `NumpadAdd`, `NumpadSeparator`, `NumpadSubtract`, `NumpadDecimal`, `NumpadDivide`;
- punctuation: `Semicolon`, `Plus`, `Comma`, `Minus`, `Period`, `Slash`, `Grave`, `OpenBracket`, `Backslash`, `CloseBracket`, and `Quote`.

`None` is the zero value.

## `Vellum.Graphics.IRenderable`

Implement `IRenderable` when you want a custom object to be drawn by the engine.

```csharp
public interface IRenderable
{
    Window AssociatedWindow { get; set; }
    void Render(Renderer renderer);
}
```

`Rect`, `Circle`, and `Texture` already integrate with the engine's renderable workflow. The interface is also useful when drawing a custom object manually from your own update/render code; there is not currently a public `RegisterRenderable` method.

## `Vellum.UI`

### `Interactive<TShape>`

`Interactive<TShape>` wraps any reference type implementing `IShape` and adds hover, press, release, and click callbacks.

```csharp
var button = new Interactive<Rect>(
    new Rect(20, 20, 180, 60, Color.Blue, window));

button.OnHoverEnter = () => button.Shape.Color = Color.Cyan;
button.OnHoverExit = () => button.Shape.Color = Color.Blue;
button.OnClicked = () => Console.WriteLine("button clicked");
```

#### Public members

| Member | Description |
| --- | --- |
| `TShape Shape` | Wrapped shape. Read-only reference; the shape's properties can still be changed. |
| `Action OnHoverEnter` | Invoked when the cursor enters the shape. |
| `Action OnHoverExit` | Invoked when the cursor leaves the shape. |
| `Action OnPressed` | Invoked when the left mouse button is pressed over the shape. |
| `Action OnReleased` | Invoked when a captured left mouse button is released. |
| `Action OnClicked` | Invoked when the left mouse button is released over the shape. |
| `Interactive(TShape bounds, Func<TShape, float, float, bool> customHitTest = null)` | Creates and registers an interactive wrapper. The custom hit-test receives the shape and local mouse coordinates. |
| `void Update(float deltaTime)` | Updates hover and mouse state. Called automatically by the engine. |
| `implicit operator TShape` | Lets the wrapper be used where its underlying shape is expected. |
| `void Dispose()` | Removes the wrapper from engine updates and clears its callbacks. |

The callback properties accept `null`, which disables that callback.

### `Draggable<TShape>`

`Draggable<TShape>` extends `Interactive<TShape>` with click-and-drag behavior.

```csharp
var draggable = new Draggable<Circle>(
    new Circle(100, 100, 30, Color.Green, window))
{
    DragThreshold = 6f,
    LockVertical = true
};
```

| Member | Description |
| --- | --- |
| `bool LockHorizontal` | Prevents horizontal movement while dragging. |
| `bool LockVertical` | Prevents vertical movement while dragging. |
| `float DragThreshold` | Minimum mouse movement before a press becomes a drag. Defaults to `4`. |
| `Draggable(TShape bounds, Func<TShape, float, float, bool> customHitTest = null)` | Creates a draggable interactive wrapper. |
| `void Update(float deltaTime)` | Runs normal interaction handling and updates the drag position. |

`Draggable<TShape>` also exposes all inherited `Shape`, callback, `Update`, conversion, and `Dispose` members from `Interactive<TShape>`.

## custom shapes

A custom interactive shape only needs to implement `IShape`:

```csharp
public sealed class Diamond : IShape
{
    public float X { get; set; }
    public float Y { get; set; }
    public Window LastDrawnWindow { get; set; }

    public bool ContainsPoint(float pointX, float pointY)
    {
        return MathF.Abs(pointX - X) + MathF.Abs(pointY - Y) <= 40f;
    }
}
```

To draw it, implement `IRenderable` or draw it yourself through `Renderer`.

## common mistakes

### Creating a window before initialization

```csharp
using var engine = new Engine();
if (!engine.Initialize()) return;

using var window = Window.Create("Ready", 640, 360);
```

### Mixing standard and overlay windows

The current engine intentionally rejects a standard window alongside an overlay. Use one window mode per engine run.

### Forgetting disposal

Prefer `using var` for `Engine`, `Window`, `Texture`, `Renderer`, and interaction wrappers. Native window and SDL resources are not managed by the garbage collector alone.

### Expecting an overlay above exclusive fullscreen

Normal topmost Win32 windows work over the desktop and many windowed/borderless applications. Exclusive fullscreen applications own the display path, so a regular overlay window cannot guarantee visibility there without API-specific rendering integration.
