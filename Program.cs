using SDL3;
using System.Runtime.InteropServices;

namespace Vellum
{
    partial class Program
    {
        [STAThread]
        private static void Main()
        {
            var windowFlags = SDL.WindowFlags.Transparent |
                              SDL.WindowFlags.Borderless |
                              SDL.WindowFlags.Fullscreen |
                              SDL.WindowFlags.AlwaysOnTop;
            
            // initialize SDL
            if (!SDL.Init(SDL.InitFlags.Video))
            {
                SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize! SDL_Error: {SDL.GetError()}");
                
                return;
            }

            // create window
            SDL.SetHint(SDL.Hints.RenderDriver, "software");
            var window = SDL.CreateWindow("Vellum", 800, 600, windowFlags);
            if (window == IntPtr.Zero)
            {
                SDL.LogError(SDL.LogCategory.Application, $"SDL could not create window! SDL_Error: {SDL.GetError()}");
                return;
            }
            
            // create renderer
            var renderer = SDL.CreateRenderer(window,"software");
            if (renderer == IntPtr.Zero)
            {
                SDL.LogError(SDL.LogCategory.Application, $"SDL could not create renderer! SDL_Error: {SDL.GetError()}");
                return;
            }
            
            MakeClickTrough(window);
            
            // run the window loop
            var loop = true;
            while (loop)
            {
                while (SDL.PollEvent(out var e))
                {
                    if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                    {
                        loop = false;
                    }
                }
                
                SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.None);
                SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
                SDL.RenderClear(renderer);
                
                SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend);
                SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
                SDL.FRect rect = new SDL.FRect
                {
                    X = 50,
                    Y = 50,
                    W = 100,
                    H = 10
                };
                SDL.RenderFillRect(renderer, rect);
                
                SDL.RenderPresent(renderer);
            }
            
            SDL.DestroyRenderer(renderer);
            SDL.DestroyWindow(window);
            SDL.Quit();
        }
        
        private static void MakeClickTrough(IntPtr window)
        {
            var props = SDL.GetWindowProperties(window);
            var hwnd = SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }
            
            var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
            var newStyle = new IntPtr(exStyle | WsExLayered | WsExTransparent);
            _ = SetWindowLongPtr(hwnd, GwlExstyle, newStyle);
            SetWindowLongPtr(hwnd, GwlExstyle, newStyle);
        }
        
        private const int GwlExstyle = -20;
        private const long WsExLayered = 0x00080000;
        private const long WsExTransparent = 0x00000020;
        
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
            
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}