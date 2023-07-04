using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender
{
    public unsafe class NearestSampler : ISampler
    {
        public readonly byte[] texture;
        public readonly int w;
        public readonly int h;
        public readonly int stride;

        public int[] _offsets = new int[8];

        /// <summary>
        /// Initializes a new instance of the <see cref="NearestSampler"/> class.
        /// </summary>
        /// <param name="texture">Texture array with 24bpp rgb encoding.</param>
        public NearestSampler(byte[] texture, Size size)
        {
            this.texture = texture;

            w = size.Width;
            h = size.Height;

            stride = w * 4;
        }

        public unsafe void ReadPixel(int x, int y, byte* rgb)
        {
            int offset = y * stride + x * 4;

            *rgb = texture[offset + 0];
            *(rgb + 1) = texture[offset + 1];
            *(rgb + 2) = texture[offset + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Sample(float u, float v, byte* rgb)
        {
            // Mirrors x and y
            var tx = System.Math.Abs((int)(u * w) % w);
            var ty = System.Math.Abs((int)(v * h) % h);

            int offset = ty * stride + tx * 4;

            *rgb = texture[offset + 0];
            *(rgb + 1) = texture[offset + 1];
            *(rgb + 2) = texture[offset + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            // https://en.wikipedia.org/wiki/Bilinear_interpolation

            var x = us * (w - 1);
            var y = vs * (h - 1);

            var half = Vector256.Create(0.5f);
            var shift = Vector256.Create(0.0001f);

            var x1 = Avx.Floor(x - half) + half;
            var x2 = Avx.Ceiling(x + shift + half) - half;
            var y1 = Avx.Floor(y - half) + half;
            var y2 = Avx.Ceiling(y + shift + half) - half;

            var q = Avx.Reciprocal((x2 - x1) * (y2 - y1));

            var a = x2 - x;
            var b = y2 - y;
            var c = x - x1;
            var d = y - y1;
            
            var w11 = a * b * q;
            var w12 = a * d * q;
            var w21 = c * b * q;
            var w22 = c * d * q;

            var sum = w11 + w12 + w21 + w22;
            Debug.Assert(Vector256.LessThanOrEqualAll(sum, Vector256.Create(1.001f)));
            Debug.Assert(Vector256.GreaterThanOrEqualAll(sum, Vector256.Create(0.999f)));

            if(Vector256.LessThanAll(sum, Vector256.Create(0.999f)))
            {
                throw new InvalidOperationException();
            }

            var _xs = Avx.Floor(us * (w - 1));
            var _ys = Avx.Floor(vs * (h - 1));

            var offsets = Avx.ConvertToVector256Int32(_xs * 4 + _ys * stride);

            fixed (byte* pTexture = texture)
            {
                var lower = Avx2.GatherVector128((int*)pTexture, offsets.GetLower(), 1);
                var upper = Avx2.GatherVector128((int*)pTexture, Avx.ExtractVector128(offsets, 1), 1);
                var rgb = Vector256.Create(lower, upper);

                var rs = Avx2.ShiftLeftLogical(rgb, 8);
                pixel.Rs = Avx2.ShiftRightLogical(rs, 24);

                var gs = Avx2.ShiftLeftLogical(rgb, 16);
                pixel.Gs = Avx2.ShiftRightLogical(gs, 24);

                var bs = Avx2.ShiftLeftLogical(rgb, 24);
                pixel.Bs = Avx2.ShiftRightLogical(bs, 24);
            }
        }
    }
}
