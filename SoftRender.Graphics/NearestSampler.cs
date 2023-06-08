using System.Drawing;

namespace SoftRender
{
    public unsafe class NearestSampler : ISampler
    {
        private readonly byte[] texture;
        private readonly Size size;
        private readonly int stride;

        /// <summary>
        /// Initializes a new instance of the <see cref="NearestSampler"/> class.
        /// </summary>
        /// <param name="texture">Texture array with 24bpp rgb encoding.</param>
        public NearestSampler(byte[] texture, Size size)
        {
            this.texture = texture;
            this.size = size;

            stride = size.Width * 3;
        }

        public ColorRGB Sample(float u, float v)
        {
            var tx = (int)(u * size.Width) * 3;
            var ty = (int)(v * size.Height) * 3;

            return ReadPixel(tx, ty);
        }

        private ColorRGB ReadPixel(int x, int y)
        {
            int offset = y * stride + x * 3;

            var r = texture[offset + 0];
            var g = texture[offset + 1];
            var b = texture[offset + 2];

            return new ColorRGB(r, g, b);
        }
    }
}
