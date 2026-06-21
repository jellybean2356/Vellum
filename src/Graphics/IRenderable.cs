namespace Vellum.Graphics;

public interface IRenderable
{
    Window AssociatedWindow { get; set; }
    void Render(Renderer renderer);
}