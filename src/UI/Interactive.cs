namespace Vellum.UI;

public class Interactive<TShape> : IUpdatable, IDisposable, IMouseCapture, IInteractiveTarget
    where TShape : class, IShape
{
    public TShape Shape { get; }
    protected bool IsHovered;
    private Window _hoveredWindow;
    private Window _capturedWindow;
    private bool _inputAllowed;
    private bool _pointerOver;
    private long _interactionOrder;

    bool IMouseCapture.IsMouseCaptured => _capturedWindow != null;
    Window IMouseCapture.CapturedWindow => _capturedWindow;
    IShape IInteractiveTarget.Shape => Shape;
    Window IInteractiveTarget.InteractionWindow => ResolveInteractionWindow();
    int? IInteractiveTarget.ZPriority => ZPriority;
    long IInteractiveTarget.InteractionOrder
    {
        get => _interactionOrder;
        set => _interactionOrder = value;
    }

    private readonly Func<TShape, float, float, bool> _customHitTest;

    public int? ZPriority { get; set; }

    public Action OnHoverEnter { get; set;}
    public Action OnHoverExit { get; set; }
    public Action OnPressed { get; set; }
    public Action OnReleased { get; set; }
    public Action OnClicked { get; set; }
    
    public Interactive(TShape bounds, Func<TShape, float, float, bool> customHitTest = null)
    {
        Shape = bounds;
        _customHitTest = customHitTest;
        
        if (bounds is IUpdatable updatableShape)
        {
            Engine.Updatables.Remove(updatableShape);
        }
        

        // add to engines updatables
        Engine.Updatables.Add(this);
    }

    protected bool HasInputPriority => _inputAllowed;

    protected (float X, float Y) GetLocalMouse()
    {
        return Manager.GetLocalMouseState(((IInteractiveTarget)this).InteractionWindow);
    }

    public virtual void Update(float deltaTime)
    {
        var win = ((IInteractiveTarget)this).InteractionWindow;
        var overInteractive = _inputAllowed && _pointerOver;

        var wasMousePressed = Manager.WasMousePressed(MouseButton.Left);
        var wasMouseReleased = Manager.WasMouseReleased(MouseButton.Left);

        switch (overInteractive)
        {
            // hover tracking
            case true when !IsHovered:
            {
                IsHovered = true; 
                Engine.GlobalHoverCount++;
                _hoveredWindow = win;
                if (_hoveredWindow != null) _hoveredWindow.HoverCount++;
                OnHoverEnter?.Invoke();
                break;
            }
            case false when IsHovered:
            {
                IsHovered = false;
                Engine.GlobalHoverCount--;
                if (_hoveredWindow != null) _hoveredWindow.HoverCount--;
                _hoveredWindow = null;
                OnHoverExit?.Invoke();
                break;
            }
        }
        
        // mouse input tracking
        if (overInteractive)
        {
            if (wasMousePressed && _capturedWindow == null)
            {
                _capturedWindow = win;
                Engine.MarkInteraction((IInteractiveTarget)this);
                OnPressed?.Invoke();
            }

            if (wasMouseReleased)
                OnClicked?.Invoke();
        }

        if (wasMouseReleased && _capturedWindow != null)
        {
            OnReleased?.Invoke();
            _capturedWindow = null;
        }

        if (Shape is IUpdatable updatableShape)
        {
            updatableShape.Update(deltaTime);
        }
    }

    bool IInteractiveTarget.IsPointerOver()
    {
        var window = ((IInteractiveTarget)this).InteractionWindow;
        if (window is not { IsValid: true } || !IsWindowVisible(window.Hwnd)) return false;
        if (!GetClientRect(window.Hwnd, out var client)) return false;

        var (localX, localY) = Manager.GetLocalMouseState(window);
        if (localX < 0 || localY < 0 || localX >= client.Right || localY >= client.Bottom)
            return false;

        return _customHitTest?.Invoke(Shape, localX, localY) ?? Shape.ContainsPoint(localX, localY);
    }

    void IInteractiveTarget.SetInputState(bool inputAllowed, bool pointerOver)
    {
        _inputAllowed = inputAllowed;
        _pointerOver = pointerOver;
    }

    private Window ResolveInteractionWindow()
    {
        if (Shape is IRenderable { AssociatedWindow: { IsValid: true } associatedWindow }) return associatedWindow;
        if (Shape.LastDrawnWindow is { IsValid: true }) return Shape.LastDrawnWindow;
        return Engine.Windows.FirstOrDefault(window => window.IsValid);
    }
    
    public static implicit operator TShape(Interactive<TShape> interactive) => interactive.Shape;

    public void Dispose()
    {
        Engine.Updatables.Remove(this);
        _capturedWindow = null;
        if (IsHovered)
        {
            IsHovered = false;
            Engine.GlobalHoverCount = Math.Max(0, Engine.GlobalHoverCount - 1);
            if (_hoveredWindow != null) _hoveredWindow.HoverCount = Math.Max(0, _hoveredWindow.HoverCount - 1);
            _hoveredWindow = null;
        }
        
        OnHoverEnter = null;
        OnHoverExit = null;
        OnPressed = null;
        OnReleased = null;
        OnClicked = null;
    }
}
