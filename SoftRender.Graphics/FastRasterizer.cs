using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SoftRender
{
    [Flags]
    public enum RasterizerMode
    {
        Fill = 1,
        Wireframe = 2
    }

    internal struct RasterizerContextPacket
    {
        private static readonly Vector256<float> Eights = Vector256.Create((float)8);

        private Vector256<float> e1x;
        private Vector256<float> e2x;
        private Vector256<float> e3x;
        private Vector256<float> e1y;
        private Vector256<float> e2y;
        private Vector256<float> e3y;

        public Vector256<float> Function1;
        public Vector256<float> Function2;
        public Vector256<float> Function3;

        public Vector256<float> NegativeAreaTimesTwo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RasterizerContextPacket(Rectangle box, PointPacket v1, PointPacket v2, PointPacket v3)
        {
            var e1 = v1 - v2;
            var e2 = v2 - v3;
            var e3 = v3 - v1;

            var start = new PointPacket()
            {
                Xs = Vector256.Create((float)box.X, box.X + 1, box.X + 2, box.X + 3, box.X + 4, box.X + 5, box.X + 6, box.X + 7),
                Ys = Vector256.Create((float)box.Y)
            };

            // Edge functions
            Function1 = e1.Xs * (start.Ys - v1.Ys) - e1.Ys * (start.Xs - v1.Xs);
            Function2 = e2.Xs * (start.Ys - v2.Ys) - e2.Ys * (start.Xs - v2.Xs);
            Function3 = e3.Xs * (start.Ys - v3.Ys) - e3.Ys * (start.Xs - v3.Xs);

            // Increments for edge functions
            // 2D Cross: u1 * v2 - u2 * v1
            // u is the edge
            // initial = (u.x * v.y) - (u.y * v.x)
            // x ->: initial - u.y
            // y ->: initial + u.x
            // x <-: initial + u.y
            // y <-: initial - u.x
            e1x = e1.Ys * Eights;
            e2x = e2.Ys * Eights;
            e3x = e3.Ys * Eights;
            var k = Vector256.Create((float)System.Math.Ceiling(box.Width / 8f));
            e1y = e1.Xs + (e1.Ys * k * Eights);
            e2y = e2.Xs + (e2.Ys * k * Eights);
            e3y = e3.Xs + (e3.Ys * k * Eights);

            NegativeAreaTimesTwo = -e2.Ys * e1.Xs + e2.Xs * e1.Ys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<float> GetInsideMask()
        {
            var inside = Vector256.GreaterThanOrEqual(Function1, FastRasterizer.Zeros);
            inside = Avx.And(inside, Vector256.GreaterThanOrEqual(Function2, FastRasterizer.Zeros));
            return Avx.And(inside, Vector256.GreaterThanOrEqual(Function3, FastRasterizer.Zeros));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementX()
        {
            Function1 -= e1x;
            Function2 -= e2x;
            Function3 -= e3x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetXAndIncrementY()
        {
            Function1 += e1y;
            Function2 += e2y;
            Function3 += e3y;
        }
    }

    public unsafe sealed class FastRasterizer : IRasterizer, IDisposable
    {
        private const byte BytesPerPixel = 3;

        internal static readonly Vector256<float> Zeros = Vector256<float>.Zero;
        internal static readonly Vector256<float> Ones = Vector256.Create(1f);
        internal static readonly Vector256<float> MaxColor = Vector256.Create(255f);

        private readonly ViewportTransform vpt;
        private readonly byte* framebuffer;
        private readonly int frameBufferStride;
        private readonly int zBufferSize;
        private readonly float* zBuffer;
        private readonly int zBufferStride;

        public RasterizerMode Mode;

        public Vector3D[] Normals = new Vector3D[3];

        public int Face = -1;

        public FastRasterizer(byte* framebuffer, int stride, Size size, ViewportTransform vpt)
        {
            this.framebuffer = framebuffer;
            this.vpt = vpt;

            frameBufferStride = stride;

            zBufferStride = size.Width;
            zBufferSize = size.Width * size.Height;
            zBuffer = (float*)Marshal.AllocHGlobal(zBufferSize * sizeof(float));
            ResetZBuffer();
        }

        ~FastRasterizer()
        {
            Marshal.FreeHGlobal((nint)zBuffer);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)zBuffer);
            GC.SuppressFinalize(this);
        }

        public void ResetZBuffer()
        {
            for (int i = 0; i < zBufferSize; i++)
            {
                Unsafe.Write(zBuffer + i, float.MinValue);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="face">Face with viewport coordinates.</param>
        public unsafe void Rasterize(ReadOnlySpan<Vector4D> triangle, ReadOnlySpan<VertexAttributes> attribs, ISampler texture)
        {
            // First transform vertices into ndc and then into screen space
            var ndcTriangle = new Vector3D[3];
            var screenTriangle = new Vector2D[3];
            for (int index = 0; index < 3; index++)
            {
                ndcTriangle[index] = triangle[index].PerspectiveDivide();
                screenTriangle[index] = vpt * ndcTriangle[index];
            }

            if ((Mode & RasterizerMode.Fill) == RasterizerMode.Fill)
            {
                FillTriangle(triangle, screenTriangle, attribs, texture);
            }

            if ((Mode & RasterizerMode.Wireframe) == RasterizerMode.Wireframe)
            {
                DrawLine((int)screenTriangle[0].X, (int)screenTriangle[0].Y, (int)screenTriangle[1].X, (int)screenTriangle[1].Y, new ColorRGB(255, 255, 255));
                DrawLine((int)screenTriangle[1].X, (int)screenTriangle[1].Y, (int)screenTriangle[2].X, (int)screenTriangle[2].Y, new ColorRGB(255, 255, 255));
                DrawLine((int)screenTriangle[2].X, (int)screenTriangle[2].Y, (int)screenTriangle[0].X, (int)screenTriangle[0].Y, new ColorRGB(255, 255, 255));
            }
        }

        private void FillTriangle(ReadOnlySpan<Vector4D> clipSpaceTriangle, Vector2D[] screenTriangle, ReadOnlySpan<VertexAttributes> attribs, ISampler texture)
        {
            // Gets axis-aligned bounding box for our triangle (brute force approach)
            var left = System.Math.Min(System.Math.Min(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var right = System.Math.Max(System.Math.Max(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var top = System.Math.Min(System.Math.Min(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);
            var bottom = System.Math.Max(System.Math.Max(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);
            var aabb = new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));

            var v1 = new PointPacket(screenTriangle[0].X, screenTriangle[0].Y);
            var v2 = new PointPacket(screenTriangle[1].X, screenTriangle[1].Y);
            var v3 = new PointPacket(screenTriangle[2].X, screenTriangle[2].Y);

            var context = new RasterizerContextPacket(aabb, v1, v2, v3);

            // Check for degenerate triangle with zero area
            if (Vector256.EqualsAny(Zeros, context.NegativeAreaTimesTwo))
            {
                return;
            }

            // Get light directions
            var a0ld = new Vector3DPacket(attribs[0].LightDirection.X, attribs[0].LightDirection.Y, attribs[0].LightDirection.Z);
            var a1ld = new Vector3DPacket(attribs[1].LightDirection.X, attribs[1].LightDirection.Y, attribs[1].LightDirection.Z);
            var a2ld = new Vector3DPacket(attribs[2].LightDirection.X, attribs[2].LightDirection.Y, attribs[2].LightDirection.Z);

            // Get texture coordinates for later interpolation
            var a0us = Vector256.Create(attribs[0].UV.X);
            var a0vs = Vector256.Create(attribs[0].UV.Y);
            var a1us = Vector256.Create(attribs[1].UV.X);
            var a1vs = Vector256.Create(attribs[1].UV.Y);
            var a2us = Vector256.Create(attribs[2].UV.X);
            var a2vs = Vector256.Create(attribs[2].UV.Y);

            var sampler = (NearestSampler)texture;

            var pixel = new PixelPacket();

            Vector3DPacket p = new Vector3DPacket();

            for (int y = aabb.Y; y < aabb.Y + aabb.Height; y++)
            {
                for (int x = aabb.X; x < aabb.X + aabb.Width; x += 8)
                {
                    var insideMask = context.GetInsideMask();

                    if (Vector256.GreaterThanAny(insideMask.AsByte(), Zeros.AsByte()))
                    {
                        // Calculate barycentric coordinates
                        var b1 = context.Function2 / context.NegativeAreaTimesTwo;
                        var b2 = context.Function3 / context.NegativeAreaTimesTwo;
                        var b3 = Ones - b1 - b2;

                        // Interpolate the depth value
                        var z1 = Vector256.Create(clipSpaceTriangle[0].W);
                        var z2 = Vector256.Create(clipSpaceTriangle[1].W);
                        var z3 = Vector256.Create(clipSpaceTriangle[2].W);
                        var z1Inv = Ones / z1; // Avx.Reciprocal(z1);
                        var z2Inv = Ones / z2; // Avx.Reciprocal(z2);
                        var z3Inv = Ones / z3; // Avx.Reciprocal(z3);
                        var zInv = z1Inv * b1 + z2Inv * b2 + z3Inv * b3;
                        var z = Ones / zInv; // Avx.Reciprocal(zInv);

                        // Update z-Buffer
                        var zBufferOffset = y * zBufferStride + x;
                        var zBufferValue = Vector256.Load(zBuffer + zBufferOffset);
                        var zBufferMask = Avx.Compare(z, zBufferValue, FloatComparisonMode.OrderedGreaterThanNonSignaling);
                        zBufferValue = Avx.Max(zBufferValue, z);
                        Avx.MaskStore(zBuffer + zBufferOffset, insideMask, zBufferValue);

                        // Apply z-Buffer
                        insideMask = Avx.And(zBufferMask.AsSingle(), insideMask);

                        // Calculate perspective-correct barycentric coordinates
                        var b1pc = z / z1 * b1;
                        var b2pc = z / z2 * b2;
                        var b3pc = Avx.Subtract(Ones, b1pc);
                        b3pc = Avx.Subtract(b3pc, b2pc);

                        // Interpolate texture coordinates
                        var us = a0us * b1pc + a1us * b2pc + a2us * b3pc;
                        us = Avx.And(us, insideMask);
                        var vs = a0vs * b1pc + a1vs * b2pc + a2vs * b3pc;
                        vs = Avx.And(vs, insideMask);

                        // Sample texture and render pixel
                        var offset = y * frameBufferStride + x * BytesPerPixel;
                        sampler.Sample(us, vs, pixel);

                        float ambientLightIntensity = 0.3f;

                        var rsdiffuse = Avx.ConvertToVector256Single(pixel.Rs);
                        var gsdiffuse = Avx.ConvertToVector256Single(pixel.Gs);
                        var bsdiffuse = Avx.ConvertToVector256Single(pixel.Bs);

                        var rsambient = rsdiffuse * ambientLightIntensity;
                        var gsambient = gsdiffuse * ambientLightIntensity;
                        var bsambient = bsdiffuse * ambientLightIntensity;

                        p.Xs = a0ld.Xs * b1pc + a1ld.Xs * b2pc + a2ld.Xs * b3pc;
                        p.Ys = a0ld.Ys * b1pc + a1ld.Ys * b2pc + a2ld.Ys * b3pc;
                        p.Zs = a0ld.Zs * b1pc + a1ld.Zs * b2pc + a2ld.Zs * b3pc;

                        var sqrt = Avx.Sqrt(p.Xs * p.Xs + p.Ys * p.Ys + p.Zs * p.Zs);
                        p.Xs /= sqrt;
                        p.Ys /= sqrt;
                        p.Zs /= sqrt;

                        var illum = Vector256.Create(0.9f);

                        var nx = Vector256.Create(Normals[0].X);
                        var ny = Vector256.Create(Normals[0].Y);
                        var nz = Vector256.Create(Normals[0].Z);

                        var dot = p.Xs * nx + p.Ys * ny + p.Zs * nz;
                        dot = Avx.Max(dot, Zeros);

                        var rsdirect = rsdiffuse * illum * dot;
                        var gsdirect = gsdiffuse * illum * dot;
                        var bsdirect = bsdiffuse * illum * dot;

                        pixel.Rs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, rsambient + rsdirect));
                        pixel.Gs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, gsambient + gsdirect));
                        pixel.Bs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, bsambient + bsdirect));

                        pixel.StoreInterleaved(framebuffer + offset, insideMask);
                    }

                    context.IncrementX();
                }

                context.ResetXAndIncrementY();
            }
        }

        private void DrawLine(int x1, int y1, int x2, int y2, ColorRGB color)
        {
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DrawPixel(int x, int y, ColorRGB color)
            {
                var offset = y * frameBufferStride + x * BytesPerPixel;

                *(framebuffer + offset + 0) = color.Blue;
                *(framebuffer + offset + 1) = color.Green;
                *(framebuffer + offset + 2) = color.Red;
            }
        }

        private static unsafe string PrintVector(Vector128<float> v)
        {
            var f = new float[4];
            fixed (float* p = f)
            {
                Sse.Store(p, v);
            }

            return $"{f[3]}, {f[2]}, {f[1]}, {f[0]}";
        }

        private static string PrintMasked(Vector256<float> v, Vector256<float> mask)
        {
            var sb = new StringBuilder(20);

            fixed (float* m = new float[8])
            {
                Avx.Store(m, mask);
                for (int i = 0; i < 8; i++)
                {
                    if (float.IsNaN(m[i]))
                    {
                        sb.Append(v[i]).Append(" | ");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
