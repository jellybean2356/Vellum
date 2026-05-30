using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Vellum
{
    partial class Program
    {
        [STAThread]
        private static void Main()
        {
            const SDL.WindowFlags windowFlags = SDL.WindowFlags.Transparent |
                                                SDL.WindowFlags.Borderless |
                                                SDL.WindowFlags.Fullscreen |
                                                SDL.WindowFlags.AlwaysOnTop |
                                                SDL.WindowFlags.NotFocusable;
            
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
            
            MakeLayered(window);
            uint[]? displays = GetScreenDisplayIds();
            if (displays != null)
                foreach (var display in displays)
                    Console.WriteLine($"Detected display id: {display}");

            SDL.FRect[] interactiveParts =
            [
                new() { X = 50, Y = 50, W = 100, H = 100 },
                new() { X = 1820, Y = 50, W = 100, H = 100 },
                new() { X = 50, Y = 980, W = 100, H = 100 }
            ];
            
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

                UpdateClickTrough(window, interactiveParts);
                
                SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.None);
                SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
                SDL.RenderClear(renderer);
                
                SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend);
                SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
                foreach (var interactivePart in interactiveParts)
                {
                    SDL.RenderFillRect(renderer, interactivePart);
                }
                
                SDL.RenderPresent(renderer);
            }
            
            SDL.DestroyRenderer(renderer);
            SDL.DestroyWindow(window);
            SDL.Quit();
        }
        
        // helper method to obrain hwnd
        private static IntPtr GetHwnd(IntPtr window)
        {
            var props = SDL.GetWindowProperties(window);
            return SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
        }
        
        // make window layered and transpparent at start, fully clickable trough at start
        private static bool _clickThrough;
        private static void MakeLayered(IntPtr window)
        {
            // get hwnd
            var hwnd = GetHwnd(window);
            if (hwnd == IntPtr.Zero)
                return;
            
            // set ExStyle to layered and transparent
            var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
            _ = SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(exStyle | WsExLayered | WsExTransparent | WsExToolWindow));
            _clickThrough = true;
        }

        // partial click-through handler (NOT REQUIRED)
        private static void UpdateClickTrough(IntPtr window, SDL.FRect[] interactiveParts)
        {
            // get hwnd
            var hwnd = GetHwnd(window);
            if (hwnd == IntPtr.Zero)
                return;
            
            _ = SDL.GetGlobalMouseState(out var gx, out var gy); // global cursor position
            SDL.GetWindowPosition(window, out var wx, out var wy); // window position
            
            // getting window-local coordinates
            var localX = gx - wx;
            var localY = gy - wy;
            
            // check if cursor is over interactive areas
            var overInteractive = false;
            foreach (var interactivePart in interactiveParts)
            {
                if (localX >= interactivePart.X && localX <= interactivePart.X + interactivePart.W &&
                    localY >= interactivePart.Y && localY <= interactivePart.Y + interactivePart.H)
                {
                    overInteractive = true;
                    break;
                }
            }
            
            var wantClickTrough = !overInteractive;
            if (wantClickTrough == _clickThrough)
                return;

            _clickThrough = wantClickTrough;
            
            // toggles transperent exstyle based on click-through state
            var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
            if (wantClickTrough)
                exStyle |= WsExTransparent;
            else
                exStyle &= ~WsExTransparent;
            
            _ = SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(exStyle));
        }

        private static uint[]? GetScreenDisplayIds()
        {
            unsafe
            {
                uint[]? displays = SDL.GetDisplays(out int count);
                return displays;
            }
        }
        
        private const int GwlExstyle = -20; // extended window style
        private const long WsExLayered = 0x00080000; // layered window
        private const long WsExTransparent = 0x00000020; // transparent window
        private const long WsExToolWindow = 0x00000080; // hides the window icon from toolbar
        
        // importing user32.dll functions
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
            
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}