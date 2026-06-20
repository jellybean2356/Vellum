using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;
using Vellum.Platform;

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

        // start window and overlay
        using var window2 = Window.Create("Vellum Engine2", 500, 500, 0);
        using var window3 = Window.Create("Vellum Engine3", 500, 500, 0);
        using var window4 = Window.Create("Vellum Engine4", 500, 500, 0);
        using var window1 = Window.Create("Vellum Engine1", 500, 500, 0);
        
        var colorState = Color.Red;
        var colorState2 = Color.Green;
        var colorState3 = Color.Blue;
        var colorState4 = Color.Yellow;

        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f))
        {
            OnClicked = async void () =>
            {
                colorState = Color.Blue;
                await Task.Delay(100);
                colorState = Color.Red;
            },
        };

        var rect = new Interactive<Rect>(new Rect(100f, 100f, 100f, 100f))
        {
            OnClicked = async void () =>
            {
                colorState2 = Color.Blue;
                await Task.Delay(100);
                colorState2 = Color.Green;
            },
        };
        
        var circle2 = new Draggable<Circle>(new Circle(100f, 100f, 50f))
        {
            OnClicked = async void () =>
            {
                colorState3 = Color.White;
                await Task.Delay(100);
                colorState3 = Color.Blue;
            },
        };

        var rect2 = new Interactive<Rect>(new Rect(100f, 100f, 100f, 100f))
        {
            OnClicked = async void () =>
            {
                colorState4 = Color.Magenta;
                await Task.Delay(100);
                colorState4 = Color.Yellow;
            },
        };

        // run the window loop
        while (engine.Update())
        {
            // draw using the window's renderer
            window1.Renderer.DrawFillCircle(circle, colorState);
            window2.Renderer.DrawFillRect(rect, colorState2);
            window3.Renderer.DrawFillCircle(circle2, colorState3);
            window4.Renderer.DrawFillRect(rect2, colorState4);
            
            // send the buffer to the screen
            window1.Renderer.Present();
            window2.Renderer.Present();
            window3.Renderer.Present();
            window4.Renderer.Present();
        }
    }
}