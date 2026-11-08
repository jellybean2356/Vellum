# Vellum.Graphics

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
    float animationSpeed = 1f,
    Color? color)
```

`w` and `h` default to the source image dimensions. `animationSpeed` only affects animated GIFs:

- `1f` — original playback speed;
- `0.5f` — half speed;
- `2f` — double speed;
- `0f` — pause the GIF.

#### Properties and methods

| Member | Description |
| --- | --- |
| `bool IsEnabled` | `bool` | Enables/disables the texture for rendering. |
| `IntPtr Handle` | SDL texture handle. Read-only from consuming projects. |
| `Rect DstRect` | Destination rectangle. The `X`, `Y`, `W`, and `H` properties proxy to it. |
| `float OriginalWidth` / `OriginalHeight` | Source image dimensions. |
| `float X` / `Y` | Destination position. |
| `float W` / `H` | Destination size. Also accepts percentual scaling and "auto" keyword for automatic calculation.|
| `Color color` | Color / opacity / tint of the texture. |
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
    animationSpeed: 1.25f,
    color: Color.Black);
```

### `IRenderable`

Implement `IRenderable` when you want a custom object to be drawn by the engine.

```csharp
public interface IRenderable
{
    bool IsEnabled { get; set; }
    Window AssociatedWindow { get; set; }
    void Render(Renderer renderer);
}
```

`Rect`, `Circle`, and `Texture` already integrate with the engine's renderable workflow. The interface is also useful when drawing a custom object manually from your own update/render code; there is not currently a public `RegisterRenderable` method.

### `IColored`

Implement `IColored` when you want a custom object to have its own color.

```csharp
public interface IColored
{
    Color Color { get; set; }
}
```

