namespace Vellum.Graphics;

public interface IRenderable
{
    bool IsEnabled { get; set; }
    Window AssociatedWindow { get; set; }
    void Render(Renderer renderer);
}