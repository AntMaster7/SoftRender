using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender
{
    public enum SamplerMode
    { 
        Nearest,
        Linear
    }

    public unsafe class TextureSampler : ISampler
    {
        public readonly byte[] texture;
        public readonly int w;
        public readonly int h;
        public readonly int stride;

        public int[] _offsets = new int[8];

        public SamplerMode Mode { get; set; } = SamplerMode.Linear;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureSampler"/> class.
        /// </summary>
        /// <param name="texture">Texture array with 24bpp rgb encoding.</param>
        public TextureSampler(byte[] texture, Size size)
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
            if (Mode == SamplerMode.Nearest)
            {
                SampleNearest(us, vs, pixel);   
            }
            else
            {
                SampleLinear(us, vs, pixel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SampleNearest(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            var _xs = Avx.Floor(us * (w - 1));
            var _ys = Avx.Floor(vs * (h - 1));

            var offsets = Avx.ConvertToVector256Int32(_xs * 4 + _ys * stride);

            fixed (byte* pTexture = texture)
            {
                var rgb = Avx2.GatherVector256((int*)pTexture, offsets, 1);

                var rs = Avx2.ShiftLeftLogical(rgb, 8);
                pixel.Rs = Avx2.ShiftRightLogical(rs, 24);

                var gs = Avx2.ShiftLeftLogical(rgb, 16);
                pixel.Gs = Avx2.ShiftRightLogical(gs, 24);

                var bs = Avx2.ShiftLeftLogical(rgb, 24);
                pixel.Bs = Avx2.ShiftRightLogical(bs, 24);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SampleLinear(Vector256<float> us, Vector256<float> vs, PixelPacket pixel)
        {
            // https://en.wikipedia.org/wiki/Bilinear_interpolation

            var x = us * (w - 1);
            var y = vs * (h - 1);

            var half = Vector256.Create(0.5f);
            var shift = Vector256.Create(0.0001f);

            var x1 = Avx.Floor(x - half) + half;
            var x2 = Avx.Ceiling(x + shift + half) - half;
            var y1 = Avx.Ceiling(y + shift + half) - half;
            var y2 = Avx.Floor(y - half) + half;

            var q = Avx.Reciprocal((x2 - x1) * (y2 - y1));

            var a = x2 - x;
            var b = y2 - y;
            var c = x - x1;
            var d = y - y1;

            var w11 = a * b * q;
            var w12 = a * d * q;
            var w21 = c * b * q;
            var w22 = c * d * q;

            var e = Avx.Floor(x1) * 4;
            var f = Avx.Floor(x2) * 4;
            var g = Avx.Floor(y1) * stride;
            var i = Avx.Floor(y2) * stride;

            var q11Offsets = Avx.ConvertToVector256Int32(e + g);
            var q12Offsets = Avx.ConvertToVector256Int32(e + i);
            var q21Offsets = Avx.ConvertToVector256Int32(f + g);
            var q22Offsets = Avx.ConvertToVector256Int32(f + i);

            fixed (byte* pTexture = texture)
            {
                var rgb = Avx2.GatherVector256((int*)pTexture, q11Offsets, 1);
                var rs = Avx2.ShiftLeftLogical(rgb, 8);
                rs = Avx2.ShiftRightLogical(rs, 24);
                var rsSum = Avx.ConvertToVector256Single(rs) * w11;
                var gs = Avx2.ShiftLeftLogical(rgb, 16);
                gs = Avx2.ShiftRightLogical(gs, 24);
                var gsSum = Avx.ConvertToVector256Single(gs) * w11;
                var bs = Avx2.ShiftLeftLogical(rgb, 24);
                bs = Avx2.ShiftRightLogical(bs, 24);
                var bsSum = Avx.ConvertToVector256Single(bs) * w11;

                rgb = Avx2.GatherVector256((int*)pTexture, q12Offsets, 1);
                rs = Avx2.ShiftLeftLogical(rgb, 8);
                rs = Avx2.ShiftRightLogical(rs, 24);
                rsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(rs), w12, rsSum);
                gs = Avx2.ShiftLeftLogical(rgb, 16);
                gs = Avx2.ShiftRightLogical(gs, 24);
                gsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(gs), w12, gsSum);
                bs = Avx2.ShiftLeftLogical(rgb, 24);
                bs = Avx2.ShiftRightLogical(bs, 24);
                bsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(bs), w12, bsSum);

                rgb = Avx2.GatherVector256((int*)pTexture, q21Offsets, 1);
                rs = Avx2.ShiftLeftLogical(rgb, 8);
                rs = Avx2.ShiftRightLogical(rs, 24);
                rsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(rs), w21, rsSum);
                gs = Avx2.ShiftLeftLogical(rgb, 16);
                gs = Avx2.ShiftRightLogical(gs, 24);
                gsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(gs), w21, gsSum);
                bs = Avx2.ShiftLeftLogical(rgb, 24);
                bs = Avx2.ShiftRightLogical(bs, 24);
                bsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(bs), w21, bsSum);

                rgb = Avx2.GatherVector256((int*)pTexture, q22Offsets, 1);
                rs = Avx2.ShiftLeftLogical(rgb, 8);
                rs = Avx2.ShiftRightLogical(rs, 24);
                rsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(rs), w22, rsSum);
                gs = Avx2.ShiftLeftLogical(rgb, 16);
                gs = Avx2.ShiftRightLogical(gs, 24);
                gsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(gs), w22, gsSum);
                bs = Avx2.ShiftLeftLogical(rgb, 24);
                bs = Avx2.ShiftRightLogical(bs, 24);
                bsSum = Fma.MultiplyAdd(Avx.ConvertToVector256Single(bs), w22, bsSum);

                pixel.Rs = Avx.ConvertToVector256Int32(rsSum);
                pixel.Gs = Avx.ConvertToVector256Int32(gsSum);
                pixel.Bs = Avx.ConvertToVector256Int32(bsSum);
            }
        }
    }
}
