using SDL3;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

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
            
            // debug to print all displays
            uint[]? displays = GetScreenDisplayIds();
            if (displays != null)
                foreach (var display in displays)
                    Console.WriteLine($"Detected display id: {display}");
            
            // initial windows log
            List<IntPtr> openWindows = GetWindowsHandles();
            List<IntPtr> openWindowsOld = GetWindowsHandles();
            foreach (IntPtr hwnd in openWindows)
            {
                Console.WriteLine($"found HWND: 0x{hwnd.ToString("X")} | Application: {GetWindowTitle(hwnd)}");
            }

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
                
                openWindows = GetWindowsHandles();
                var difference = openWindows.Except(openWindowsOld).ToList();
                foreach (var hwnd in difference)
                {
                    Console.WriteLine($"found HWND: 0x{hwnd.ToString("X")} | Application: {GetWindowTitle(hwnd)}");
                }
                openWindowsOld = openWindows;
                
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

        // helper function to get screen display ids
        private static uint[]? GetScreenDisplayIds()
        {
            var displays = SDL.GetDisplays(out _);
            return displays;
        }
        
        // helper function to get all visible windows
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private static List<IntPtr> GetWindowsHandles()
        {
            List<IntPtr> visibleWindows = new List<IntPtr>();
            
            // getting primary monitor for now, support for other monitors later
            Point primaryPoint = new Point { X = 0, Y = 0 };
            IntPtr hPrimaryMonitor = MonitorFromPoint(primaryPoint, 2);
            
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;  // skip invisible windows
                if (GetWindowTitle(hWnd).Contains("MainWindowView", StringComparison.OrdinalIgnoreCase)) return true; // skip main window
                
                // skips windows without title
                int titleLength = GetWindowTextLength(hWnd);
                if (titleLength == 0) return true;
                
                // skips windows that are excluded from toolbar
                long exStyle = GetWindowLongPtr(hWnd, GwlExstyle).ToInt64();
                if ((exStyle & WsExToolWindow) != 0) return true;
                
                // checking if window is on primary monitor
                IntPtr hWindowMonitor = MonitorFromWindow(hWnd, 2);
                if (hWindowMonitor != hPrimaryMonitor) return true;

                if (GetWindowRect(hWnd, out Rect rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    if (width <= 0 || height <= 0) return true;
                }
                else
                {
                    return true;
                }
                
                // skips windows that are childs of other windows
                IntPtr owner = GetWindow(hWnd, GwOwner);
                if (owner != IntPtr.Zero && IsWindowVisible(owner)) return true;
                
                // skips windows that are cloaked (windows flagged as visible even though they are invisible)
                int hr = DwmGetWindowAttribute(hWnd, DwmwaIsCloaked, out var cloaked, sizeof(int));
                if (hr == 0 && cloaked != 0) return true;
                
                visibleWindows.Add(hWnd);
                return true; 
            }, IntPtr.Zero);
            
            return visibleWindows;
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length > 0)
            {
                char[] buffer = new char[length + 1];
                GetWindowText(hWnd, buffer, buffer.Length);
                return new string(buffer);
            }
            return string.Empty;
        }
        
        private const int GwlExstyle = -20; // extended window style
        private const uint GwOwner = 3;
        private const uint DwmwaIsCloaked = 14;
        private const long WsExLayered = 0x00080000; // layered window
        private const long WsExTransparent = 0x00000020; // transparent window
        private const long WsExToolWindow = 0x00000080; // hides the window icon from toolbar
        
        // importing user32.dll functions
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
            
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindowVisible(IntPtr hWnd);
        
        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
        private static partial int GetWindowTextLength(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        
        [LibraryImport("dwmapi.dll")]
        private static partial int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out int pvAttribute, int cbAttribute);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
        
        [LibraryImport("user32.dll")]
        private static partial IntPtr MonitorFromPoint(Point pt, uint dwFlags);

        [LibraryImport("user32.dll")]
        private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
    }
}