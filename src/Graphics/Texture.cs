namespace Vellum.Graphics;

using StbImageSharp;
using SkiaSharp;

// this file is temporary, animated texture will be soon separated from classic texture
public class Texture : IShape, IRenderable, IUpdatable, IDisposable
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

    // animation handling
    private readonly bool _isAnimated;
    private readonly List<byte[]> _gifFrames = [];
    private readonly List<float> _gifDelaysInSeconds = [];
    private int _currentFrameIndex;
    private float _frameTimer;
    private float _animationSpeed = 1f;

    public float AnimationSpeed
    {
        get => _animationSpeed;
        init
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Animation speed must be a finite value greater than or equal to zero.");
            }

            _animationSpeed = value;
        }
    }

    public Texture(string path, float x, float y, Window window, float? w = null, float? h = null,
        float animationSpeed = 1f)
    {
        AssociatedWindow = window;
        AnimationSpeed = animationSpeed;
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Vellum Texture Error: File not found at '{path}'");
        }
        
        _isAnimated = path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        
        if (_isAnimated)  // if the texture is a GIF
        {
            using (var codec = SKCodec.Create(path))
            {
                if (codec == null)
                {
                    throw new Exception($"Vellum Texture Error: Skia failed to read GIF file structure at '{path}'");
                }

                OriginalWidth = codec.Info.Width;
                OriginalHeight = codec.Info.Height;
                
                var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using (var bitmap = new SKBitmap(info))
                using (var canvas = new SKCanvas(bitmap))
                {
                    var frameCount = codec.FrameCount;
                    var frameInfos = codec.FrameInfo;

                    for (var i = 0; i < frameCount; i++)
                    {
                        var frameInfo = frameInfos[i];
                        if (frameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestoreBackgroundColor)
                        {
                            canvas.Clear(SKColors.Transparent);
                        }
                        
                        var options = new SKCodecOptions(i);
                        codec.GetPixels(info, bitmap.GetPixels(), options);
                        
                        var pixelBytes = bitmap.Bytes;
                        _gifFrames.Add(pixelBytes);
                        
                        var delay = frameInfo.Duration / 1000f;
                        _gifDelaysInSeconds.Add(delay > 0 ? delay : 0.1f);
                    }
                }
            }

            var finalW = w ?? OriginalWidth;
            var finalH = h ?? OriginalHeight;
            DstRect = new Rect(x, y, finalW, finalH);
            
            Handle = SDL.CreateTexture(
                window.Renderer.Handle, 
                SDL.PixelFormat.ARGB8888, 
                SDL.TextureAccess.Streaming, 
                (int)OriginalWidth, 
                (int)OriginalHeight
            );
            
            if (_gifFrames.Count > 0)
            {
                UpdateGpuTextureFrame(_gifFrames[0]); 
            }
        }
        else // static texture handling
        {
            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            
            OriginalWidth = image.Width;
            OriginalHeight = image.Height;

            var finalW = w ?? image.Width;
            var finalH = h ?? image.Height;
            DstRect = new Rect(x, y, finalW, finalH);
            
            UpdateStaticTexture(image);
        }

        if (Handle == IntPtr.Zero)
        {
            throw new Exception($"Vellum Texture Error: Failed to compile GPU texture resource. SDL: {SDL.GetError()}");
        }
        
        SDL.SetTextureBlendMode(Handle, SDL.BlendMode.Blend);
        
        Engine.Renderables.Add(this);
        if (_isAnimated)
        {
            Engine.Updatables.Add(this);
        }
    }
    
    public bool ContainsPoint(float pointX, float pointY)
    {
        return DstRect.ContainsPoint(pointX, pointY);
    }

    public void Update(float deltaTime)
    {
        if (!_isAnimated || _gifFrames.Count <= 1 || AnimationSpeed <= 0f) return;

        _frameTimer += deltaTime * AnimationSpeed;

        if (!(_frameTimer >= _gifDelaysInSeconds[_currentFrameIndex])) return;
        
        while (_frameTimer >= _gifDelaysInSeconds[_currentFrameIndex])
        {
            _frameTimer -= _gifDelaysInSeconds[_currentFrameIndex];
            _currentFrameIndex = (_currentFrameIndex + 1) % _gifFrames.Count;
        }
            
        UpdateGpuTextureFrame(_gifFrames[_currentFrameIndex]);
    }

    public void Render(Renderer renderer)
    {
        if (Handle == IntPtr.Zero) return;
        LastDrawnWindow = renderer.Window;

        SDL.FRect nativeSrc = new() { X = 0f, Y = 0f, W = OriginalWidth, H = OriginalHeight };
        SDL.FRect nativeDst = new() { X = this.X, Y = this.Y, W = this.W, H = this.H };

        SDL.RenderTexture(renderer.Handle, Handle, in nativeSrc, in nativeDst);
    }

    private void UpdateGpuTextureFrame(byte[] pixelData)
    {
        var pitch = (int)OriginalWidth * 4; 
        SDL.UpdateTexture(Handle, IntPtr.Zero, pixelData, pitch);
    }

    private void UpdateStaticTexture(ImageResult image)
    {
        unsafe
        {
            fixed (byte* pixelPtr = image.Data)
            {
                var pitch = image.Width * 4;
                var surface = SDL.CreateSurfaceFrom(
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
                
                Handle = SDL.CreateTextureFromSurface(AssociatedWindow.Renderer.Handle, surface);
                SDL.DestroySurface(surface);
            }
        }
    }

    public void Dispose()
    {
        if (_isAnimated)
        {
            Engine.Updatables.Remove(this);
        }
        Engine.Renderables.Remove(this);

        if (Handle == IntPtr.Zero) return;
        SDL.DestroyTexture(Handle);
        Handle = IntPtr.Zero;
    }
}
