using SoftRender.SRMath;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static SoftRender.Graphics.PixelShader;

namespace SoftRender.Graphics
{
    public unsafe sealed class Rasterizer : IRasterizer, IDisposable
    {
        private const byte BytesPerPixel = 3;

        internal static readonly Vector256<float> Zeros = Vector256<float>.Zero;
        internal static readonly Vector256<float> Ones = Vector256.Create(1f);

        private readonly Size viewportSize;
        private readonly ViewportTransform vpt;
        private readonly byte* framebuffer;
        private readonly int frameBufferStride;

        // object pooling
        private TriangleTextureCoordinates tc = new();
        private readonly PixelPacket pixel = new();
        private readonly Vector3D[] screenTriangle = new Vector3D[3];
        private readonly Vector3DPacket barycentric = new Vector3DPacket();
        private readonly PixelShaderInput pixelShaderInput = new PixelShaderInput();

        private PixelShader? pixelShader;
        private readonly int zBufferSize;
        private readonly int zBufferStride;

        public readonly float* ZBuffer;
        public RasterizerMode Mode;
        public int Face = -1;

        public Rasterizer(byte* framebuffer, int stride, Size viewportSize, ViewportTransform vpt)
        {
            this.framebuffer = framebuffer;
            this.vpt = vpt;
            this.viewportSize = viewportSize;

            frameBufferStride = stride;

            zBufferStride = this.viewportSize.Width;
            zBufferSize = this.viewportSize.Width * this.viewportSize.Height;
            ZBuffer = (float*)MemoryPoolSlim.Shared.Rent(zBufferSize * sizeof(float));

            ResetZBuffer();

            pixelShaderInput.WorldPositions = new Vector3DPacket();
            pixelShaderInput.TexCoords = new Vector2DPacket();
            pixelShaderInput.WorldNormals = new Vector3DPacket();
        }

        ~Rasterizer()
        {
            MemoryPoolSlim.Shared.Return((nint)ZBuffer);
        }

        public void Dispose()
        {
            MemoryPoolSlim.Shared.Return((nint)ZBuffer);
            GC.SuppressFinalize(this);
        }

        public unsafe void Rasterize(Span<VertexShaderOutput> input, PixelShader pixelShader)
        {
            this.pixelShader = pixelShader;

            if (IsFrontFace(ref input[0].ClipPosition, ref input[1].ClipPosition, ref input[2].ClipPosition)) // Back-face culling
            {
                // Transform clip space positions to screen space
                MapScreenTriangle(ref input[0].ClipPosition, ref input[1].ClipPosition, ref input[2].ClipPosition);

                if ((Mode & RasterizerMode.Fill) == RasterizerMode.Fill)
                {
                    FillTriangle(input, screenTriangle);
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
            if (IsFrontFace(ref clipPositions[0], ref clipPositions[1], ref clipPositions[2]))
            {
                MapScreenTriangle(ref clipPositions[0], ref clipPositions[1], ref clipPositions[2]);
                Rectangle aabb = GetAABB(screenTriangle);
                var context = new RasterizerContextPacket(aabb, viewportSize.Width, screenTriangle);

                // Check for degenerate triangle with zero area
                if (Vector256.EqualsAny(Zeros, context.AreaTimesTwo))
                {
                    return;
                }

                bool insideTriangle;

                for (int y = aabb.Y; y < aabb.Y + aabb.Height; y++)
                {
                    insideTriangle = false;
                    context.AdvanceXToStart(out int leftX);

                    for (int x = leftX; x < aabb.X + aabb.Width; x += 8)
                    {
                        var insideMask = context.GetInsideMask(x);
                        if (Vector256.GreaterThanAny(insideMask.AsByte(), Zeros.AsByte()))
                        {
                            insideTriangle = true;
                            context.UpdateBarycentricCoordinates(barycentric);
                            var z = context.InterpolateDepth(barycentric);

                            // Update z-Buffer
                            var zBufferOffset = y * zBufferStride + x;
                            var zBufferValue = Vector256.Load(ZBuffer + zBufferOffset);
                            zBufferValue = Avx.Max(zBufferValue, z);
                            Avx.MaskStore(ZBuffer + zBufferOffset, insideMask, zBufferValue);
                        }
                        else if (insideTriangle)
                        {
                            context.AdvanceXToEnd(x);
                            break;
                        }

                        context.IncrementX();
                    }

                    context.ResetXAndIncrementY();
                }
            }
        }

        private void FillTriangle(Span<VertexShaderOutput> input, Vector3D[] screenTriangle)
        {
            var aabb = GetAABB(screenTriangle);

            var context = new RasterizerContextPacket(aabb, viewportSize.Width, screenTriangle);

            // Check for degenerate triangle with zero area
            if (Vector256.EqualsAny(Zeros, context.AreaTimesTwo))
            {
                return;
            }

            // Get texture coordinates for later interpolation
            InitializeTextureCoordinates(input);

            bool insideTriangle;

            var wn0x = Vector256.Create(input[0].WorldNormal.X);
            var wn0y = Vector256.Create(input[0].WorldNormal.Y);
            var wn0z = Vector256.Create(input[0].WorldNormal.Z);

            var wn1x = Vector256.Create(input[1].WorldNormal.X);
            var wn1y = Vector256.Create(input[1].WorldNormal.Y);
            var wn1z = Vector256.Create(input[1].WorldNormal.Z);

            var wn2x = Vector256.Create(input[2].WorldNormal.X);
            var wn2y = Vector256.Create(input[2].WorldNormal.Y);
            var wn2z = Vector256.Create(input[2].WorldNormal.Z);

            for (int y = aabb.Y; y < aabb.Y + aabb.Height; y++)
            {
                insideTriangle = false;

                // Skip scanline outside of frame
                context.AdvanceXToStart(out int leftX);

                for (int x = leftX; x < aabb.X + aabb.Width; x += 8)
                {
                    var insideMask = context.GetInsideMask(x);
                    if (Vector256.GreaterThanAny(insideMask.AsByte(), Zeros.AsByte()))
                    {
                        insideTriangle = true;

                        context.UpdateBarycentricCoordinates(barycentric);
                        var z = context.InterpolateDepth(barycentric);
                        insideMask = UpdateAndApplyZBuffer(y, x, insideMask, z);

                        // Calculate perspective-correct barycentric coordinates
                        var b1pc = z / context.Z1 * barycentric.Xs;
                        var b2pc = z / context.Z2 * barycentric.Ys;
                        var b3pc = Avx.Subtract(Ones, b1pc);
                        b3pc = Avx.Subtract(b3pc, b2pc);

                        // Interpolate texture coordinates
                        pixelShaderInput.TexCoords.Xs = Avx.And(Fma.MultiplyAdd(b3pc, tc.a2us, Fma.MultiplyAdd(b1pc, tc.a0us, b2pc * tc.a1us)), insideMask);
                        pixelShaderInput.TexCoords.Ys = Avx.And(Fma.MultiplyAdd(b3pc, tc.a2vs, Fma.MultiplyAdd(b1pc, tc.a0vs, b2pc * tc.a1vs)), insideMask);

                        // Interpolate normals
                        pixelShaderInput.WorldNormals.Xs = Fma.MultiplyAdd(b3pc, wn2x, Fma.MultiplyAdd(b1pc, wn0x, b2pc * wn1x));
                        pixelShaderInput.WorldNormals.Ys = Fma.MultiplyAdd(b3pc, wn2y, Fma.MultiplyAdd(b1pc, wn0y, b2pc * wn1y));
                        pixelShaderInput.WorldNormals.Zs = Fma.MultiplyAdd(b3pc, wn2z, Fma.MultiplyAdd(b1pc, wn0z, b2pc * wn1z));

                        // Interpolate world positions
                        pixelShaderInput.WorldPositions.Xs = b1pc * input[0].WorldPosition.X + b2pc * input[1].WorldPosition.X + b3pc * input[2].WorldPosition.X;
                        pixelShaderInput.WorldPositions.Ys = b1pc * input[0].WorldPosition.Y + b2pc * input[1].WorldPosition.Y + b3pc * input[2].WorldPosition.Y;
                        pixelShaderInput.WorldPositions.Zs = b1pc * input[0].WorldPosition.Z + b2pc * input[1].WorldPosition.Z + b3pc * input[2].WorldPosition.Z;

                        pixelShader!.Run(pixel, pixelShaderInput);

                        var offset = y * frameBufferStride + x * BytesPerPixel;
                        pixel.StoreInterleaved(framebuffer + offset, insideMask);
                    }
                    else if (insideTriangle)
                    {
                        context.AdvanceXToEnd(x);
                        break;
                    }

                    context.IncrementX();
                }

                context.ResetXAndIncrementY();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeTextureCoordinates(Span<VertexShaderOutput> input)
        {
            tc.a0us = Vector256.Create(input[0].TexCoords.X);
            tc.a0vs = Vector256.Create(input[0].TexCoords.Y);
            tc.a1us = Vector256.Create(input[1].TexCoords.X);
            tc.a1vs = Vector256.Create(input[1].TexCoords.Y);
            tc.a2us = Vector256.Create(input[2].TexCoords.X);
            tc.a2vs = Vector256.Create(input[2].TexCoords.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<float> UpdateAndApplyZBuffer(int y, int x, Vector256<float> insideMask, Vector256<float> z)
        {
            var zBufferOffset = y * zBufferStride + x;
            var zBufferValue = Vector256.Load(ZBuffer + zBufferOffset);
            var zBufferMask = Avx.Compare(z, zBufferValue, FloatComparisonMode.OrderedGreaterThanNonSignaling);
            zBufferValue = Avx.Max(zBufferValue, z);
            Avx.MaskStore(ZBuffer + zBufferOffset, insideMask, zBufferValue);

            return Avx.And(zBufferMask.AsSingle(), insideMask);
        }

        private Rectangle GetAABB(Vector3D[] screenTriangle)
        {
            var left = (int)System.Math.Min(System.Math.Min(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var right = (int)System.Math.Max(System.Math.Max(screenTriangle[0].X, screenTriangle[1].X), screenTriangle[2].X);
            var top = (int)System.Math.Min(System.Math.Min(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);
            var bottom = (int)System.Math.Max(System.Math.Max(screenTriangle[0].Y, screenTriangle[1].Y), screenTriangle[2].Y);

            var width = right - left;
            var height = bottom - top;
            var shoot = top + height - viewportSize.Height;
            if (shoot > 0)
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
                if(y >= viewportSize.Height || x >= viewportSize.Width || y < 0 || x < 0) { return; } // TODO: Brute-force clipping

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
                Unsafe.Write(ZBuffer + i, float.MinValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MapScreenTriangle(ref Vector4D c1, ref Vector4D c2, ref Vector4D c3)
        {
            screenTriangle[0].X = vpt.MapX(c1.X / c1.W);
            screenTriangle[0].Y = vpt.MapY(c1.Y / c1.W);
            screenTriangle[0].Z = c1.W;

            screenTriangle[1].X = vpt.MapX(c2.X / c2.W);
            screenTriangle[1].Y = vpt.MapY(c2.Y / c2.W);
            screenTriangle[1].Z = c2.W;

            screenTriangle[2].X = vpt.MapX(c3.X / c3.W);
            screenTriangle[2].Y = vpt.MapY(c3.Y / c3.W);
            screenTriangle[2].Z = c3.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFrontFace(ref Vector4D c1, ref Vector4D c2, ref Vector4D c3) => true; // TODO
                                                                                             //(c2.X - c1.X) * (c3.Y - c1.Y) - (c3.X - c1.X) * (c2.Y - c1.Y) < 0; // gpt
    }
}
