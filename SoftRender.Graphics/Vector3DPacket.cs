using System.Runtime.Intrinsics;

namespace SoftRender.Graphics
{
    public class Vector3DPacket
    {
        public Vector256<float> Xs;
        public Vector256<float> Ys;
        public Vector256<float> Zs;

        public Vector3DPacket()
        {
        }

        public Vector3DPacket(float x, float y, float z)
        {
            Xs = Vector256.Create(x);
            Ys = Vector256.Create(y);
            Zs = Vector256.Create(z);
        }
    }
}
