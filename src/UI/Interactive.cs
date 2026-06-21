namespace Vellum.UI;

public class Interactive<TShape> : IUpdatable , IDisposable
    where TShape : class, IShape
{
    // private variables
    public TShape Shape { get; }
    protected bool IsHovered;
    private Window _hoveredWindow;

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
        
        
        // add to engines updatables
        Engine.Updatables.Add(this);
    }
    
    protected (float X, float Y) GetLocalMouse()
    {
        var win = Shape.LastDrawnWindow ?? (Engine.Windows.Count > 0 ? Engine.Windows[0] : null);
        return Input.Manager.GetLocalMouseState(win);
    }

    public virtual void Update(float deltaTime)
    {
        var win = Shape.LastDrawnWindow;

        // Smart fallback behavior for the very first frame before drawing has run
        if (win == null && Engine.Windows.Count > 0)
        {
            SDL.GetGlobalMouseState(out var gx, out var gy);
            foreach (var w in Engine.Windows)
            {
                SDL.GetWindowPosition(w.Handle, out var wx, out var wy);
                SDL.GetWindowSize(w.Handle, out var ww, out var wh);
                if (gx >= wx && gx <= wx + ww && gy >= wy && gy <= wy + wh)
                {
                    win = w;
                    break;
                }
            }
            win ??= Engine.Windows[0];
        }

        // Convert global mouse coordinates cleanly to the localized target window space
        float localX, localY;
        if (win != null)
        {
            SDL.GetGlobalMouseState(out var gx, out var gy);
            SDL.GetWindowPosition(win.Handle, out var wx, out var wy);
            localX = gx - wx;
            localY = gy - wy;
        }
        else
        {
            localX = Manager.MouseX;
            localY = Manager.MouseY;
        }

        // Perform spatial evaluation entirely localized within target window boundaries
        bool overInteractive = _customHitTest != null 
            ? _customHitTest(Shape, localX, localY) 
            : Shape.ContainsPoint(localX, localY);
        
        // Track hover metrics safely relative to their localized origin instance
        if (overInteractive && !IsHovered) 
        { 
            IsHovered = true; 
            Engine.GlobalHoverCount++;
            _hoveredWindow = win;
            if (_hoveredWindow != null) _hoveredWindow.HoverCount++;
            OnHoverEnter?.Invoke(); 
        }
        
        if (!overInteractive && IsHovered) 
        { 
            IsHovered = false;
            Engine.GlobalHoverCount--;
            if (_hoveredWindow != null) _hoveredWindow.HoverCount--;
            _hoveredWindow = null;
            OnHoverExit?.Invoke(); 
        }
        
        if (overInteractive)
        {
            if (Manager.WasMousePressed(MouseButton.Left))   OnPressed?.Invoke();
            if (Manager.WasMouseReleased(MouseButton.Left))  OnReleased?.Invoke();
            if (Manager.WasMouseClicked(MouseButton.Left))   OnClicked?.Invoke();
        }
    }
    
    public static implicit operator TShape(Interactive<TShape> interactive) => interactive.Shape;

    public void Dispose()
    {
        Engine.Updatables.Remove(this);
        
        OnHoverEnter = null;
        OnHoverExit = null;
        OnPressed = null;
        OnReleased = null;
        OnClicked = null;
    }
}