namespace Vellum;

public class Interactive<TShape> : IUpdatable 
    where TShape : class, IShape
{
    // private variables
    protected TShape Bounds { get; }
    protected bool IsHovered;

    private readonly Func<TShape, float, float, bool>? _customHitTest;
    
    // actions
    public Action? OnHoverEnter { get; set;}
    public Action? OnHoverExit { get; set; }
    public Action? OnPressed { get; set; }
    public Action? OnReleased { get; set; }
    public Action? OnClicked { get; set; }
    
    public Interactive(TShape bounds, Func<TShape, float, float, bool>? customHitTest = null)
    {
        Bounds = bounds;
        _customHitTest = customHitTest;
        
        // add to engines updatables
        Engine.Updatables.Add(this);
    }

    public virtual void Update(float deltaTime)
    {
        // check if cursor is inside the rect
        var overInteractive = _customHitTest != null 
            ? _customHitTest(Bounds, Input.MouseX, Input.MouseY) 
            : Bounds.ContainsPoint(Input.MouseX, Input.MouseY);
        
        // cursor enters rect area
        if (overInteractive && !IsHovered) 
        { 
            IsHovered = true; 
            Engine.GlobalHoverCount++; 
            OnHoverEnter?.Invoke(); 
        }
        
        // cursor leaves rect area
        if (!overInteractive && IsHovered) 
        { 
            IsHovered = false; 
            Engine.GlobalHoverCount--; 
            OnHoverExit?.Invoke(); 
        }
        
        if (overInteractive)
        {
            // on pressed
            if (Input.WasMousePressed(MouseButton.Left))
            {
                OnPressed?.Invoke();
            }

            // on released
            if (Input.WasMouseReleased(MouseButton.Left))
            {
                OnReleased?.Invoke();
            }

            // on clicked
            if (Input.WasMouseClicked(MouseButton.Left))
            {
                OnClicked?.Invoke();
            }
        }
    }
    
    public static implicit operator TShape(Interactive<TShape> interactive) => interactive.Bounds;
}