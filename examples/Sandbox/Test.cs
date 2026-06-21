using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;
using Vellum.Platform;

namespace Sandbox;

public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;

        // create windows
        using var window1 = Window.Create("Vellum Engine1", 500, 500, 0);
        using var window2 = Window.Create("Vellum Engine2", 500, 500, 0);
        using var window3 = Window.Create("Vellum Engine3", 500, 500, 0);
        using var window4 = Window.Create("Vellum Engine4", 500, 500, 0);

        // --- window 1: draggable circle---
        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f, Color.Red, window1));
        circle.OnClicked = async void () =>
        {
            circle.Shape.Color = Color.Blue;
            await Task.Delay(100);
            circle.Shape.Color = Color.Red;
        };

        // --- window 2: interactive rect ---
        var rect = new Interactive<Rect>(new Rect(100f, 100f, 100f, 100f, Color.Green, window2));
        rect.OnClicked = async void () =>
        {
            rect.Shape.Color = Color.Blue;
            await Task.Delay(100);
            rect.Shape.Color = Color.Green;
        };
        
        // --- window 3: draggable circle 2 ---
        var circle2 = new Draggable<Circle>(new Circle(100f, 100f, 50f, Color.Blue, window3));
        circle2.OnClicked = async void () =>
        {
            circle2.Shape.Color = Color.White;
            await Task.Delay(100);
            circle2.Shape.Color = Color.Blue;
        };

        // --- window 4: interactive rect 2 ---
        var rect2 = new Interactive<Rect>(new Rect(100f, 100f, 100f, 100f, Color.Yellow, window4));
        rect2.OnClicked = async void () =>
        {
            rect2.Shape.Color = Color.Magenta;
            await Task.Delay(100);
            rect2.Shape.Color = Color.Yellow;
        };
        
        engine.Run();
    }
}