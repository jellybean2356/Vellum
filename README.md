# Vellum

Vellum is a lightweight C# / .NET 10 framework for building SDL3-powered desktop windows, transparent overlays, and simple interactive UI primitives on Windows.

It wraps SDL3-CS rendering and window creation with a small engine loop, Win32/DWM helpers for overlay behavior, global input tracking, basic geometry, and shape-based interaction classes.

## Current Capabilities

- **SDL3 engine loop**: initializes SDL video, polls quit events, tracks delta time, updates registered interactive objects, clears all windows, and disposes resources cleanly.
- **Standard windows and overlays**: create normal SDL windows or fullscreen transparent overlays. The engine intentionally prevents mixing standard and overlay window modes in one run.
- **Transparent click-through overlays**: overlay windows are layered, borderless, always on top, not focusable, and dynamically toggle click-through based on hover state.
- **Multiple standard windows**: the sandbox demonstrates rendering and interaction across several independent SDL windows.
- **Renderer fallback chain**: tries Vulkan, OpenGL, and OpenGLES2 before falling back to SDL software rendering.
- **Shape rendering**: draw filled rectangles and circles with RGBA colors and alpha blending.
- **Shape interaction**: wrap any `IShape` in `Interactive<TShape>` for hover, press, release, and click callbacks.
- **Drag behavior**: `Draggable<TShape>` adds threshold-based dragging with optional horizontal or vertical axis locks.
- **Win32 window utilities**: enumerate visible top-level windows, read titles and visual bounds, translate native window bounds into overlay-local coordinates, and draw debug outlines.

## Requirements

- Windows
- .NET 10 SDK
- SDL3-CS packages, restored by `dotnet restore`

The core library targets `net10.0`, enables nullable as disabled, allows unsafe blocks, and has `PublishAot` enabled. The current package references are:

- `SDL3-CS` 3.4.2
- `SDL3-CS.Native` 3.4.2

## Project Layout

```text
Vellum/
|-- Vellum.sln
|-- README.md
|-- src/
|   |-- Vellum.csproj
|   |-- Core/
|   |   |-- Engine.cs        # SDL initialization, frame update loop, window/updatable registry
|   |   |-- Global.cs        # global usings shared by the library
|   |   `-- IUpdatable.cs    # update contract used by interactive objects
|   |-- Geometry/
|   |   |-- IShape.cs        # shape contract with position, hit testing, and last drawn window
|   |   |-- Rect.cs          # rectangle geometry and SDL/Win32 conversion helpers
|   |   `-- Circle.cs        # circle geometry and hit testing
|   |-- Graphics/
|   |   |-- Color.cs         # RGBA color struct and common colors
|   |   `-- Renderer.cs      # SDL renderer wrapper and draw helpers
|   |-- Input/
|   |   |-- Key.cs           # Win32 virtual-key enum
|   |   |-- MouseButton.cs   # Win32 mouse-button enum
|   |   `-- Manager.cs       # global/local mouse state and keyboard/mouse queries
|   |-- Platform/
|   |   |-- NativeMethods.cs # Win32 and DWM P/Invoke declarations
|   |   |-- Window.cs        # SDL window wrapper and overlay click-through behavior
|   |   |-- WindowFlags.cs   # window flag enum and overlay preset
|   |   |-- WindowType.cs    # Standard or Overlay
|   |   `-- WindowUtils.cs   # HWND helpers, bounds helpers, debug window outlines
|   `-- UI/
|       |-- Interactive.cs   # generic shape event wrapper
|       |-- Draggable.cs     # draggable shape wrapper
|       `-- _outdated/       # commented legacy rectangle-only interaction prototypes
`-- examples/
    |-- Sandbox/             # multiple standard windows with interactive shapes
    `-- Sandbox2/            # fullscreen transparent overlay with a draggable circle
```

## Build

```bash
dotnet restore
dotnet build
```

## Run The Examples

Run the standard multi-window demo:

```bash
dotnet run --project examples/Sandbox/Sandbox.csproj
```

Run the transparent overlay demo:

```bash
dotnet run --project examples/Sandbox2/Sandbox2.csproj
```

## Basic Standard Window Example

```csharp
using System.Threading.Tasks;
using Vellum.Core;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Platform;
using Vellum.UI;

using var engine = new Engine();
if (!engine.Initialize()) return;

using var window = Window.Create("Vellum", 500, 500, WindowFlags.None);

var color = Color.Red;
var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f))
{
    OnClicked = async void () =>
    {
        color = Color.Blue;
        await Task.Delay(100);
        color = Color.Red;
    }
};

while (engine.Update())
{
    window.Renderer.DrawFillCircle(circle, color);
    window.Renderer.Present();
}
```

## Basic Overlay Example

```csharp
using Vellum.Core;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Platform;
using Vellum.UI;

using var engine = new Engine();
if (!engine.Initialize()) return;

using var overlay = Window.CreateOverlay("Vellum Overlay");
var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f));

while (engine.Update())
{
    overlay.Renderer.DrawFillCircle(circle, Color.Red);
    overlay.Renderer.Present();
}
```

When the mouse is not over an interactive shape, overlay windows stay click-through. When an interactive shape is hovered, Vellum disables click-through for that overlay so the shape can receive mouse input.

## Notes

- Keyboard and mouse button states use per-frame snapshots backed by Win32 `GetAsyncKeyState`, while mouse position is read from SDL global mouse state and converted into local window coordinates.
- `DrawFillRect` and `DrawFillCircle` update each shape's `LastDrawnWindow`, which is how `Interactive<TShape>` resolves hit testing for shapes rendered into different windows.
- The overlay helper currently defaults to the first display returned by SDL when no display id is provided.
