# Vellum

<div align="center">

### small native windows, transparent overlays, and simple 2D rendering for Windows

<img src="https://img.shields.io/badge/platform-Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
<img src="https://img.shields.io/badge/DOTNET-10-512BD4?style=for-the-badge" alt="DOTNET 10" />
<img src="https://img.shields.io/badge/renderer-SDL3-1A1A1A?style=for-the-badge" alt="SDL3" />
<img src="https://img.shields.io/badge/status-experimental-F59E0B?style=for-the-badge" alt="Experimental" />

*Vellum is a little framework I’m building for native Windows tools, overlays, experiments, and interactive things that should stay lightweight.*

*NOTE: this project did use a little bit ai for learning purposes, 90% of the code was still written by me and every single line is looked at by me. dont expect something crazy, its just my personal project where im trying to learn something new.*

</div>

## what it does

Vellum creates real Win32 windows and uses SDL3 for the rendering side. That gives it a useful middle ground: the windows behave like native Windows windows, while drawing stays straightforward inside the project.

The current focus is:

- standard resizable windows;
- transparent, borderless overlays;
- click-through overlays that become interactive when needed;
- software-rendered rectangles, circles, textures, and animated GIFs;
- simple mouse and keyboard polling;
- small interaction helpers for hover, click, and dragging;
- multiple windows managed by one engine loop.

It is intentionally still a work in progress. The API is useful today, but it is not pretending to be a finished game engine or a universal overlay solution for exclusive-fullscreen applications.

## a quick look

```csharp
using Vellum.Core;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Input;
using Vellum.Platform;
using Vellum.UI;

using var engine = new Engine
{
    MaxFrameRate = 0,                 // 0 = follow the display refresh rate
    ClampToDisplayRefreshRate = true
};

if (!engine.Initialize()) return;

using var window = Window.Create(
    "Vellum",
    640,
    360,
    WindowFlags.Resizable);

using var circle = new Draggable<Circle>(
    new Circle(120, 120, 48, Color.Cyan, window));

circle.OnClicked = () => circle.Shape.Color = Color.Magenta;

engine.OnUpdate = () =>
{
    if (Manager.WasKeyPressed(Key.Escape))
        engine.Stop();
};

engine.Run();
```

For an overlay, replace the window creation line with:

```csharp
using var overlay = Window.CreateOverlay("Vellum overlay");
```

An overlay starts transparent and click-through. When an `Interactive<TShape>` or `Draggable<TShape>` object is hovered, Vellum temporarily allows the overlay to receive mouse input.

## getting started

### requirements

- Windows
- .NET 10 SDK
- a project that references the Vellum project or compiled DLL

The library currently targets `net10.0` and uses SDL3-CS, SDL3-CS.Native, SkiaSharp, and StbImageSharp internally.

### build

```powershell
dotnet restore
dotnet build src\Vellum.csproj
```

### examples

```powershell
dotnet run --project examples\Sandbox\Sandbox.csproj
dotnet run --project examples\Sandbox2\Sandbox2.csproj
```

The first example shows normal windows and interaction. The second one demonstrates a fullscreen transparent overlay with a draggable shape and animated assets.

## important ideas

### [Engine Overview](docs/Engine.md)

- **One engine loop**: Create one `Engine`, initialize it, create windows and renderables, then call `Run()`.
- **Object Registration**: Shapes like `Rect`, `Circle`, and `Texture` must be added to the engine to be rendered.
- **Texture Scaling**: `Texture.Create` supports percentage (`"110%"`) and automatic aspect ratio (`"auto"`) scaling.

## project layout

The repository is split into a few focused areas rather than exposing its internal platform plumbing as part of the public API:

- `src/Core` — engine lifecycle and update contracts
- `src/Geometry` — shapes and hit testing
- `src/Graphics` — colors, renderer, and textures
- `src/Input` — keyboard and mouse state
- `src/Platform` — public window types plus private native implementation
- `src/UI` — interaction and dragging helpers
- `examples` — small runnable projects
- `docs` — public API documentation and usage notes

## documentation

The public API reference is split into logical areas:

- [Engine Overview](docs/Engine.md)
- [Vellum.Core](docs/api/Core.md)
- [Vellum.Platform](docs/api/Platform.md)
- [Vellum.Graphics](docs/api/Graphics.md)
- [Vellum.Geometry](docs/api/Geometry.md)
- [Vellum.Input](docs/api/Input.md)
- [Vellum.UI](docs/api/UI.md)
- [Internal Implementation](docs/api/Internal.md)

The reference covers the classes, methods, properties, interfaces, and enums that are accessible when using the compiled DLL. Native implementation details are also included in the internal reference.

## current limitations

- Windows is the only supported platform right now.
- Rendering currently uses SDL's software renderer.
- Exclusive-fullscreen applications can prevent normal topmost overlays from appearing; Vellum does not inject into or hook game render backends.
- Input is polled through Windows global state, so applications with unusual input handling may behave differently from ordinary desktop windows.
- The API may change while the project is being cleaned up.

## license

There is no finalized license yet. Treat this repository as an experimental project while the API and distribution model are still being decided.
