namespace Vellum.Input;

internal interface IMouseCapture
{
    bool IsMouseCaptured { get; }
    Window CapturedWindow { get; }
}
