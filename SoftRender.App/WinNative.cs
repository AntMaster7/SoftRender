using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public static class WinNative
    {
        public const int KEY_PRESSED = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)] // https://pinvoke.net/default.aspx/user32/PeekMessage.html
        public static extern bool PeekMessage(out NativeMessage lpMsg, HandleRef hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(Keys nVirtKey);
    }
}
