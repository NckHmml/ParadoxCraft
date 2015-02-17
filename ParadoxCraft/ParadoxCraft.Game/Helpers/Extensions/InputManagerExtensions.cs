using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft
{
    public static class InputManagerExtensions
    {
        public static bool ResetMousePosition(this InputManager inputManager)
        {
            Size2 clientSize = inputManager.Game.Window.ClientBounds.Size;
            IntPtr gamehandle = (inputManager.Game.Window.NativeWindow.NativeHandle as dynamic).Handle;
            IntPtr activehandle = GetActiveWindow();
            if (activehandle != gamehandle) return false;
            Point point = new Point(clientSize.Width / 2, clientSize.Height / 2);
            ClientToScreen(activehandle, ref point);
            SetCursorPos(point.X, point.Y);
            return true;
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", EntryPoint = "ClientToScreen")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point position);

        [DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
        private static extern IntPtr GetActiveWindow();
    }
}
