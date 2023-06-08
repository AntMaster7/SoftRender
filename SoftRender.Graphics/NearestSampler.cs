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

        public int[] offsets = new int[8];
        private float[] _rs = new float[8];
        private float[] _gs = new float[8];
        private float[] _bs = new float[8];

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
        public unsafe void SamplePacket(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            var xs = w * us;
            var ys = h * vs;

            fixed (int* p = offsets)
            {
                Avx.Store(p, Avx.ConvertToVector256Int32(ys * stride + xs * 3));
            }

            _rs[0] = texture[offsets[0] + 0];
            _gs[0] = texture[offsets[0] + 1];
            _bs[0] = texture[offsets[0] + 2];

            _rs[1] = texture[offsets[1] + 3 + 0];
            _gs[1] = texture[offsets[1] + 3 + 1];
            _bs[1] = texture[offsets[1] + 3 + 2];

            _rs[2] = texture[offsets[2] + 6 + 0];
            _gs[2] = texture[offsets[2] + 6 + 1];
            _bs[2] = texture[offsets[2] + 6 + 2];

            _rs[3] = texture[offsets[3] + 9 + 0];
            _gs[3] = texture[offsets[3] + 9 + 1];
            _bs[3] = texture[offsets[3] + 9 + 2];

            _rs[4] = texture[offsets[4] + 12 + 0];
            _gs[4] = texture[offsets[4] + 12 + 1];
            _bs[4] = texture[offsets[4] + 12 + 2];

            _rs[5] = texture[offsets[5] + 15 + 0];
            _gs[5] = texture[offsets[5] + 15 + 1];
            _bs[5] = texture[offsets[5] + 15 + 2];

            _rs[6] = texture[offsets[6] + 18 + 0];
            _gs[6] = texture[offsets[6] + 18 + 1];
            _bs[6] = texture[offsets[6] + 18 + 2];

            _rs[7] = texture[offsets[7] + 21 + 0];
            _gs[7] = texture[offsets[7] + 21 + 1];
            _bs[7] = texture[offsets[7] + 21 + 2];

            fixed (float* prs = _rs)
            fixed (float* pgs = _gs)
            fixed (float* pbs = _bs)
            {
                pixel.Rs = Avx.ConvertToVector256Int32(Avx.LoadVector256(prs));
                pixel.Gs = Avx.ConvertToVector256Int32(Avx.LoadVector256(pgs));
                pixel.Bs = Avx.ConvertToVector256Int32(Avx.LoadVector256(pbs));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Sample(float u, float v, byte* rgb)
        {
            var tx = (int)(u * w);
            var ty = (int)(v * h);

            int offset = ty * stride + tx * 3;

            *rgb = texture[offset + 0];
            *(rgb + 1) = texture[offset + 1];
            *(rgb + 2) = texture[offset + 2];
        }
    }
}
