using System;
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
        
        var colorState = Color.Red; // draggable + interactable square
        var colorState2 = Color.Orange; // interactable square

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