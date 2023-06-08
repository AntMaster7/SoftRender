using SoftRender.SRMath;
using System.Drawing;

namespace SoftRender
{
    public class ViewportTransform
    {
        private int halfWidth;
        private int halfHeight;
        private int height;

        public ViewportTransform(int width, int height)
        {
            this.height = height;
            halfWidth = width / 2;
            halfHeight = height / 2;
        }

        public static Point operator *(ViewportTransform t, Vector2D v)
        {
            int x = (int)(v.X * t.halfWidth + t.halfWidth);
            int y = t.height - (int)(v.Y * t.halfHeight + t.halfHeight);

            return new Point(x, y);
        }
    }
}
