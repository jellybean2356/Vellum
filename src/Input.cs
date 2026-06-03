using SDL3;

namespace Vellum;

public static class Input
{
    // private variables
    private static readonly MouseButton[] ButtonsToTrack = 
    [
        MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.X1, MouseButton.X2
    ];

    private const float DragThresholdSq = 16.0f;

    private static readonly bool[] CurrentMouseStates = new bool[7];
    private static readonly bool[] PreviousMouseStates = new bool[7];
    private static readonly float[] PressPositionsX = new float[7];
    private static readonly float[] PressPositionsY = new float[7];
    
    private static readonly bool[] IsDraggingState = new bool[7];
    private static readonly bool[] WasClickedState = new bool[7];
    private static readonly bool[] WasReleasedState = new bool[7];

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

    /// <summary> true every frame the mouse button is held down AND has moved past the drag threshold </summary>
    public static bool IsMouseDragging(MouseButton button) => IsDraggingState[(int)button];

    /// <summary> true only on the frame the mouse button transitioned from up to down </summary>
    public static bool WasMousePressed(MouseButton button) 
    {
        return CurrentMouseStates[(int)button] && !PreviousMouseStates[(int)button];
    }

    /// <summary> true on the frame the mouse button is let go regardless of whether a drag occurred. </summary>
    public static bool WasMouseReleased(MouseButton button) => WasReleasedState[(int)button];

    /// <summary> true on the frame the mouse is let go ONLY if the cursor stayed within the drag threshold (a clean click). </summary>
    public static bool WasMouseClicked(MouseButton button) => WasClickedState[(int)button];
    
    // update states for mouse input
    internal static void UpdateStates(IntPtr window, Rect[] interactiveParts)
    {
        SDL.GetGlobalMouseState(out var gx, out var gy);
        SDL.GetWindowPosition(window, out var wx, out var wy);

        MouseX = gx - wx;
        MouseY = gy - wy;

        foreach (var btn in ButtonsToTrack)
        {
            var index = (int)btn;
            PreviousMouseStates[index] = CurrentMouseStates[index];
            CurrentMouseStates[index] = (NativeMethods.GetAsyncKeyState(index) & 0x8000) != 0;

            var wasDown = PreviousMouseStates[index];
            var isDown = CurrentMouseStates[index];

            WasClickedState[index] = false;
            WasReleasedState[index] = false;

            if (isDown && !wasDown)
            {
                PressPositionsX[index] = MouseX;
                PressPositionsY[index] = MouseY;
                IsDraggingState[index] = false;
            }
            else if (isDown)
            {
                if (IsDraggingState[index]) continue;
                
                var dx = MouseX - PressPositionsX[index];
                var dy = MouseY - PressPositionsY[index];
                if ((dx * dx + dy * dy) >= DragThresholdSq)
                {
                    IsDraggingState[index] = true;
                }
            }
            else if (!isDown && wasDown)
            {
                WasReleasedState[index] = true;
                if (!IsDraggingState[index])
                {
                    WasClickedState[index] = true;
                }
                IsDraggingState[index] = false;
            }
        }

        // set click-through to false if mouse is over interactive parts
        var overInteractive = false;
        foreach (var part in interactiveParts)
        {
            if (!(MouseX >= part.X) || !(MouseX <= part.X + part.W) ||
                !(MouseY >= part.Y) || !(MouseY <= part.Y + part.H)) continue;
            overInteractive = true;
            break;
        }

        Window.SetClickThrough(window, !overInteractive);
    }
}