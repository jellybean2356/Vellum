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
        
        var interactiveBox = new Rect(100, 100, 150, 150);
        bool isDragging = false;
        
        float offsetX = 0;
        float offsetY = 0;

        // click configuration
        long blinkEndTime = 0;
        const long blinkDurationMs = 50;

        // run the window loop
        while (engine.Update())
        {
            bool isHovering = Input.MouseX >= interactiveBox.X && Input.MouseX <= interactiveBox.X + interactiveBox.W &&
                              Input.MouseY >= interactiveBox.Y && Input.MouseY <= interactiveBox.Y + interactiveBox.H;
            
            // when dragging the box
            if (isHovering && Input.IsMouseDragging(MouseButton.Left) && !isDragging)
            {
                isDragging = true;
                offsetX = Input.MouseX - interactiveBox.X;
                offsetY = Input.MouseY - interactiveBox.Y;
            }
            
            if (isDragging)
            {
                interactiveBox.X = Input.MouseX - offsetX;
                interactiveBox.Y = Input.MouseY - offsetY;

                if (Input.WasMouseReleased(MouseButton.Left))
                {
                    isDragging = false;
                }
            }

            // when clicked on the box
            if (isHovering && Input.WasMouseClicked(MouseButton.Left))
            {
                blinkEndTime = Environment.TickCount64 + blinkDurationMs;
            }
            
            List<Rect> interactiveAreas = [ interactiveBox ];
            engine.SetInteractiveRegions(interactiveAreas);
            
            // render the square with different colors based on the state
            Color boxColor;
            if (isDragging)
            {
                boxColor = Color.Green; // green when moving
            }
            else if (Environment.TickCount64 < blinkEndTime)
            {
                boxColor = Color.Orange; // orange flash when clicked
            }
            else
            {
                boxColor = Color.Red; // red default
            }

            engine.DrawFillRect(interactiveBox, boxColor);
            
            Window.DrawDebugWindows();
            engine.Present();
        }
    }
}