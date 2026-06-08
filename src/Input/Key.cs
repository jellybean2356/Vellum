namespace Vellum.Input;

public enum Key // https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
{
    None = 0x00,

    // ========================================================================
    // MODIFIER KEYS
    // ========================================================================
    LeftShift = 0xA0,
    RightShift = 0xA1,
    LeftCtrl = 0xA2,
    RightCtrl = 0xA3,
    LeftAlt = 0xA4,
    RightAlt = 0xA5,
    LeftWin = 0x5B,
    RightWin = 0x5C,

    // ========================================================================
    // TEXT EDITING & CONTROLS
    // ========================================================================
    Backspace = 0x08,
    Tab = 0x09,
    Return = 0x0D,
    Escape = 0x1B,
    Space = 0x20,
    CapsLock = 0x14,
    NumLock = 0x90,
    ScrollLock = 0x91,

    // ========================================================================
    // NAVIGATION & UTILITY
    // ========================================================================
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    Insert = 0x2D,
    Delete = 0x2E,
    Home = 0x24,
    End = 0x23,
    PageUp = 0x21,
    PageDown = 0x22,
    PrintScreen = 0x2C,
    Pause = 0x13,
    ContextMenu = 0x5D,

    // ========================================================================
    // ALPHANUMERIC ROW (0 - 9)
    // ========================================================================
    Num0 = 0x30, Num1 = 0x31, Num2 = 0x32, Num3 = 0x33, Num4 = 0x34,
    Num5 = 0x35, Num6 = 0x36, Num7 = 0x37, Num8 = 0x38, Num9 = 0x39,

    // ========================================================================
    // LETTERS (A - Z)
    // ========================================================================
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
    H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
    O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
    V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,

    // ========================================================================
    // FUNCTION KEYS
    // ========================================================================
    F1 = 0x70,  F2 = 0x71,  F3 = 0x72,  F4 = 0x73,  F5 = 0x74,  F6 = 0x75,
    F7 = 0x76,  F8 = 0x77,  F9 = 0x78,  F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
    F13 = 0x7C, F14 = 0x7D, F15 = 0x7E, F16 = 0x7F, F17 = 0x80, F18 = 0x81,
    F19 = 0x82, F20 = 0x83, F21 = 0x84, F22 = 0x85, F23 = 0x86, F24 = 0x87,

    // ========================================================================
    // NUMPAD KEYS
    // ========================================================================
    Numpad0 = 0x60, Numpad1 = 0x61, Numpad2 = 0x62, Numpad3 = 0x63, Numpad4 = 0x64,
    Numpad5 = 0x65, Numpad6 = 0x66, Numpad7 = 0x67, Numpad8 = 0x68, Numpad9 = 0x69,
    NumpadMultiply = 0x6A,
    NumpadAdd = 0x6B,
    NumpadSeparator = 0x6C,
    NumpadSubtract = 0x6D,
    NumpadDecimal = 0x6E,
    NumpadDivide = 0x6F,

    // ========================================================================
    // PUNCTUATION & SYMBOLS (Standard US Layout Mappings)
    // ========================================================================
    Semicolon = 0xBA,    // ; :
    Plus = 0xBB,         // = +
    Comma = 0xBC,        // , <
    Minus = 0xBD,        // - _
    Period = 0xBE,       // . >
    Slash = 0xBF,        // / ?
    Grave = 0xC0,        // ` ~ (Tilde)
    OpenBracket = 0xDB,  // [ {
    Backslash = 0xDC,    // \ |
    CloseBracket = 0xDD, // ] }
    Quote = 0xDE         // ' "
}