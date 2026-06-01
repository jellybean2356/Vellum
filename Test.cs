using SDL3;

namespace Vellum;

public class Test
{
    private static void Main()
    {
        // initialize SDL
        if(!Window.Initialize()) return;
        var (window, renderer) = Window.CreateOverlay();
        
        // clickable parts of the overlay
        SDL.FRect[] interactiveParts =
        [
            new() { X = 50, Y = 50, W = 100, H = 100 },
            new() { X = 1820, Y = 50, W = 100, H = 100 },
            new() { X = 50, Y = 980, W = 100, H = 100 }
        ];
        
        // run the window loop
        var loop = true;
        while (loop)
        {
            loop = Window.ProcessEvents();
            if (!loop) break;
            
            // clean the background
            SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.None);
            SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
            SDL.RenderClear(renderer);
            
            // draw test squares
            SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend);
            SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
            
            foreach (var interactivePart in interactiveParts)
            {
                SDL.RenderFillRect(renderer, interactivePart);
            }
            
            // update input for clickable parts and draw debug windows
            Window.UpdateInput(window, interactiveParts);
            Window.DebugWindows(window, renderer);
            
            SDL.RenderPresent(renderer);
        }
        
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }
}