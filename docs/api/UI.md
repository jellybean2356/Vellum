# Vellum.UI

### `Interactive<TShape>`

`Interactive<TShape>` wraps any reference type implementing `IShape` and adds hover, press, release, and click callbacks.

```csharp
var button = new Interactive<Rect>(
    new Rect(20, 20, 180, 60, Color.Blue, window));
```

#### Properties

| Member | Type | Description |
| --- | --- | --- |
| `Shape` | `TShape` | Wrapped shape. |
| `IsEnabled` | `bool` | Enables/disables the underlying shape. |
| `X` | `float` | X position of the underlying shape. |
| `Y` | `float` | Y position of the underlying shape. |
| `IsHovered` | `bool` | `true` when the mouse is over the shape. |
| `IsPressed` | `bool` | `true` when the left mouse button is held on the shape. |
| `OnHover` | `Action` | Callback when hovering starts. |
| `OnLeave` | `Action` | Callback when hovering ends. |
| `OnPressed` | `Action` | Callback when the mouse button is pressed on the shape. |
| `OnReleased` | `Action` | Callback when the mouse button is released. |
| `OnClicked` | `Action` | Callback when the mouse button is clicked (pressed then released inside). |

`Interactive<TShape>` also implements `IUpdatable`, so `Update` is called automatically by the engine.

### `Draggable<TShape>`

`Draggable<TShape>` extends `Interactive<TShape>` and adds dragging support.

```csharp
var circle = new Draggable<Circle>(
    new Circle(120, 120, 48, Color.Cyan, window));
```

#### Properties

| Member | Type | Description |
| --- | --- | --- |
| `IsDragging` | `bool` | `true` when the shape is being dragged. |
| `OnDrag` | `Action` | Callback while dragging is active. |

`Draggable<TShape>` maintains `shape.X` and `shape.Y` automatically while dragging.
