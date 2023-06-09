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

        public unsafe void ReadPixel(int x, int y, byte* rgb)
        {
            int offset = y * stride + x * 3;

            *rgb = texture[offset + 0];
            *(rgb + 1) = texture[offset + 1];
            *(rgb + 2) = texture[offset + 2];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SamplePacket(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            var xs = Avx.Floor(us * w);
            var ys = Avx.Floor(vs * h);

            var offsets = Avx.ConvertToVector256Int32(xs * 3 + ys * stride);

            fixed (int* p_offsets = _offsets)
            {
                Avx.Store(p_offsets, offsets);
            }

            _rs[0] = texture[_offsets[0] + 2];
            _gs[0] = texture[_offsets[0] + 1];
            _bs[0] = texture[_offsets[0] + 0];

            _rs[1] = texture[_offsets[1] + 2];
            _gs[1] = texture[_offsets[1] + 1];
            _bs[1] = texture[_offsets[1] + 0];

            _rs[2] = texture[_offsets[2] + 2];
            _gs[2] = texture[_offsets[2] + 1];
            _bs[2] = texture[_offsets[2] + 0];

            _rs[3] = texture[_offsets[3] + 2];
            _gs[3] = texture[_offsets[3] + 1];
            _bs[3] = texture[_offsets[3] + 0];

            _rs[4] = texture[_offsets[4] + 2];
            _gs[4] = texture[_offsets[4] + 1];
            _bs[4] = texture[_offsets[4] + 0];

            _rs[5] = texture[_offsets[5] + 2];
            _gs[5] = texture[_offsets[5] + 1];
            _bs[5] = texture[_offsets[5] + 0];

            _rs[6] = texture[_offsets[6] + 2];
            _gs[6] = texture[_offsets[6] + 1];
            _bs[6] = texture[_offsets[6] + 0];

            _rs[7] = texture[_offsets[7] + 2];
            _gs[7] = texture[_offsets[7] + 1];
            _bs[7] = texture[_offsets[7] + 0];

            fixed (float* prs = _rs)
            fixed (float* pgs = _gs)
            fixed (float* pbs = _bs)
            {
                pixel.Rs = Avx.ConvertToVector256Int32(Avx.LoadVector256(prs));
                pixel.Gs = Avx.ConvertToVector256Int32(Avx.LoadVector256(pgs));
                pixel.Bs = Avx.ConvertToVector256Int32(Avx.LoadVector256(pbs));
            }
        }
    }
}
