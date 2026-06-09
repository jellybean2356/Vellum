using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;

/*
    Demo test sandbox for vellum, i try stuff out here
*/

namespace Sandbox;

public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;
        
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
            engine.DrawFillCircle(circle, colorState);
            
            // send the buffer to the screen
            engine.Present();
        }
    }
}