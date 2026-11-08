# Internal and Native Implementation

This section provides information about the internal and native implementation of Vellum.

The Vellum project includes several internal components that handle native Windows integration and SDL bridging. These components are intentionally not exposed as part of the public API.

### Key Internal Components

- `Vellum.Platform.NativeMethods`: Contains P/Invoke declarations for the Windows API.
- `Vellum.Platform.NativeMethods.Windowing`: Specific native windowing P/Invoke declarations.
- `Vellum.Platform.SdlWindowBridge`: Handles the communication and bridging between SDL window handles and Vellum's own window management system.
- `Vellum.Platform.Win32Window`: The internal implementation of Vellum windows, providing the Win32 `HWND` and handling platform-specific window messages.
- `Vellum.Platform.Window`: Public-facing window type that wraps the internal `Win32Window`.

These components are subject to change and are not intended for use by consuming projects.
