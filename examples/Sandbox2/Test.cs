using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;
using Vellum.Platform;

/*
    Demo test sandbox for vellum, i try stuff out here
*/

namespace Sandbox2;

public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;

        // create overlay
        using var overlay = Window.CreateOverlay("VellumEngine");

        // make a draggable circle
        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f, Color.Red, overlay));
        circle.OnClicked = async void () =>
        {
            circle.Shape.Color = Color.Blue;
            await Task.Delay(100);
            circle.Shape.Color = Color.Red;
        };
        
        engine.Run();
    }
}