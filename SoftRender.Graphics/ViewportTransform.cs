using System.Runtime.CompilerServices;

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
            halfWidth = (width - 1) / 2;
            halfHeight = (height - 1) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapX(float x) => (int)(x * halfWidth + halfWidth + 0.5);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapY(float y) => (int)(-y * halfHeight + halfHeight + 0.5);
    }
}
