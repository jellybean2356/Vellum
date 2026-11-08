# Vellum.Core

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
| `void Add()` | Registers the object into the engine. |
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
