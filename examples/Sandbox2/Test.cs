using System.Threading.Tasks;
using Vellum.UI;
using Vellum.Geometry;
using Vellum.Graphics;
using Vellum.Core;
using Vellum.Platform;

/*
    Demo test sandbox for vellum, I try stuff out here
*/

namespace Sandbox2;

public class Test
{
    private static void Main()
    {
        // initialize engine
        using var engine = new Engine();
        if (!engine.Initialize()) return;

        engine.ClampToDisplayRefreshRate = true;

        // create overlay
        using var overlay = Window.CreateOverlay("VellumEngine", WindowFlags.ForceTopmost);

        // make a draggable circle
        var circle = Engine.Add(new Draggable<Circle>(new Circle(100f, 100f, 50f, Color.Red, overlay)));
        circle.OnClicked = async void () =>
        {
            circle.Color = Color.Blue;
            await Task.Delay(100);
            circle.Color = Color.Red;
        };
        
        // make textures
        Engine.Add(new Draggable<Texture>(new Texture("assets/texture.png", 100, 100, overlay, 200, 200)));
        var gif2 = Engine.Add(new Draggable<Texture>(new Texture("assets/gif2.gif", 200, 200, overlay)));
        Engine.Add(new Draggable<Texture>(new Texture("assets/gif.gif", 100, 100, overlay)));

        // test percentage and auto
        Engine.Add(new Draggable<Texture>(Texture.Create("assets/texture.png", 300, 300, overlay, w: "110%", h: "auto")));

        gif2.Color = new Color(255, 100, 255, 255);
        
        engine.OnUpdate = Update;
        engine.Run();
    }

    private static void Update()
    {
        // update loop
    }
}
