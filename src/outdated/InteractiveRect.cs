//OUTDATED//

/*namespace Vellum;

public class InteractiveRect : Rect, IUpdatable
{
    public Action? OnHoverEnter { get; set;}
    public Action? OnHoverExit { get; set; }
    public Action? OnPressed { get; set; }
    public Action? OnReleased { get; set; }
    public Action? OnClicked { get; set; }

    private nint _window;
    private static int _hoverCount = 0; 
    private bool _isHovered = false;

    protected InteractiveRect(float x, float y, float w, float h) : base(x, y, w, h)
    {
        Engine.Updatables.Add(this);
        _window = Engine.Window;
    }
    
    protected bool IsOverInteractive()
    {
        // check if cursor is inside the rect
        return !(Input.MouseX < X || 
                 Input.MouseX > X + W ||
                 Input.MouseY < Y || 
                 Input.MouseY > Y + H);
    }

    public virtual void Update(float deltaTime)
    {
        var overInteractive = IsOverInteractive();
        
        // cursor enters rect area
        if (overInteractive && !_isHovered) 
        { 
            _isHovered = true; 
            _hoverCount++; 
            OnHoverEnter?.Invoke(); 
        }
        
        // cursor leaves rect area
        if (!overInteractive && _isHovered) 
        { 
            _isHovered = false; 
            _hoverCount--; 
            OnHoverExit?.Invoke(); 
        }
        
        // set click-through to false if mouse is over interactive parts
        Window.SetClickThrough(_window, _hoverCount == 0);
        
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
}
*/