using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

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
            var tx = (int)(u * w);
            var ty = (int)(v * h);

            int offset = ty * stride + tx * 4;

            *rgb = texture[offset + 0];
            *(rgb + 1) = texture[offset + 1];
            *(rgb + 2) = texture[offset + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            var xs = Avx.Floor(us * w);
            var ys = Avx.Floor(vs * h);

            var offsets = Avx.ConvertToVector256Int32(xs * 4 + ys * stride);

            fixed(byte* pTexture = texture)
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
