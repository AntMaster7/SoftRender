using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public static class WinNative
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
    }
}
