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
        var circle = new Draggable<Circle>(new Circle(100f, 100f, 50f, Color.Red, overlay));
        circle.OnClicked = async void () =>
        {
            circle.Shape.Color = Color.Blue;
            await Task.Delay(100);
            circle.Shape.Color = Color.Red;
        };
        
        // make textures
        var texture = new Draggable<Texture>(new Texture("assets/texture.png", 100, 100, overlay, 200, 200));
        var gif = new Draggable<Texture>(new Texture("assets/gif.gif", 100, 100, overlay));
        var gif2 = new Draggable<Texture>(new Texture("assets/gif2.gif", 500, 200, overlay, animationSpeed: 1f));
        
        engine.OnUpdate = Update;
        engine.Run();
    }

    private static void Update()
    {
        // update loop
    }
}
