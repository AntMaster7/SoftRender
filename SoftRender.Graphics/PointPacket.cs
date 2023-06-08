using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender
{
    public class PointPacket
    {
        public Vector256<int> Xs;
        public Vector256<int> Ys;

        public PointPacket()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointPacket(int xs, int ys)
        {
            Xs = Vector256.Create<int>(xs);
            Ys = Vector256.Create<int>(ys);
        }

        public static PointPacket operator -(PointPacket left, PointPacket right)
        {
            return new PointPacket()
            {
                Xs = Avx2.Subtract(left.Xs, right.Xs),
                Ys = Avx2.Subtract(left.Ys, right.Ys)
            };
        }
    }
}
