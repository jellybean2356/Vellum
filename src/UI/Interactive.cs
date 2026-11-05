namespace Vellum.UI;

public class Interactive<TShape> : IUpdatable, IDisposable, IMouseCapture
    where TShape : class, IShape
{
    // private variables
    public TShape Shape { get; }
    protected bool IsHovered;
    private Window _hoveredWindow;
    private Window _capturedWindow;

    bool IMouseCapture.IsMouseCaptured => _capturedWindow != null;
    Window IMouseCapture.CapturedWindow => _capturedWindow;

    private readonly Func<TShape, float, float, bool> _customHitTest;
    
    // actions
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
    
    // get local mouse state
    protected (float X, float Y) GetLocalMouse()
    {
        return Manager.GetLocalMouseState(Shape.LastDrawnWindow ?? (Engine.Windows.Count > 0 ? Engine.Windows[0] : null));
    }

    // update each frame
    public virtual void Update(float deltaTime)
    {
        if (Shape.LastDrawnWindow != null && _hoveredWindow == null)
        {
            _hoveredWindow = Shape.LastDrawnWindow;
        }

        var win = Shape.LastDrawnWindow ?? _hoveredWindow;

        // fallback behavior for the very first frame before drawing has run
        if (win == null && Engine.Windows.Count > 0)
        {
            foreach (var w in Engine.Windows.Where(w => w.IsValid && IsWindowVisible(w.Hwnd)))
            {
                var (x, y) = Manager.GetLocalMouseState(w);
                Win32Window.Check(GetClientRect(w.Hwnd, out var client), "GetClientRect (hit testing)");
                if (!(x >= 0) || !(x < client.Right) || !(y >= 0) || !(y < client.Bottom)) continue;
                win = w;
                break;
            }

            win ??= Engine.Windows[0];
        }

        // convert global mouse coordinates to the local target window space
        float localX, localY;
        if (win != null)
        {
            (localX, localY) = Manager.GetLocalMouseState(win);
        }
        else
        {
            localX = Manager.MouseX;
            localY = Manager.MouseY;
        }

        // is cursor over an interactive element in local space
        var overInteractive = (win == null || win.IsValid && IsWindowVisible(win.Hwnd)) &&
                              (_customHitTest?.Invoke(Shape, localX, localY) ?? Shape.ContainsPoint(localX, localY));

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
