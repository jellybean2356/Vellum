# Vellum

Vellum is a lightweight, desktop overlay and GUI framework for **C# / .NET 10** built on top of **SDL3** (via `SDL3-CS` bindings). It is designed to simplify creation of interactive overlays and user interfaces.

---

## Key Features

- **Layered Transparency & Click-through**: Create overlays that let mouse clicks pass directly through to background applications.
- **Dynamic Interaction Switching**: Seamlessly toggle click-through off when the cursor hovers over interactive overlay shapes, and toggle it back on when the cursor leaves.
- **Native OS Window Integration**: Includes utilities to retrieve active window handles, read window bounds, translate screen positions, and render debug visualizers on top of other desktop windows.
- **SDL3 Rendering Core**: Rendering pipeline with automatic fallbacks (Vulkan -> OpenGL -> OpenGLES2 -> Software).
- **Interactive Geometries**: Generic shapes (`Circle`, `Rect`) that support click-and-drag mechanics and UI interaction callbacks.

---

## Project Structure

```text
Vellum/
├── Vellum.sln                  # Project Solution
├── src/
│   ├── Core/
│   │   ├── Engine.cs           # Main game/application loop and SDL initialization
│   │   ├── Global.cs           # Global constants/settings
│   │   └── IUpdatable.cs       # Interface for elements that update each frame
│   ├── Geometry/
│   │   ├── IShape.cs           # Geometry interface (ContainsPoint support)
│   │   ├── Rect.cs             # Rectangular shapes and conversion helpers
│   │   └── Circle.cs           # Circular shapes
│   ├── Graphics/
│   │   ├── Color.cs            # RGBA color representation
│   │   └── Renderer.cs         # SDL3 renderer wrapper with fallback drivers
│   ├── Input/
│   │   ├── Key.cs              # Virtual keyboard key codes
│   │   ├── MouseButton.cs      # Mouse button codes
│   │   └── Manager.cs          # Low-level input tracking via SDL and GetAsyncKeyState
│   ├── Platform/
│   │   ├── NativeMethods.cs    # P/Invoke Win32 declarations (user32.dll, dwmapi.dll)
│   │   ├── Window.cs           # Wrapper for SDL3 window creation & layered settings
│   │   ├── WindowFlags.cs      # Preset flags (e.g., DefaultOverlay)
│   │   └── WindowUtils.cs      # Win32 helper methods for active window tracking
│   └── UI/
│       ├── Interactive.cs      # Event wrapper for shapes (OnHover, OnClicked, etc.)
│       └── Draggable.cs        # Extension of Interactive supporting click-and-drag
└── examples/
    └── Sandbox/                # Testing environment / demo code
```

---

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** or higher
- **Windows OS** (due to native Win32/DWM APIs used for overlay transparent modes)

### Basic Usage Example

The following example initializes the engine, registers a draggable circle, and renders it inside an overlay window.

```csharp
using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;

namespace Sandbox;

public class Test
{
    private static void Main()
    {
        // Initialize the engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;
        
        var colorState = Color.Red;

        // Create a draggable circle
        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f))
        {
            OnClicked = async void () =>
            {
                colorState = Color.Blue;
                await Task.Delay(100);
                colorState = Color.Red;
            },
        };

        // Main overlay loop
        while (engine.Update())
        {
            // Draw circle shape
            engine.Renderer.DrawFillCircle(circle, colorState);
            
            // Present the renderer frame buffer
            engine.Renderer.Present();
        }
    }
}
```

### Running the Example

To run the sandbox example, navigate to the project directory and run the Sandbox project:

```bash
dotnet run --project examples/Sandbox/Sandbox.csproj
```
