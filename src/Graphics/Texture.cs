namespace Vellum.Graphics;

using System;
using System.IO;
using StbImageSharp;
using SDL3;
using Vellum.Geometry;

public class Texture : IShape, IRenderable, IDisposable
{
    public IntPtr Handle { get; private set; }

    public Rect DstRect { get; private set; }

    public float OriginalWidth { get; private set; }
    public float OriginalHeight { get; private set; }

    public float X { get => DstRect.X; set => DstRect.X = value; }
    public float Y { get => DstRect.Y; set => DstRect.Y = value; }
    public float W { get => DstRect.W; set => DstRect.W = value; }
    public float H { get => DstRect.H; set => DstRect.H = value; }
    
    public Window AssociatedWindow { get; set;}
    public Window LastDrawnWindow { get; set; }

    public Texture(string path, float x, float y, Window window, float? w = null, float? h = null)
    {
        AssociatedWindow = window;
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Vellum Texture Error: File not found at '{path}'");
        }
        
        using var stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        
        OriginalWidth = image.Width;
        OriginalHeight = image.Height;

        float finalW = w ?? image.Width;
        float finalH = h ?? image.Height;

        DstRect = new Rect(x, y, finalW, finalH);
        
        unsafe
        {
            fixed (byte* pixelPtr = image.Data)
            {
                int pitch = image.Width * 4;
                
                IntPtr surface = SDL.CreateSurfaceFrom(
                    image.Width, 
                    image.Height, 
                    SDL.PixelFormat.ABGR8888, 
                    (IntPtr)pixelPtr, 
                    pitch
                );

                if (surface == IntPtr.Zero)
                {
                    throw new Exception($"Vellum Texture Error: Failed to wrap pixel array. SDL: {SDL.GetError()}");
                }
                
                Handle = SDL.CreateTextureFromSurface(window.Renderer.Handle, surface);
                SDL.DestroySurface(surface);
            }
        }

        if (Handle == IntPtr.Zero)
        {
            throw new Exception($"Vellum Texture Error: Failed to compile GPU texture resource. SDL: {SDL.GetError()}");
        }
        
        SDL.SetTextureBlendMode(Handle, SDL.BlendMode.Blend);
        Engine.Renderables.Add(this);
    }
    
    public bool ContainsPoint(float pointX, float pointY)
    {
        return DstRect.ContainsPoint(pointX, pointY);
    }

    public void Render(Renderer renderer)
    {
        if (Handle == IntPtr.Zero) return;

        SDL.FRect nativeSrc = new() { X = 0f, Y = 0f, W = OriginalWidth, H = OriginalHeight };
        SDL.FRect nativeDst = new() { X = this.X, Y = this.Y, W = this.W, H = this.H };

        SDL.RenderTexture(renderer.Handle, Handle, in nativeSrc, in nativeDst);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyTexture(Handle);
            Handle = IntPtr.Zero;
        }
    }
}