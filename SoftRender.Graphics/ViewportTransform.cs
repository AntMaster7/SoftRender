using SoftRender.SRMath;
using System.Drawing;

namespace SoftRender
{
    public class ViewportTransform
    {
        private int halfWidth;
        private int halfHeight;
        private int height;
        private float aspect;

        public ViewportTransform(int width, int height)
        {
            this.height = height;
            halfWidth = width / 2;
            halfHeight = height / 2;
            aspect = (float)width / height;
        }

        public static Vector3D operator *(ViewportTransform t, Vector3D v)
        {
            int x = (int)(v.X * t.halfWidth + t.halfWidth);
            int y = t.height - (int)(v.Y * t.halfHeight + t.halfHeight);

            return new Vector3D(x, y, v.Z);
        }
    }
}
