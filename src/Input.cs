using SDL3;

namespace Vellum;

public static class Input
{
    // private variables
    private static readonly MouseButton[] ButtonsToTrack = 
    [
        MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.X1, MouseButton.X2
    ];

    private static readonly bool[] CurrentMouseStates = new bool[7];
    private static readonly bool[] PreviousMouseStates = new bool[7];
    private static readonly float[] PressPositionsX = new float[7];
    private static readonly float[] PressPositionsY = new float[7];
    
    private static readonly bool[] WasClickedState = new bool[7];
    private static readonly bool[] WasReleasedState = new bool[7];

    // public variables
    public static float MouseX { get; private set; }
    public static float MouseY { get; private set; }

    // KEYBOARD INPUT
    /// <summary> continuous state: true every frame the key is being held down </summary>
    public static bool IsKeyHeld(Key key) => (NativeMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;

    /// <summary> single-frame event: true only on the exact frame the key was first pushed down. </summary>
    public static bool WasKeyPressed(Key key) => false; // Implement similarly if tracking previous keyboard frames later
    
    // MOUSE INPUT
    /// <summary> true every frame the mouse button is actively held down </summary>
    public static bool IsMouseHeld(MouseButton button) => CurrentMouseStates[(int)button];

    /// <summary> true only on the frame the mouse button transitioned from up to down </summary>
    public static bool WasMousePressed(MouseButton button) 
    {
        return CurrentMouseStates[(int)button] && !PreviousMouseStates[(int)button];
    }

    /// <summary> true on the frame, the mouse button is let go. </summary>
    public static bool WasMouseReleased(MouseButton button) => WasReleasedState[(int)button];

    /// <summary> true on the frame, the mouse is let go. </summary>
    public static bool WasMouseClicked(MouseButton button) => WasClickedState[(int)button];
    
    // update states for mouse input
    internal static void UpdateStates(IntPtr window)
    {
        // get mouse and window positions
        SDL.GetGlobalMouseState(out var gx, out var gy);
        SDL.GetWindowPosition(window, out var wx, out var wy);

        // calculate the position relative to window
        MouseX = gx - wx;
        MouseY = gy - wy;

        // update mouse states
        foreach (var btn in ButtonsToTrack)
        {
            var index = (int)btn;
            PreviousMouseStates[index] = CurrentMouseStates[index];
            CurrentMouseStates[index] = (NativeMethods.GetAsyncKeyState(index) & 0x8000) != 0;

            var wasDown = PreviousMouseStates[index];
            var isDown = CurrentMouseStates[index];

            WasClickedState[index] = false;
            WasReleasedState[index] = false;

            if (isDown || !wasDown) continue;
            WasReleasedState[index] = true;
            WasClickedState[index] = true;
        }
    }
}