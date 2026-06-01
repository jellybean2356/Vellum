using SDL3;
namespace Vellum;

public class Engine : IDisposable
{
    // private variables
    private IntPtr _window;
    private IntPtr _renderer;
    private bool _isInitialized;
    private bool _isRunning;
    
    private SDL.FRect[] _clickBounds = [];

    // initialize the engine
    public bool Initialize()
    {
        if (_isInitialized) return true;
        if (!Window.Initialize()) return false; // will be combined later, for now it's a little messy
        
        var (window, renderer) = Window.CreateOverlay();
        _window = window;
        _renderer = renderer;
        
        if (_window == IntPtr.Zero || _renderer == IntPtr.Zero) return false;
        _isInitialized = true;
        _isRunning = true;
        return true;
    }

    // update loop
    public bool Update()
    {
        if (!_isRunning) return false;

        while (SDL.PollEvent(out var e))
        {
            if ((SDL.EventType)e.Type != SDL.EventType.Quit) continue;
            
            _isRunning = false;
            return false;
        }
        
        Window.UpdateInput(_window, _clickBounds);
        
        SDL.SetRenderDrawBlendMode(_renderer, SDL.BlendMode.None);
        SDL.SetRenderDrawColor(_renderer, 0, 0, 0, 0);
        SDL.RenderClear(_renderer);
        
        return true;
    }

    // allows the user to set what objects should register inputs (makes certain objects not click trough)
    public void SetInteractiveRegions(IEnumerable<Rect> regions)
    {
        _clickBounds = regions.Select(r => r.ToSdl()).ToArray();
    }

    // draw function for full rectangle
    public void DrawFillRect(Rect rect, Color color)
    {
        var sdlRect = rect.ToSdl();
        SDL.SetRenderDrawBlendMode(_renderer, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL.RenderFillRect(_renderer, in sdlRect);
    }

    // send the buffer to the screen
    public void Present()
    {
        SDL.RenderPresent(_renderer);
    }
    
    // dispose of the engine resources
    public void Dispose()
    {
        if (_renderer != IntPtr.Zero) SDL.DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero) SDL.DestroyWindow(_window);
        
        Window.ClearActiveHandles();
        
        SDL.Quit();
        GC.SuppressFinalize(this);
    }
}