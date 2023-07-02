using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static SoftRender.Graphics.PixelShader;

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
            // var k = Vector256.Create((float)System.Math.Ceiling(box.Width / 8f));
            e1y = e1.Xs;
            e2y = e2.Xs;
            e3y = e3.Xs;

            AreaTimesTwo = -e2.Ys * e1.Xs + e2.Xs * e1.Ys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<float> GetInsideMask(int x)
        {
            var inside = Vector256.GreaterThanOrEqual(Function1, FastRasterizer.Zeros);
            inside = Avx.And(inside, Vector256.GreaterThanOrEqual(Function2, FastRasterizer.Zeros));
            inside = Avx.And(inside, Vector256.GreaterThanOrEqual(Function3, FastRasterizer.Zeros));

            if (x < 0)
            {
                var insideView = Vector256.Create((float)x, x + 1, x + 2, x + 3, x + 4, x + 5, x + 6, x + 7);
                inside = Avx.And(inside, Vector256.GreaterThanOrEqual(insideView, FastRasterizer.Zeros));
            }

            return inside;
        }

        private int incs = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementX()
        {
            Function1 -= e1x;
            Function2 -= e2x;
            Function3 -= e3x;

            incs++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementX(int fac)
        {
            Function1 -= e1x * fac;
            Function2 -= e2x * fac;
            Function3 -= e3x * fac;

            incs += fac;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetXAndIncrementY()
        {
            Function1 += (e1y + e1x * incs);
            Function2 += (e2y + e2x * incs);
            Function3 += (e3y + e3x * incs);

            incs = 0;
        }
    }

    public unsafe sealed class FastRasterizer : IRasterizer, IDisposable
    {
        private const byte BytesPerPixel = 3;

        internal static readonly Vector256<float> Zeros = Vector256<float>.Zero;
        internal static readonly Vector256<float> Ones = Vector256.Create(1f);

        private readonly Size viewportSize;
        private readonly ViewportTransform vpt;
        private readonly byte* framebuffer;
        private readonly int frameBufferStride;
        public readonly int zBufferSize;
        public readonly float* zBuffer;
        public readonly int zBufferStride;

        public RasterizerMode Mode;

        //public Vector3D[] Normals = new Vector3D[3];

        public int Face = -1;

        public FastRasterizer(byte* framebuffer, int stride, Size viewportSize, ViewportTransform vpt)
        {
            this.framebuffer = framebuffer;
            this.vpt = vpt;
            this.viewportSize = viewportSize;

            frameBufferStride = stride;

            zBufferStride = this.viewportSize.Width;
            zBufferSize = this.viewportSize.Width * this.viewportSize.Height;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="face">Face with viewport coordinates.</param>
        public unsafe void Rasterize(Span<VertexShaderOutput> input, Light[] lights, ISampler texture)
        {
            // First transform vertices into ndc and then into screen space
            var ndcTriangle = new Vector3D[3];
            var screenTriangle = new Vector2D[3];
            for (int index = 0; index < 3; index++)
            {
                ndcTriangle[index] = input[index].ClipPosition.PerspectiveDivide();
                screenTriangle[index] = vpt * ndcTriangle[index];
            }

            var normal = Vector3D.CrossProduct(ndcTriangle[1] - ndcTriangle[0], ndcTriangle[2] - ndcTriangle[0]);
            if (Vector3D.DotProduct(normal, ndcTriangle[0]) < 0)
            {
                if ((Mode & RasterizerMode.Fill) == RasterizerMode.Fill)
                {
                    FillTriangle(input, screenTriangle, lights, texture);
                }

                if ((Mode & RasterizerMode.Wireframe) == RasterizerMode.Wireframe)
                {
                    DrawLine((int)screenTriangle[0].X, (int)screenTriangle[0].Y, (int)screenTriangle[1].X, (int)screenTriangle[1].Y, new ColorRGB(255, 255, 255));
                    DrawLine((int)screenTriangle[1].X, (int)screenTriangle[1].Y, (int)screenTriangle[2].X, (int)screenTriangle[2].Y, new ColorRGB(255, 255, 255));
                    DrawLine((int)screenTriangle[2].X, (int)screenTriangle[2].Y, (int)screenTriangle[0].X, (int)screenTriangle[0].Y, new ColorRGB(255, 255, 255));
                }
            }
        }

        public unsafe void RasterizeZBufferOnly(Span<Vector4D> clipPositions)
        {
            var ndcTriangle = new Vector3D[3];
            var screenTriangle = new Vector2D[3];
            for (int index = 0; index < 3; index++)
            {
                ndcTriangle[index] = clipPositions[index].PerspectiveDivide();
                screenTriangle[index] = vpt * ndcTriangle[index];
            }

            var normal = Vector3D.CrossProduct(ndcTriangle[1] - ndcTriangle[0], ndcTriangle[2] - ndcTriangle[0]);
            if (Vector3D.DotProduct(normal, ndcTriangle[0]) < 0)
            {
                // Gets axis-aligned bounding box for our triangle (brute force approach)
                Rectangle aabb = GetAABB(screenTriangle);

                var v1 = new PointPacket(screenTriangle[0].X, screenTriangle[0].Y);
                var v2 = new PointPacket(screenTriangle[1].X, screenTriangle[1].Y);
                var v3 = new PointPacket(screenTriangle[2].X, screenTriangle[2].Y);

                var context = new RasterizerContextPacket(aabb, v1, v2, v3);

                // Check for degenerate triangle with zero area
                if (Vector256.EqualsAny(Zeros, context.AreaTimesTwo))
                {
                    return;
                }

                // Get inverse depths
                var z1 = Vector256.Create(clipPositions[0].W);
                var z2 = Vector256.Create(clipPositions[1].W);
                var z1Inv = Avx.Reciprocal(z1);
                var z2Inv = Avx.Reciprocal(z2);
                var z3Inv = Vector256.Create(1 / clipPositions[2].W);

                bool enter;
                bool exit;

                for (int y = aabb.Y; y < aabb.Y + aabb.Height; y++)
                {
                    enter = false;
                    exit = false;

                    // Skip scanline outside of frame
                    int leftX = aabb.X;
                    if (leftX < 0)
                    {
                        var k = leftX / 8;
                        leftX -= k * 8;
                        context.IncrementX(-k);
                    }

                    for (int x = leftX; x < aabb.X + aabb.Width; x += 8)
                    {
                        if (!exit)
                        {
                            var insideMask = context.GetInsideMask(x);

                            if (Vector256.GreaterThanAny(insideMask.AsByte(), Zeros.AsByte()))
                            {
                                enter = true;

                                // Calculate barycentric coordinates
                                var b1 = context.Function2 / context.AreaTimesTwo;
                                var b2 = context.Function3 / context.AreaTimesTwo;
                                var b3 = Ones - b1 - b2;

                                // Interpolate the depth value
                                var zInv = z1Inv * b1 + z2Inv * b2 + z3Inv * b3;
                                var z = Avx.Reciprocal(zInv);

                                // Update z-Buffer
                                var zBufferOffset = y * zBufferStride + x;
                                var zBufferValue = Vector256.Load(zBuffer + zBufferOffset);
                                zBufferValue = Avx.Max(zBufferValue, z);
                                Avx.MaskStore(zBuffer + zBufferOffset, insideMask, zBufferValue);
                            }
                            else if (enter)
                            {
                                exit = true;
                            }

                            context.IncrementX();
                        }
                        else // basically some useless optimization
                        {
                            var r = aabb.X + aabb.Width - x;
                            var rm = (int)System.Math.Ceiling((float)r / 8);
                            context.IncrementX(rm);
                            break;
                        }
                    }

                    context.ResetXAndIncrementY();
                }
            }
        }

        private void FillTriangle(Span<VertexShaderOutput> input, Vector2D[] screenTriangle, Light[] lights, ISampler texture)
        {
            // Gets axis-aligned bounding box for our triangle (brute force approach)
            Rectangle aabb = GetAABB(screenTriangle);

            var v1 = new PointPacket(screenTriangle[0].X, screenTriangle[0].Y);
            var v2 = new PointPacket(screenTriangle[1].X, screenTriangle[1].Y);
            var v3 = new PointPacket(screenTriangle[2].X, screenTriangle[2].Y);

            var context = new RasterizerContextPacket(aabb, v1, v2, v3);

            // Check for degenerate triangle with zero area
            if (Vector256.EqualsAny(Zeros, context.AreaTimesTwo))
            {
                return;
            }

            // Get texture coordinates for later interpolation
            var a0us = Vector256.Create(input[0].TexCoords.X);
            var a0vs = Vector256.Create(input[0].TexCoords.Y);
            var a1us = Vector256.Create(input[1].TexCoords.X);
            var a1vs = Vector256.Create(input[1].TexCoords.Y);
            var a2us = Vector256.Create(input[2].TexCoords.X);
            var a2vs = Vector256.Create(input[2].TexCoords.Y);

            // Get inverse depths
            var z1 = Vector256.Create(input[0].ClipPosition.W);
            var z2 = Vector256.Create(input[1].ClipPosition.W);
            var z1Inv = Avx.Reciprocal(z1);
            var z2Inv = Avx.Reciprocal(z2);
            var z3Inv = Vector256.Create(1 / input[2].ClipPosition.W);

            var sampler = (NearestSampler)texture;
            var pixel = new PixelPacket();
            bool enter;
            bool exit;

            var lightPackets = new LightPacket[lights.Count()];
            for (int i = 0; i < lights.Count(); i++)
            {
                var lightPos = lights[i].GetWorldPosition();
                lightPackets[i] = new LightPacket(lightPos.X, lightPos.Y, lightPos.Z);
            }

            var pixelShader = new PixelShader(sampler, lightPackets);
            var pixelShaderInput = new PixelShaderInput();
            pixelShaderInput.WorldPositions = new Vector3DPacket();
            pixelShaderInput.TexCoords = new Vector2DPacket();
            pixelShaderInput.WorldNormals = new Vector3DPacket();

            for (int y = aabb.Y; y < aabb.Y + aabb.Height; y++)
            {
                enter = false;
                exit = false;

                // Skip scanline outside of frame
                int leftX = aabb.X;
                if(leftX < 0)
                {
                    var k = leftX / 8;
                    leftX -= k * 8;
                    context.IncrementX(-k);
                }

                for (int x = leftX; x < aabb.X + aabb.Width; x += 8)
                {
                    if (!exit)
                    {
                        var insideMask = context.GetInsideMask(x);
                        if (Vector256.GreaterThanAny(insideMask.AsByte(), Zeros.AsByte()))
                        {
                            enter = true;

                            // Calculate barycentric coordinates
                            var b1 = context.Function2 / context.AreaTimesTwo;
                            var b2 = context.Function3 / context.AreaTimesTwo;
                            var b3 = Ones - b1 - b2;

                            // Interpolate the depth value
                            var zInv = z1Inv * b1 + z2Inv * b2 + z3Inv * b3;
                            var z = Avx.Reciprocal(zInv);

                            // Update z-Buffer
                            var zBufferOffset = y * zBufferStride + x;
                            var zBufferValue  = Vector256.Load(zBuffer + zBufferOffset);
                            var zBufferMask   = Avx.Compare(z, zBufferValue, FloatComparisonMode.OrderedGreaterThanNonSignaling);
                            zBufferValue      = Avx.Max(zBufferValue, z);
                            Avx.MaskStore(zBuffer + zBufferOffset, insideMask, zBufferValue);

                            // Apply z-Buffer
                            insideMask = Avx.And(zBufferMask.AsSingle(), insideMask);

                            // Calculate perspective-correct barycentric coordinates
                            var b1pc = z / z1 * b1;
                            var b2pc = z / z2 * b2;
                            var b3pc = Avx.Subtract(Ones, b1pc);
                            b3pc = Avx.Subtract(b3pc, b2pc);

                            // Interpolate texture coordinates
                            pixelShaderInput.TexCoords.Xs = Avx.And(b1pc * a0us + b2pc * a1us + b3pc * a2us, insideMask);
                            pixelShaderInput.TexCoords.Ys = Avx.And(b1pc * a0vs + b2pc * a1vs + b3pc * a2vs, insideMask);

                            // Interpolate normals
                            pixelShaderInput.WorldNormals.Xs = b1pc * input[0].WorldNormal.X + b2pc * input[1].WorldNormal.X + b3pc * input[2].WorldNormal.X;
                            pixelShaderInput.WorldNormals.Ys = b1pc * input[0].WorldNormal.Y + b2pc * input[1].WorldNormal.Y + b3pc * input[2].WorldNormal.Y;
                            pixelShaderInput.WorldNormals.Zs = b1pc * input[0].WorldNormal.Z + b2pc * input[1].WorldNormal.Z + b3pc * input[2].WorldNormal.Z;

                            // Interpolate world positions
                            pixelShaderInput.WorldPositions.Xs = b1pc * input[0].WorldPosition.X + b2pc * input[1].WorldPosition.X + b3pc * input[2].WorldPosition.X;
                            pixelShaderInput.WorldPositions.Ys = b1pc * input[0].WorldPosition.Y + b2pc * input[1].WorldPosition.Y + b3pc * input[2].WorldPosition.Y;
                            pixelShaderInput.WorldPositions.Zs = b1pc * input[0].WorldPosition.Z + b2pc * input[1].WorldPosition.Z + b3pc * input[2].WorldPosition.Z;

                            pixelShader.Run(pixel, pixelShaderInput);

                            var offset = y * frameBufferStride + x * BytesPerPixel;
                            pixel.StoreInterleaved(framebuffer + offset, insideMask);
                        }
                        else if (enter)
                        {
                            exit = true;
                        }

                        context.IncrementX();
                    }
                    else // basically some useless optimization
                    {
                        var r = aabb.X + aabb.Width - x;
                        var rm = (int)System.Math.Ceiling((float)r / 8);
                        context.IncrementX(rm);
                        break;
                    }

                }

                context.ResetXAndIncrementY();
            }
        }

        private Rectangle GetAABB(Vector2D[] screenTriangle)
        {
            var left = (int)System.Math.Min(System.Math.Min(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var right = (int)System.Math.Max(System.Math.Max(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var top = (int)System.Math.Min(System.Math.Min(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);
            var bottom = (int)System.Math.Max(System.Math.Max(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);
            
            var width = (right - left);
            var height = (bottom - top);
            var shoot = top + height - viewportSize.Height;
            if(shoot > 0)
            {
                height -= shoot;
            }

            var aabb = new Rectangle(left, top, width, height);
            
            return aabb;
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

        private void ResetZBuffer()
        {
            for (int i = 0; i < zBufferSize; i++)
            {
                Unsafe.Write(zBuffer + i, float.MinValue);
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
