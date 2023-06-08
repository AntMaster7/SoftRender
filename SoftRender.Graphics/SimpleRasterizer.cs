using System.Drawing;
using System.Runtime.CompilerServices;

namespace SoftRender
{
    public unsafe class SimpleRasterizer : IRasterizer
    {
        private const byte BytesPerPixel = 3;

        public int Dummy = 0;

        private readonly byte* framebuffer;
        private readonly int stride;

        public SimpleRasterizer(byte* framebuffer, int stride)
        {
            this.framebuffer = framebuffer;
            this.stride = stride;
        }

        public unsafe void Rasterize(Point[] face)
        {
            var l = System.Math.Min(System.Math.Min(face[0].X, face[1].X), face[2].X);
            var r = System.Math.Max(System.Math.Max(face[0].X, face[1].X), face[2].X);
            var t = System.Math.Min(System.Math.Min(face[0].Y, face[1].Y), face[2].Y);
            var b = System.Math.Max(System.Math.Max(face[0].Y, face[1].Y), face[2].Y);

            var aabb = new Rectangle(l, t, r - l, b - t);

            var e1x = -(face[1].X - face[0].X);
            var e2x = -(face[2].X - face[1].X);
            var e3x = -(face[0].X - face[2].X);
            var e1y = -(face[1].Y - face[0].Y);
            var e2y = -(face[2].Y - face[1].Y);
            var e3y = -(face[0].Y - face[2].Y);

            var f1 = e1x * (aabb.Y - face[0].Y) - e1y * (aabb.X - face[0].X);
            var f2 = e2x * (aabb.Y - face[1].Y) - e2y * (aabb.X - face[1].X);
            var f3 = e3x * (aabb.Y - face[2].Y) - e3y * (aabb.X - face[2].X);

            int x, y;

            int i = e1x + (e1y * aabb.Width);
            int j = e2x + (e2y * aabb.Width);
            int k = e3x + (e3y * aabb.Width);

            for (y = aabb.Y; y < aabb.Y + aabb.Height - 1; y++)
            {
                for (x = aabb.X; x < aabb.X + aabb.Width; x++)
                {
                    if ((f1 | f2 | f3) >= 0)
                    {
                        var offset = y * stride + x * BytesPerPixel;
                        *(framebuffer + offset + 2) = 255;
                        *(framebuffer + offset + 1) = 128;
                        *(framebuffer + offset + 0) = 64;

                        Dummy++;
                    }

                    f1 -= e1y;
                    f2 -= e2y;
                    f3 -= e3y;
                }

                f1 += i;
                f2 += j;
                f3 += k;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Inside(int p1x, int p1y, int p2x, int p2y) => p1x * p2x + p1y * p2y >= 0;
    }
}
