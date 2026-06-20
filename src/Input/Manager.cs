namespace Vellum.Input;

public static class Manager
{
    // private variables
    private static readonly MouseButton[] ButtonsToTrack = 
    [
        MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.X1, MouseButton.X2
    ];
    private static readonly Key[] KeysToTrack = Enum.GetValues<Key>()
        .Where(key => key != Key.None)
        .ToArray();

    private static readonly bool[] CurrentKeyStates = new bool[256];
    private static readonly bool[] PreviousKeyStates = new bool[256];
    private static readonly bool[] CurrentMouseStates = new bool[7];
    private static readonly bool[] PreviousMouseStates = new bool[7];
    
    private static readonly bool[] WasClickedState = new bool[7];
    private static readonly bool[] WasReleasedState = new bool[7];

    // public variables
    public static float MouseX { get; private set; }
    public static float MouseY { get; private set; }
    
    public static float GlobalMouseX { get; private set; }
    public static float GlobalMouseY { get; private set; }

    // KEYBOARD INPUT
    /// <summary> continuous state: true every frame the key is being held down </summary>
    public static bool IsKeyHeld(Key key)
    {
        var index = (int)key;
        return IsValidKeyIndex(index) && CurrentKeyStates[index];
    }

    /// <summary> single-frame event: true only on the exact frame the key was first pushed down. </summary>
    public static bool WasKeyPressed(Key key)
    {
        var index = (int)key;
        return IsValidKeyIndex(index) && CurrentKeyStates[index] && !PreviousKeyStates[index];
    }
    
    // MOUSE INPUT
    /// <summary> true every frame the mouse button is actively held down </summary>
    public static bool IsMouseHeld(MouseButton button)
    {
        var index = (int)button;
        return IsValidMouseIndex(index) && CurrentMouseStates[index];
    }

    /// <summary> true only on the frame the mouse button transitioned from up to down </summary>
    public static bool WasMousePressed(MouseButton button) 
    {
        var index = (int)button;
        return IsValidMouseIndex(index) && CurrentMouseStates[index] && !PreviousMouseStates[index];
    }

    /// <summary> true on the frame, the mouse button is let go. </summary>
    public static bool WasMouseReleased(MouseButton button)
    {
        var index = (int)button;
        return IsValidMouseIndex(index) && WasReleasedState[index];
    }

    /// <summary> true on the frame, the mouse is let go. </summary>
    public static bool WasMouseClicked(MouseButton button)
    {
        var index = (int)button;
        return IsValidMouseIndex(index) && WasClickedState[index];
    }
    
    public static (float X, float Y) GetLocalMouseState(Window window)
    {
        if (window == null || window.Handle == IntPtr.Zero)
            return (GlobalMouseX, GlobalMouseY);
            
        SDL.GetWindowPosition(window.Handle, out var wx, out var wy);
        return (GlobalMouseX - wx, GlobalMouseY - wy);
    }
    
    // update states for mouse input
    internal static void UpdateStates(IntPtr window)
    {
        SDL.GetGlobalMouseState(out var gx, out var gy);
        GlobalMouseX = gx;
        GlobalMouseY = gy;

        SDL.GetWindowPosition(window, out var wx, out var wy);

        // Maintains native fallback behavior for active main window
        MouseX = gx - wx;
        MouseY = gy - wy;

        foreach (var key in KeysToTrack)
        {
            var index = (int)key;
            PreviousKeyStates[index] = CurrentKeyStates[index];
            CurrentKeyStates[index] = (GetAsyncKeyState(index) & 0x8000) != 0;
        }

        foreach (var btn in ButtonsToTrack)
        {
            var index = (int)btn;
            PreviousMouseStates[index] = CurrentMouseStates[index];
            CurrentMouseStates[index] = (GetAsyncKeyState(index) & 0x8000) != 0;

            var wasDown = PreviousMouseStates[index];
            var isDown = CurrentMouseStates[index];

            WasClickedState[index] = false;
            WasReleasedState[index] = false;

            if (!wasDown || isDown) continue;
            WasReleasedState[index] = true;
            WasClickedState[index] = true;
        }
    }

    private static bool IsValidKeyIndex(int index)
    {
        return index >= 0 && index < CurrentKeyStates.Length;
    }

    private static bool IsValidMouseIndex(int index)
    {
        return index >= 0 && index < CurrentMouseStates.Length;
    }
}
