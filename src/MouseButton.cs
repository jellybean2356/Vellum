namespace Vellum;

public enum MouseButton
{
    None = 0x00,

    // ========================================================================
    // STANDARD BUTTONS
    // ========================================================================
    Left = 0x01,      // VK_LBUTTON
    Right = 0x02,     // VK_RBUTTON
    Middle = 0x04,    // VK_MBUTTON

    // ========================================================================
    // SIDE / EXTRA BUTTONS
    // ========================================================================
    X1 = 0x05,        // VK_XBUTTON1 (Typically the back thumb button)
    X2 = 0x06         // VK_XBUTTON2 (Typically the forward thumb button)
}