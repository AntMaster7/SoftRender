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

        private int[] offsets = new int[8];

        /// <summary>
        /// Initializes a new instance of the <see cref="NearestSampler"/> class.
        /// </summary>
        /// <param name="texture">Texture array with 24bpp rgb encoding.</param>
        public NearestSampler(byte[] texture, Size size)
        {
            this.texture = texture;
            
            w = size.Width;
            h = size.Height;

            stride = w * 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(Vector256<float> us, Vector256<float> vs, float[] rs, float[] gs, float[] bs)
        {
            //rs[0] = 255;
            //rs[1] = 255;

            var xs = w * us;
            var ys = h * vs;

            var _offsets = Avx.ConvertToVector256Int32(ys * stride + xs * 3);

            fixed (int* p = offsets)
            {
                Avx.Store(p, _offsets);
            }

            rs[0] = texture[offsets[0] + 0];
            gs[0] = texture[offsets[0] + 1];
            bs[0] = texture[offsets[0] + 2];

            rs[1] = texture[offsets[1] + 3 + 0];
            gs[1] = texture[offsets[1] + 3 + 1];
            bs[1] = texture[offsets[1] + 3 + 2];

            rs[2] = texture[offsets[2] + 6 + 0];
            gs[2] = texture[offsets[2] + 6 + 1];
            bs[2] = texture[offsets[2] + 6 + 2];

            rs[3] = texture[offsets[3] + 9 + 2];
            gs[3] = texture[offsets[3] + 9 + 1];
            bs[3] = texture[offsets[3] + 9 + 0];

            rs[4] = texture[offsets[4] + 12 + 0];
            gs[4] = texture[offsets[4] + 12 + 1];
            bs[4] = texture[offsets[4] + 12 + 2];

            rs[5] = texture[offsets[5] + 15 + 0];
            gs[5] = texture[offsets[5] + 15 + 1];
            bs[5] = texture[offsets[5] + 15 + 2];

            rs[6] = texture[offsets[6] + 18 + 0];
            gs[6] = texture[offsets[6] + 18 + 1];
            bs[6] = texture[offsets[6] + 18 + 2];

            rs[7] = texture[offsets[7] + 21 + 0];
            gs[7] = texture[offsets[7] + 21 + 1];
            bs[7] = texture[offsets[7] + 21 + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Sample(float u, float v, byte* r, byte* g, byte* b)
        {
            var tx = (int)(u * w);
            var ty = (int)(v * h);

            int offset = ty * stride + tx * 3;

            *r = texture[offset + 0];
            *g = texture[offset + 1];
            *b = texture[offset + 2];
        }
    }
}
