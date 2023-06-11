using SoftRender.SRMath;

namespace SoftRender.Graphics
{
    public class VertexAttributes
    {
        public Vector2D UV;

        public Vector3D Normal;

        public VertexAttributes()
        {
        }

        public VertexAttributes(float u, float v, float nx, float ny, float nz)
        {
            UV = new Vector2D(u, v);
            Normal = new Vector3D(nx, ny, nz);
        }
    }
}
