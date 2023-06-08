using SoftRender.Graphics;
using SoftRender.SRMath;

namespace SoftRender
{
    public interface IRasterizer
    {
        void Rasterize(Vector3D[] face, VertexAttributes[] attribs);
    }
}