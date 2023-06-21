using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SoftRender
{
    unsafe public class BitmapContext : IDisposable
    {
        private readonly BitmapData data;
        private readonly Bitmap bitmap;
        private bool disposed;

        // public access for super fast writing
        public readonly int BytesPerPixel;
        public readonly byte* Scan0;

        public int Stride => data.Stride;

        public BitmapContext(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            data = this.bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BytesPerPixel = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            Scan0 = (byte*)data.Scan0;
        }

        public void Clear(byte value = 255)
        {
            NativeMemory.Fill(Scan0, (nuint)(data.Width * data.Height * BytesPerPixel), value);
        }

        public void DrawPixel(Point p, ColorRGB color) => DrawPixel(p.X, p.Y, color);

        public void DrawPixel(int x, int y, ColorRGB color)
        {
            EnsureNotDisposed();

            var offset = y * data.Stride + x * BytesPerPixel;

            if (x > 0 && x < bitmap.Width && y > 0 && y < bitmap.Height) // brute-force clipping
            {
                *(Scan0 + offset + 0) = color.Blue;
                *(Scan0 + offset + 1) = color.Green;
                *(Scan0 + offset + 2) = color.Red;
            }
        }

        public void DrawLine(Point p1, Point p2, ColorRGB color) => DrawLine(p1.X, p1.Y, p2.X, p2.Y, color);

        public void DrawLine(int x1, int y1, int x2, int y2, ColorRGB color)
        {
            EnsureNotDisposed();

            int error = 0;
            int dx = x2 - x1;
            int dy = y2 - y1;

            if (System.Math.Abs(dy) > System.Math.Abs(dx))
            {
                int inc = dx < 0 ? -1 : 1;
                int errInc = System.Math.Abs(dx << 1);
                int errDec = System.Math.Abs(dy << 1);
                int sign = dy == 0 ? 1 : System.Math.Sign(dy);
                for (; y1 != y2 + 1; y1 += sign)
                {
                    DrawPixel(x1, y1, color);
                    error += errInc;
                    if (error >= System.Math.Abs(dy))
                    {
                        x1 += inc;
                        error -= errDec;
                    }
                }
            }
            else
            {
                int inc = dy < 0 ? -1 : 1;
                int errInc = System.Math.Abs(dy << 1);
                int errDec = System.Math.Abs(dx << 1);
                int sign = dx == 0 ? 1 : System.Math.Sign(dx);
                for (; x1 != x2 + 1; x1 += sign)
                {
                    DrawPixel(x1, y1, color);
                    error += errInc;
                    if (error >= System.Math.Abs(dx))
                    {
                        y1 += inc;
                        error -= errDec;
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            bitmap.UnlockBits(data);
            disposed = true;
        }

        [Conditional("DEBUG"), DebuggerStepThrough]
        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new InvalidOperationException("BitmapContext is disposed.");
            }
        }
    }
}
