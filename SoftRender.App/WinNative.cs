using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public static class WinNative
    {
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
    }
}
