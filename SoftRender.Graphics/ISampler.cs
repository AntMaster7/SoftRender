using System.Runtime.Intrinsics;

namespace SoftRender
{
    public interface ISampler
    {
        unsafe void Sample(float u, float v, byte* rgb);

        unsafe void SamplePacket(Vector256<float> us, Vector256<float> vs, PixelPacket pixel);
    }
}