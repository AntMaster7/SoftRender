using System.Runtime.Intrinsics;

namespace SoftRender.Graphics
{
    public class Vector2DPacket
    {
        public Vector256<float> Xs;
        public Vector256<float> Ys;

        public Vector2DPacket()
        {
        }

        public Vector2DPacket(float x, float y)
        {
            Xs = Vector256.Create(x);
            Ys = Vector256.Create(y);
        }
    }
}
