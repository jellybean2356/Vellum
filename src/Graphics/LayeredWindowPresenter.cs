namespace Vellum.Graphics;

// NOTE: AI USED INSIDE NATIVE METHODS AND MOST OF THE STUFF RELATED TO WIN32 API

// presents SDLs transparent render target through the native layered window
internal sealed class LayeredWindowPresenter : IDisposable
{
    private IntPtr _dc, _bitmap, _previousBitmap, _pixels;
    private int _width, _height;

    internal unsafe void Present(IntPtr hwnd, IntPtr surfaceHandle)
    {
        var surface = Marshal.PtrToStructure<SDL.Surface>(surfaceHandle);
        Win32Window.Check(GetWindowRect(hwnd, out Win32Rect outer), "GetWindowRect (layered window)");
        var origin = new Win32Point();
        Win32Window.Check(ClientToScreen(hwnd, ref origin), "ClientToScreen (layered window)");
        int width = outer.Right - outer.Left, height = outer.Bottom - outer.Top;
        int offsetX = origin.X - outer.Left, offsetY = origin.Y - outer.Top;
        EnsureBitmap(width, height);
        var destination = new Span<byte>((void*)_pixels, checked(width * height * 4));
        var directCopy = offsetX == 0 && offsetY == 0 && surface.Width == width && surface.Height == height;

        if (!directCopy)
        {
            destination.Clear();
            SendMessage(hwnd, WmPrint, _dc, new IntPtr(PrfNonClient));
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    if (x < offsetX || y < offsetY || x >= offsetX + surface.Width || y >= offsetY + surface.Height)
                        destination[(y * width + x) * 4 + 3] = 255;
        }

        var copyWidth = directCopy ? width : Math.Min(surface.Width, width - offsetX);
        var copyHeight = directCopy ? height : Math.Min(surface.Height, height - offsetY);
        if (offsetX >= 0 && offsetY >= 0 && copyWidth > 0 && copyHeight > 0)
            for (var y = 0; y < copyHeight; y++)
                new ReadOnlySpan<byte>((byte*)surface.Pixels + y * surface.Pitch, copyWidth * 4)
                    .CopyTo(destination.Slice(((y + offsetY) * width + offsetX) * 4, copyWidth * 4));

        var position = new Win32Point { X = outer.Left, Y = outer.Top };
        var size = new Win32Size { Width = width, Height = height };
        var source = new Win32Point();
        var blend = new BlendFunction { ConstantAlpha = 255, AlphaFormat = 1 };
        Win32Window.Check(UpdateLayeredWindow(hwnd, IntPtr.Zero, in position, in size, _dc,
            in source, 0, in blend, UlwAlpha), "UpdateLayeredWindow");
    }

    private void EnsureBitmap(int width, int height)
    {
        if (_bitmap != IntPtr.Zero && _width == width && _height == height) return;
        Dispose();
        _dc = CreateCompatibleDc(IntPtr.Zero);
        if (_dc == IntPtr.Zero) Win32Window.Check(false, "CreateCompatibleDC");
        var info = new BitmapInfo
        {
            Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32,
            SizeImage = checked((uint)(width * height * 4))
        };
        try
        {
            _bitmap = CreateDibSection(_dc, in info, 0, out _pixels, IntPtr.Zero, 0);
            if (_bitmap == IntPtr.Zero) Win32Window.Check(false, "CreateDIBSection");
            _previousBitmap = SelectObject(_dc, _bitmap);
            if (_previousBitmap == IntPtr.Zero || _previousBitmap == new IntPtr(-1))
                Win32Window.Check(false, "SelectObject");
            _width = width; _height = height;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_dc != IntPtr.Zero && _previousBitmap != IntPtr.Zero && _previousBitmap != new IntPtr(-1))
            SelectObject(_dc, _previousBitmap);
        if (_bitmap != IntPtr.Zero) DeleteObject(_bitmap);
        if (_dc != IntPtr.Zero) DeleteDc(_dc);
        _dc = _bitmap = _previousBitmap = _pixels = IntPtr.Zero;
        _width = _height = 0;
    }
}
