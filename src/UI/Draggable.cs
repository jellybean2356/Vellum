namespace Vellum.UI;

public sealed class Draggable<TShape>(TShape bounds, Func<TShape, float, float, bool> customHitTest = null) : 
    Interactive<TShape>(bounds, customHitTest) 
    where TShape : class, IShape
{
    // private variables
    private bool _isDragging;
    private bool _isHolding;
    private float _grabOffsetX;
    private float _grabOffsetY;
    private float _startMouseX;
    private float _startMouseY;
    
    // public variables
    public bool LockHorizontal { get; set; }
    public bool LockVertical { get; set; }
    public float DragThreshold { get; set; } = 4f;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        if (_isDragging)
        {
            // X or Y axis locking + drag application
            if (!LockHorizontal) Shape.X = Manager.MouseX - _grabOffsetX;
            if (!LockVertical) Shape.Y = Manager.MouseY - _grabOffsetY;
            
            // when mouse releases, stop dragging
            if (Manager.WasMouseReleased(MouseButton.Left))
            {
                _isDragging = false;
            }
        }
        else if (_isHolding) 
        {
            // drag calculation
            var dx = Manager.MouseX - _startMouseX;
            var dy = Manager.MouseY - _startMouseY;
            var distanceSq = (dx * dx) + (dy * dy);
            
            if (distanceSq >= DragThreshold * DragThreshold)
            {
                _isDragging = true;
                _isHolding = false;
            }
            
            if (Manager.WasMouseReleased(MouseButton.Left))
            {
                _isHolding = false;
            }
        }
        else
        {
            // when mouse is over the bounds and pressed, start dragging
            if (IsHovered && Manager.WasMousePressed(MouseButton.Left))
            {
                _isHolding = true;
                
                _startMouseX = Manager.MouseX;
                _startMouseY = Manager.MouseY;
                
                _grabOffsetX = Manager.MouseX - Shape.X;
                _grabOffsetY = Manager.MouseY - Shape.Y;
            }
        }
    }
}