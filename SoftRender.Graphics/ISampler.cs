using System.Runtime.Intrinsics;

namespace SoftRender
{
    public interface ISampler
    {
        unsafe void Sample(float u, float v, byte* r, byte* g, byte* b);

        unsafe void Sample(Vector256<float> us, Vector256<float> vs, float[] rs, float[] gs, float[] bs);
    }
}