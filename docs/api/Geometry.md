# Vellum.Geometry

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
| `bool IsEnabled` | Enables/disables the rectangle for rendering. |
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
| `bool IsEnabled` | Enables/disables the circle for rendering. |
| `Color Color` | Fill color. |
| `Window AssociatedWindow` | Window in which the circle is rendered. |
| `Window LastDrawnWindow` | Most recent render target. |
| `Circle(float x, float y, float radius, Color? color = null, Window associatedWindow = null)` | Creates a circle. Defaults to white and registers it with the engine. |
| `bool ContainsPoint(float pointX, float pointY)` | Tests whether a point is inside the circle. |
