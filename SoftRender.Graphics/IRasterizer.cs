using SoftRender.Graphics;
using SoftRender.SRMath;

namespace SoftRender
{
    public interface IRasterizer
    {
        void Rasterize(ReadOnlySpan<Vector4D> triangle, ReadOnlySpan<VertexAttributes> attribs, ISampler texture);
    }
}