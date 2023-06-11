using SoftRender.Graphics;
using SoftRender.SRMath;

namespace SoftRender
{
    public interface IRasterizer
    {
        void Rasterize(Vector4D[] triangle, VertexAttributes[] attribs, ISampler texture);
    }
}