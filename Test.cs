using Vellum.UI;
using Vellum.Core;
using Vellum.Graphics;
using Vellum.Geometry;

/*
    DEMO TEST FOR VELLUM. This project WON'T work like it does now. I will be working on DLL soon, this is a testing file and will not be included in the final package
*/

namespace Vellum;
public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;
        
        Color colorState = Color.Red; // draggable + interactable square
        Color colorState2 = Color.Orange; // interactable square

        var startButton = new Draggable<Rect>(new Rect(10, 10, 200, 50)) // draggable square, includes events from interactive
        {
            OnClicked = async void () =>
            {
                Console.WriteLine("Clicked!");
                colorState = Color.Green;
                await Task.Delay(100);
                colorState = Color.Red;
            },
        };
        
        var startButton2 = new Interactive<Rect>(new Rect(500, 500, 200, 50)) // interactable square
        {
            OnClicked = async void () =>
            {
                Console.WriteLine("Clicked!");
                colorState2 = Color.Magenta;
                await Task.Delay(100);
                colorState2 = Color.Orange;
            },
        };

        var rect = new Rect(800, 800, 200, 50); // casual square

        // run the window loop
        while (engine.Update())
        {
            // draw
            engine.DrawFillRect(startButton, colorState);
            engine.DrawFillRect(startButton2, colorState2);
            engine.DrawFillRect(rect, Color.Blue);

            // send the buffer to the screen
            engine.Present();
        }
    }
}