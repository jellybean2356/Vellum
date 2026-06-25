namespace Vellum.Geometry;

public interface IShape
{
    float X { get; set; } // X position on screen
    float Y { get; set; } // Y position on screen
    bool ContainsPoint(float pointX, float pointY); // returns true if the point is inside the shape
    Window LastDrawnWindow { get; set; } // tracks which window the shape was last drawn on
}