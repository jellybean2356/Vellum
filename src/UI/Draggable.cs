namespace Vellum.UI;

public sealed class Draggable<TShape>(TShape bounds, int? zPriority = null, Func<TShape, float, float, bool> customHitTest = null) : 
    Interactive<TShape>(bounds, zPriority, customHitTest) 
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
        if (!HasInputPriority) return;

        var (mouseX, mouseY) = GetLocalMouse();
        
        if (_isDragging)
        {
            // X or Y axis locking + drag application
            if (!LockHorizontal) Shape.X = mouseX - _grabOffsetX;
            if (!LockVertical) Shape.Y = mouseY - _grabOffsetY;
            
            // when mouse releases, stop dragging
            if (Manager.WasMouseReleased(MouseButton.Left))
            {
                _isDragging = false;
            }
        }
        else if (_isHolding) 
        {
            // drag calculation
            var dx = mouseX - _startMouseX;
            var dy = mouseY - _startMouseY;
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
            if (!IsHovered || !Manager.WasMousePressed(MouseButton.Left)) return;
            _isHolding = true;
                
            _startMouseX = mouseX;
            _startMouseY = mouseY;
                
            _grabOffsetX = mouseX - Shape.X;
            _grabOffsetY = mouseY - Shape.Y;
        }
    }
}
