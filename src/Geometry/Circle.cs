namespace Vellum.Geometry;

public class Circle : IShape, IRenderable
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; }
    
    public Window AssociatedWindow { get; set;}
    public Window LastDrawnWindow { get; set; }
    
    public Color Color { get; set; }

    public Circle(float x, float y, float radius, Color? color = null, Window associatedWindow = null)
    {
        X = x;
        Y = y;
        Radius = radius;
        Color = color ?? Color.White;
        AssociatedWindow = associatedWindow;
        
        Engine.Renderables.Add(this);
    }

    public bool ContainsPoint(float pointX, float pointY)
    {
        var dx = pointX - X;
        var dy = pointY - Y;

        return dx * dx + dy * dy <= Radius * Radius;
    }
    
    void IRenderable.Render(Renderer renderer)
    {
        if (Color.A == 0 || AssociatedWindow == null) return;
        renderer.DrawFillCircle(this, Color);
    }
}