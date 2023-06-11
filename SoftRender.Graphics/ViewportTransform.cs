using SoftRender.SRMath;

namespace SoftRender
{
    /// <summary>
    /// Implements the transformation from normalized device coordinates to screen coordinates.
    /// This class performs better for the viewport transform than a general purpose 4x4 matrix.
    /// </summary>
    public class ViewportTransform
    {
        private int halfWidth;
        private int halfHeight;

        public ViewportTransform(int width, int height)
        {
            halfWidth = width / 2;
            halfHeight = height / 2;
        }

        public static Vector2D operator *(ViewportTransform t, Vector3D ndc)
        {
            int x = (int)(ndc.X * t.halfWidth + t.halfWidth + 0.5);
            int y = (int)(-ndc.Y * t.halfHeight + t.halfHeight + 0.5);

            return new Vector2D(x, y);
        }
    }
}
