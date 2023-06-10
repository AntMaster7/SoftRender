using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Drawing;

namespace SoftRender
{
    public unsafe class SimpleRasterizer : IRasterizer
    {
        private const byte BytesPerPixel = 3;

        public int Dummy = 0;

        private readonly byte* framebuffer;
        private readonly int stride;

        public SimpleRasterizer(byte* framebuffer, int stride)
        {
            this.framebuffer = framebuffer;
            this.stride = stride;
        }

        public unsafe void DrawTexture(ISampler texture, Rectangle screen)
        {
            var w = screen.Width;
            var h = screen.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var u = (float)x / w;
                    var v = (float)y / h;

                    int offset = y * stride + x * BytesPerPixel;

                    texture.Sample(u, v, framebuffer + offset);
                }
            }
        }

        public unsafe void Rasterize(Vector3D[] face, VertexAttributes[] attribs, ISampler texture)
        {
            var l = System.Math.Min(System.Math.Min(face[0].X, face[1].X), face[2].X);
            var r = System.Math.Max(System.Math.Max(face[0].X, face[1].X), face[2].X);
            var t = System.Math.Min(System.Math.Min(face[0].Y, face[1].Y), face[2].Y);
            var b = System.Math.Max(System.Math.Max(face[0].Y, face[1].Y), face[2].Y);

            var aabb = new Rectangle((int)l, (int)t, (int)(r - l), (int)(b - t));

            var e1x = (int)-(face[1].X - face[0].X);
            var e2x = (int)-(face[2].X - face[1].X);
            var e3x = (int)-(face[0].X - face[2].X);
            var e1y = (int)-(face[1].Y - face[0].Y);
            var e2y = (int)-(face[2].Y - face[1].Y);
            var e3y = (int)-(face[0].Y - face[2].Y);

            var f1 = (int)(e1x * (aabb.Y - face[0].Y) - e1y * (aabb.X - face[0].X));
            var f2 = (int)(e2x * (aabb.Y - face[1].Y) - e2y * (aabb.X - face[1].X));
            var f3 = (int)(e3x * (aabb.Y - face[2].Y) - e3y * (aabb.X - face[2].X));

            float negativeAreaTimesTwo = -e2y * e1x + e2x * e1y; // perp dot product

            int x, y;

            int i = e1x + (e1y * aabb.Width);
            int j = e2x + (e2y * aabb.Width);
            int k = e3x + (e3y * aabb.Width);

            var sampler = (NearestSampler)texture;

            for (y = aabb.Y; y < aabb.Y + aabb.Height - 1; y++)
            {
                for (x = aabb.X; x < aabb.X + aabb.Width; x++)
                {
                    if ((f1 | f2 | f3) >= 0)
                    {
                        var b1 = f2 / negativeAreaTimesTwo;
                        var b2 = f3 / negativeAreaTimesTwo;
                        var b3 = 1 - b1 - b2;

                        var z1Inv = 1 / attribs[0].Z;
                        var z2Inv = 1 / attribs[1].Z;
                        var z3Inv = 1 / attribs[2].Z;

                        var zInv = z1Inv * b1 + z2Inv * b2 + z3Inv * b3;
                        var z = 1 / zInv;

                        var b1pc = z / attribs[0].Z * b1;
                        var b2pc = z / attribs[1].Z * b2;
                        var b3pc = 1 - b1pc - b2pc;

                        var u = attribs[0].UV.X * b1pc + attribs[1].UV.X * b2pc + attribs[2].UV.X * b3pc;
                        var v = attribs[0].UV.Y * b1pc + attribs[1].UV.Y * b2pc + attribs[2].UV.Y * b3pc;

                        var offset = y * stride + x * BytesPerPixel;
                        //*(framebuffer + offset + 2) = (byte)(attribs[0].R * b1pc + attribs[1].R * b2pc + attribs[2].R * b3pc);
                        //*(framebuffer + offset + 1) = (byte)(attribs[0].G * b1pc + attribs[1].G * b2pc + attribs[2].G * b3pc);
                        //*(framebuffer + offset + 0) = (byte)(attribs[0].B * b1pc + attribs[1].B * b2pc + attribs[2].B * b3pc);

                        sampler.Sample(u, v, framebuffer + offset);
                    }

                    f1 -= e1y;
                    f2 -= e2y;
                    f3 -= e3y;
                }

                f1 += i;
                f2 += j;
                f3 += k;
            }
        }
    }
}
