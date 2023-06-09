using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SoftRender
{
    public unsafe class FastRasterizer : IRasterizer
    {
        private const byte BytesPerPixel = 3;

        private static readonly Vector256<float> Zeros = Vector256<float>.Zero;
        private static readonly Vector256<float> Eights = Vector256.Create((float)8);
        private static readonly Vector256<float> Ones = Vector256.Create(1f);
        private static readonly Vector128<float> VectorAllBitsSet = Vector128<float>.AllBitsSet;

        public int Dummy = 0;

        private readonly byte* framebuffer;
        private readonly int stride;

        public FastRasterizer(byte* framebuffer, int stride)
        {
            this.framebuffer = framebuffer;
            this.stride = stride;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="face">Face with viewport coordinates.</param>
        public unsafe void Rasterize(Vector3D[] face, VertexAttributes[] attribs, ISampler texture)
        {
            var left = System.Math.Min(System.Math.Min(face[0].X, face[1].X), face[2].X);
            var right = System.Math.Max(System.Math.Max(face[0].X, face[1].X), face[2].X);
            var top = System.Math.Min(System.Math.Min(face[0].Y, face[1].Y), face[2].Y);
            var bottom = System.Math.Max(System.Math.Max(face[0].Y, face[1].Y), face[2].Y);

            var aabb = new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));

            // 2D Cross: u1 * v2 - u2 * v1
            // u is the edge
            // initial = (u.x * v.y) - (u.y * v.x)
            // x ->: initial - u.y
            // y ->: initial + u.x
            // x <-: initial + u.y
            // y <-: initial - u.x

            var v1 = new PointPacket(face[0].X, face[0].Y);
            var v2 = new PointPacket(face[1].X, face[1].Y);
            var v3 = new PointPacket(face[2].X, face[2].Y);

            // negated because Y starts at top
            var e1 = v1 - v2;
            var e2 = v2 - v3;
            var e3 = v3 - v1;

            var start = new PointPacket()
            {
                Xs = Vector256.Create((float)aabb.X, aabb.X + 1, aabb.X + 2, aabb.X + 3, aabb.X + 4, aabb.X + 5, aabb.X + 6, aabb.X + 7),
                Ys = Vector256.Create((float)aabb.Y)
            };

            // interpolaters
            var i1 = e1.Xs * (start.Ys - v1.Ys) - e1.Ys * (start.Xs - v1.Xs);
            var i2 = e2.Xs * (start.Ys - v2.Ys) - e2.Ys * (start.Xs - v2.Xs);
            var i3 = e3.Xs * (start.Ys - v3.Ys) - e3.Ys * (start.Xs - v3.Xs);

            // var n1 = new PointPacket() { Xs = -e1.Ys, Ys = e1.Xs };
            // var l1 = Vector256.LessThan(n1.Xs, VectorZero);
            // var t1 = Avx2.And(Vector256.Equals(n1.Xs, VectorZero), Vector256.GreaterThanOrEqual(n1.Ys, VectorZero));
            // var tl1 = Avx2.Or(l1, t1);

            var negativeAreaTimesTwo = -e2.Ys * e1.Xs + e2.Xs * e1.Ys; // perp dot product

            var p = new PointPacket();

            int x, y;

            var pixel = new PixelPacket();

            var i = e1.Ys * Eights;
            var j = e2.Ys * Eights;
            var k = e3.Ys * Eights;

            var aabbWidth = Vector256.Create((float)aabb.Width);

            var g = e1.Xs + (e1.Ys * aabbWidth);
            var h = e2.Xs + (e2.Ys * aabbWidth);
            var q = e3.Xs + (e3.Ys * aabbWidth);

            //var a0rs = Vector256.Create((float)attribs[0].R);
            //var a1rs = Vector256.Create((float)attribs[1].R);
            //var a2rs = Vector256.Create((float)attribs[2].R);

            //var a0gs = Vector256.Create((float)attribs[0].G);
            //var a1gs = Vector256.Create((float)attribs[1].G);
            //var a2gs = Vector256.Create((float)attribs[2].G);

            //var a0bs = Vector256.Create((float)attribs[0].B);
            //var a1bs = Vector256.Create((float)attribs[1].B);
            //var a2bs = Vector256.Create((float)attribs[2].B);

            var a0us = Vector256.Create(attribs[0].U);
            var a1us = Vector256.Create(attribs[1].U);
            var a2us = Vector256.Create(attribs[2].U);

            var a0vs = Vector256.Create(attribs[0].V);
            var a1vs = Vector256.Create(attribs[1].V);
            var a2vs = Vector256.Create(attribs[2].V);

            var sampler = (NearestSampler)texture;

            for (y = aabb.Y; y < aabb.Y + aabb.Height; y++)
            {
                //p.Ys = Vector256.Create(y);

                for (x = aabb.X; x < aabb.X + aabb.Width; x += 8)
                {
                    var mask = Vector256.GreaterThanOrEqual(i1, Zeros);
                    mask = Avx.And(mask, Vector256.GreaterThanOrEqual(i2, Zeros));
                    mask = Avx.And(mask, Vector256.GreaterThanOrEqual(i3, Zeros));

                    if (Vector256.GreaterThanAny(mask.AsByte(), Zeros.AsByte()))
                    {
                        var b1 = i2 / negativeAreaTimesTwo;
                        var b2 = i3 / negativeAreaTimesTwo;
                        var b3 = Ones - b1 - b2;

                        var z1 = Vector256.Create(attribs[0].Z);
                        var z2 = Vector256.Create(attribs[1].Z);
                        var z3 = Vector256.Create(attribs[2].Z);

                        var z1Inv = Ones / z1; // Avx.Reciprocal(z1);
                        var z2Inv = Ones / z2; // Avx.Reciprocal(z2);
                        var z3Inv = Ones / z3; // Avx.Reciprocal(z3);

                        var zInv = z1Inv * b1 + z2Inv * b2 + z3Inv * b3;
                        var z = Ones / zInv; // Avx.Reciprocal(zInv);

                        var b1pc = z / z1 * b1;
                        var b2pc = z / z2 * b2;
                        var b3pc = Ones - b1pc - b2pc;

                        var us = a0us * b1pc + a1us * b2pc + a2us * b3pc;
                        var vs = a0vs * b1pc + a1vs * b2pc + a2vs * b3pc;

                        var offset = y * stride + x * BytesPerPixel;

                        sampler.Sample(us, vs, pixel);

                        pixel.StoreInterleaved(framebuffer + offset, mask);
                    }

                    i1 -= i;
                    i2 -= j;
                    i3 -= k;
                }

                i1 += g;
                i2 += h;
                i3 += q;

                p.Xs = start.Xs;
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

            return sb.ToString(); // .Trim(new char[] { ' ', '|' });
        }
    }
}
