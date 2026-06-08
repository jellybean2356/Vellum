//OUTDATED//

/*namespace Vellum;

public sealed class DraggableRect(float x, float y, float w, float h) : InteractiveRect(x, y, w, h)
{
    public float DragThreshold { get; set; } = 5f;
    public bool LockHorizontal { get; set; } = false;
    public bool LockVertical { get; set; } = false;
    
    private float _grabOffsetX;
    private float _grabOffsetY;
    
    private float _startMouseX;
    private float _startMouseY;
    
    private bool _isHolding;
    private bool _isDragging;

    public override void Update(float deltaTime)
    {
        if (_isDragging)
        {
            if (!LockHorizontal) X = Input.MouseX - _grabOffsetX;
            if (!LockVertical) Y = Input.MouseY - _grabOffsetY;
            
            if (Input.WasMouseReleased(MouseButton.Left))
            {
                _isDragging = false;
            }
        }
        else if (_isHolding)
        {
            var dx = Input.MouseX - _startMouseX;
            var dy = Input.MouseY - _startMouseY;
            var distanceSq = (dx * dx) + (dy * dy);
            
            if (distanceSq >= DragThreshold * DragThreshold)
            {
                _isDragging = true;
                _isHolding = false;
            }
            
            if (Input.WasMouseReleased(MouseButton.Left))
            {
                _isHolding = false;
            }
        }
        else
        {
            if (IsOverInteractive() && Input.WasMousePressed(MouseButton.Left))
            {
                _isHolding = true;
                
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                
                _grabOffsetX = Input.MouseX - X;
                _grabOffsetY = Input.MouseY - Y;
            }
        }
        
        base.Update(deltaTime);
    }
}
*/