namespace Vellum.Input;

internal interface IMouseCapture
{
    bool IsMouseCaptured { get; }
    Window CapturedWindow { get; }
}

internal interface IInteractiveTarget : IMouseCapture
{
    IShape Shape { get; }
    Window InteractionWindow { get; }
    int? ZPriority { get; }
    long InteractionOrder { get; set; }
    bool IsPointerOver();
    void SetInputState(bool inputAllowed, bool pointerOver);
}
