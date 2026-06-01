namespace Vellum;

/*
    DEMO TEST FOR VELLUM. This project WON'T work like it does now. I will be working on DLL soon, this is a testing file and will not be included in the final package
*/

public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;

        // clickable parts of the overlay
        List<Rect> clickableParts =
        [
            new(50, 50, 100, 100),
            new(1820, 50, 100, 100),
            new(50, 980, 100, 100)
        ];
        
        engine.SetInteractiveRegions(clickableParts);

        // run the window loop
        while (engine.Update())
        {
            foreach (var part in clickableParts)
            {
                engine.DrawFillRect(part, new Color(255, 0, 0, 255));
            }
            
            Window.DrawDebugWindows();

            engine.Present();
        }
    }
}