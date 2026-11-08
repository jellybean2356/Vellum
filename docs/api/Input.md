# Vellum.Input

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
