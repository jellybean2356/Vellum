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

        using var overlay = Window.CreateOverlay("VellumEngine");
        
        var colorState = Color.Red;

        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f))
        {
            OnClicked = async void () =>
            {
                colorState = Color.Blue;
                await Task.Delay(100);
                colorState = Color.Red;
            },
        };

        // run the window loop
        while (engine.Update())
        {
            // draw
            overlay.Renderer.DrawFillCircle(circle, colorState);
            
            // send the buffer to the screen
            overlay.Renderer.Present();
        }
    }
}