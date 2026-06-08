namespace Vellum.Geometry;

public interface IShape
{
    float X { get; set; } // X position on screen
    float Y { get; set; } // Y position on screen
    bool ContainsPoint(float pointX, float pointY); // returns true if the point is inside the shape
}