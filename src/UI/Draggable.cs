using Vellum.Geometry;
using Vellum.Input;

namespace Vellum.UI;

public sealed class Draggable<TShape>(TShape bounds, Func<TShape, float, float, bool>? customHitTest = null) : 
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
            if (!LockHorizontal) Bounds.X = Input.Input.MouseX - _grabOffsetX;
            if (!LockVertical) Bounds.Y = Input.Input.MouseY - _grabOffsetY;
            
            // when mouse releases, stop dragging
            if (Input.Input.WasMouseReleased(MouseButton.Left))
            {
                _isDragging = false;
            }
        }
        else if (_isHolding) 
        {
            // drag calculation
            var dx = Input.Input.MouseX - _startMouseX;
            var dy = Input.Input.MouseY - _startMouseY;
            var distanceSq = (dx * dx) + (dy * dy);
            
            if (distanceSq >= DragThreshold * DragThreshold)
            {
                _isDragging = true;
                _isHolding = false;
            }
            
            if (Input.Input.WasMouseReleased(MouseButton.Left))
            {
                _isHolding = false;
            }
        }
        else
        {
            // when mouse is over the bounds and pressed, start dragging
            if (IsHovered && Input.Input.WasMousePressed(MouseButton.Left))
            {
                _isHolding = true;
                
                _startMouseX = Input.Input.MouseX;
                _startMouseY = Input.Input.MouseY;
                
                _grabOffsetX = Input.Input.MouseX - Bounds.X;
                _grabOffsetY = Input.Input.MouseY - Bounds.Y;
            }
        }
    }
}