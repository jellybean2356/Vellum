namespace Vellum.Graphics;

public class Renderer : IDisposable
{
    internal IntPtr Handle{get; private set;}
    public bool IsValid => Handle != IntPtr.Zero;
    public Window Window { get; internal init; }
    private IntPtr _alphaTarget;
    private int _targetWidth, _targetHeight;
    private LayeredWindowPresenter _layeredPresenter;

    private Renderer(IntPtr handle)
    {
        Handle = handle;
    }

    // create renderer
    public static Renderer Create(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        ObjectDisposedException.ThrowIf(!window.IsValid, window);
        var properties = SDL.CreateProperties();
        if (properties == 0)
            throw new InvalidOperationException($"SDL_CreateProperties failed: {SDL.GetError()}");

        try
        {
            // this is the wrapped SDL_Window*, not window.Hwnd
            if (!SDL.SetPointerProperty(properties, SDL.Props.RendererCreateWindowPointer, window.Handle) ||
                !SDL.SetStringProperty(properties, SDL.Props.RendererCreateNameString, "software"))
                throw new InvalidOperationException($"Setting native renderer properties failed: {SDL.GetError()}");

            var handle = SDL.CreateRendererWithProperties(properties);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException($"SDL_CreateRendererWithProperties failed: {SDL.GetError()}");

            return new Renderer(handle) { Window = window };
        }
        finally
        {
            SDL.DestroyProperties(properties);
        }
    }

    // clear the buffer
    public void Clear(Color color)
    {
        if (!BeginFrame()) return;
        // cache blend mode and clear buffer with color
        SDL.GetRenderDrawBlendMode(Handle, out var blendMode);
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.None);
        if (Window.UsesLayeredPresentation)
            SDL.SetRenderDrawColor(Handle, (byte)(color.R * color.A / 255),
                (byte)(color.G * color.A / 255), (byte)(color.B * color.A / 255), color.A);
        else
            SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderClear(Handle);
        
        // restore blend mode
        SDL.SetRenderDrawBlendMode(Handle, blendMode);
    }
    
    // send the buffer to the screen
    public void Present()
    {
        if (Window.UsesLayeredPresentation)
        {
            var surface = SDL.RenderReadPixels(Handle, null);
            if (surface == IntPtr.Zero)
                throw new InvalidOperationException($"Reading the SDL overlay frame failed: {SDL.GetError()}");
            var converted = IntPtr.Zero;
            try
            {
                var data = Marshal.PtrToStructure<SDL.Surface>(surface);
                if (data.Format != SDL.PixelFormat.ARGB8888)
                {
                    converted = SDL.ConvertSurface(surface, SDL.PixelFormat.ARGB8888);
                    if (converted == IntPtr.Zero)
                        throw new InvalidOperationException($"Converting the SDL overlay frame failed: {SDL.GetError()}");
                }
                (_layeredPresenter ??= new LayeredWindowPresenter()).Present(Window.Hwnd,
                    converted != IntPtr.Zero ? converted : surface);
            }
            finally
            {
                if (converted != IntPtr.Zero) SDL.DestroySurface(converted);
                SDL.DestroySurface(surface);
            }
            return;
        }
        SDL.RenderPresent(Handle);
    }

    internal bool BeginFrame()
    {
        if (!IsValid || !Window.IsValid) return false;
        if (!IsWindowVisible(Window.Hwnd) || IsIconic(Window.Hwnd)) return false;
        if (!Window.UsesLayeredPresentation) return true;
        Win32Window.Check(GetClientRect(Window.Hwnd, out Win32Rect rect), "GetClientRect (overlay target)");
        int width = rect.Right, height = rect.Bottom;
        if (width <= 0 || height <= 0) return false;
        if (_alphaTarget == IntPtr.Zero || width != _targetWidth || height != _targetHeight)
        {
            if (!SDL.SetRenderTarget(Handle, IntPtr.Zero))
                throw new InvalidOperationException($"Resetting the SDL overlay target failed: {SDL.GetError()}");
            if (_alphaTarget != IntPtr.Zero) SDL.DestroyTexture(_alphaTarget);
            _alphaTarget = IntPtr.Zero;
            _alphaTarget = SDL.CreateTexture(Handle, SDL.PixelFormat.ARGB8888, SDL.TextureAccess.Target, width, height);
            if (_alphaTarget == IntPtr.Zero)
                throw new InvalidOperationException($"Creating the SDL overlay target failed: {SDL.GetError()}");
            _targetWidth = width; _targetHeight = height;
        }
        if (!SDL.SetRenderTarget(Handle, _alphaTarget))
            throw new InvalidOperationException($"Selecting the SDL overlay target failed: {SDL.GetError()}");
        return true;
    }
    
    // draw function for full rectangle
    public void DrawFillRect(Rect rect, Color color)
    {
        rect.LastDrawnWindow = this.Window;
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderFillRect(Handle, rect);
    }
    
    // draw function for full circle
    public void DrawFillCircle(Circle circle, Color color)
    {
        circle.LastDrawnWindow = this.Window;
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);

        var cx = (int)circle.X;
        var cy = (int)circle.Y;
        var r = (int)circle.Radius;

        // scan vertically and draw horizontal lines across the width of the circle
        for (var y = -r; y <= r; y++)
        {
            var width = (int)MathF.Sqrt(r * r - y * y);
            SDL.RenderLine(Handle, cx - width, cy + y, cx + width, cy + y);
        }
    }
    
    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            if (_alphaTarget != IntPtr.Zero)
            {
                SDL.SetRenderTarget(Handle, IntPtr.Zero);
                SDL.DestroyTexture(_alphaTarget);
                _alphaTarget = IntPtr.Zero;
            }
            _layeredPresenter?.Dispose();
            _layeredPresenter = null;
            SDL.DestroyRenderer(Handle);
            Handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
