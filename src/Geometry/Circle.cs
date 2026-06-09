namespace Vellum.Geometry;

public class Circle(float x, float y, float radius) : IShape
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Radius { get; set; } = radius;

    public bool ContainsPoint(float pointX, float pointY)
    {
        var dx = pointX - X;
        var dy = pointY - Y;

        return dx * dx + dy * dy <= Radius * Radius;
    }
}