using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender
{
    public class PointPacket
    {
        public Vector256<float> Xs;
        public Vector256<float> Ys;

        public PointPacket()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointPacket(float xs, float ys)
        {
            Xs = Vector256.Create<float>(xs);
            Ys = Vector256.Create<float>(ys);
        }

        public static PointPacket operator -(PointPacket left, PointPacket right)
        {
            return new PointPacket()
            {
                Xs = Avx.Subtract(left.Xs, right.Xs),
                Ys = Avx.Subtract(left.Ys, right.Ys)
            };
        }
    }
}
