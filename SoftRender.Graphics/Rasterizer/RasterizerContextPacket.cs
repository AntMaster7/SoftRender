using SoftRender.SRMath;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender.Graphics
{
    internal struct RasterizerContextPacket
    {
        private static readonly Vector256<float> Eights = Vector256.Create((float)8);

        private readonly float xRightClip;
        private int xIncrements = 0;
        private readonly Rectangle aabb;

        public Vector256<float> Z1;
        public Vector256<float> Z2;

        private Vector256<float> z1Inv;
        private Vector256<float> z2Inv;
        private Vector256<float> z3Inv;

        // Increments for the edge function accumulators
        private Vector256<float> e1x;
        private Vector256<float> e2x;
        private Vector256<float> e3x;
        private Vector256<float> e1y;
        private Vector256<float> e2y;
        private Vector256<float> e3y;

        // Edge function accumulators
        public Vector256<float> Function1;
        public Vector256<float> Function2;
        public Vector256<float> Function3;

        // Double the area of the triangle
        public Vector256<float> AreaTimesTwo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RasterizerContextPacket(Rectangle aabb, int screenWidth, Vector3D[] screenTriangle)
        {
            Vector256<float> v1x = Vector256.Create(screenTriangle[0].X);
            Vector256<float> v1y = Vector256.Create(screenTriangle[0].Y);
            Vector256<float> v2x = Vector256.Create(screenTriangle[1].X);
            Vector256<float> v2y = Vector256.Create(screenTriangle[1].Y);
            Vector256<float> v3x = Vector256.Create(screenTriangle[2].X);
            Vector256<float> v3y = Vector256.Create(screenTriangle[2].Y);

            Vector256<float> e1Xs = v1x - v2x;
            Vector256<float> e1Ys = v1y - v2y;
            Vector256<float> e2Xs = v2x - v3x;
            Vector256<float> e2Ys = v2y - v3y;
            Vector256<float> e3Xs = v3x - v1x;
            Vector256<float> e3Ys = v3y - v1y;

            xRightClip = screenWidth - 10; // TODO
            this.aabb = aabb;

            var start = new PointPacket()
            {
                Xs = Vector256.Create((float)aabb.X, aabb.X + 1, aabb.X + 2, aabb.X + 3, aabb.X + 4, aabb.X + 5, aabb.X + 6, aabb.X + 7),
                Ys = Vector256.Create((float)aabb.Y)
            };

            // Edge functions
            Function1 = e1Xs * (start.Ys - v1y) - e1Ys * (start.Xs - v1x);
            Function2 = e2Xs * (start.Ys - v2y) - e2Ys * (start.Xs - v2x);
            Function3 = e3Xs * (start.Ys - v3y) - e3Ys * (start.Xs - v3x);

            // Increments for edge functions
            // x ->: initial - u.y
            // y ->: initial + u.x
            // x <-: initial + u.y
            // y <-: initial - u.x
            e1x = e1Ys * Eights;
            e2x = e2Ys * Eights;
            e3x = e3Ys * Eights;
            e1y = e1Xs;
            e2y = e2Xs;
            e3y = e3Xs;

            AreaTimesTwo = Fma.MultiplyAdd(e2Xs, e1Ys, -e2Ys * e1Xs);

            // Get inverse depths
            Z1 = Vector256.Create(screenTriangle[0].Z);
            Z2 = Vector256.Create(screenTriangle[1].Z);
            z1Inv = Avx.Reciprocal(Z1);
            z2Inv = Avx.Reciprocal(Z2);
            z3Inv = Vector256.Create(1 / screenTriangle[2].Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<float> GetInsideMask(int x)
        {
            var inside = Vector256.GreaterThanOrEqual(Function1, Rasterizer.Zeros);
            inside = Avx.And(inside, Vector256.GreaterThanOrEqual(Function2, Rasterizer.Zeros));
            inside = Avx.And(inside, Vector256.GreaterThanOrEqual(Function3, Rasterizer.Zeros));

            if (x < 0)
            {
                var insideView = Vector256.Create((float)x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, x + 7);
                inside = Avx.And(inside, Vector256.GreaterThanOrEqual(insideView, Rasterizer.Zeros));
            }
            else if (x > xRightClip)
            {
                var insideView = Vector256.Create((float)x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, x + 7);
                inside = Avx.And(inside, Vector256.LessThanOrEqual(insideView, Vector256.Create(xRightClip)));
            }

            return inside;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementX()
        {
            Function1 -= e1x;
            Function2 -= e2x;
            Function3 -= e3x;

            xIncrements++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementX(int fac)
        {
            Function1 -= e1x * fac;
            Function2 -= e2x * fac;
            Function3 -= e3x * fac;

            xIncrements += fac;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceXToStart(out int x)
        {
            x = aabb.X;
            if (x < 0)
            {
                var k = x / 8;
                x -= k * 8;
                IncrementX(-k);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceXToEnd(int x)
        {
            var r = aabb.X + aabb.Width - x;
            var rm = (int)System.Math.Ceiling((float)r / 8);
            IncrementX(rm);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetXAndIncrementY()
        {
            Function1 += e1y + e1x * xIncrements;
            Function2 += e2y + e2x * xIncrements;
            Function3 += e3y + e3x * xIncrements;

            xIncrements = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateBarycentricCoordinates(Vector3DPacket barycentric)
        {
            barycentric.Xs = Function2 / AreaTimesTwo;
            barycentric.Ys = Function3 / AreaTimesTwo;
            barycentric.Zs = Rasterizer.Ones - barycentric.Xs - barycentric.Ys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<float> InterpolateDepth(Vector3DPacket barycentric) =>
            Avx.Reciprocal(z1Inv * barycentric.Xs + z2Inv * barycentric.Ys + z3Inv * barycentric.Zs);
    }
}
