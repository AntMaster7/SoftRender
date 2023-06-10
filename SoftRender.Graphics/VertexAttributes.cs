using SoftRender.SRMath;

namespace SoftRender.Graphics
{
    public class VertexAttributes
    {
        public float Z;

        public Vector2D UV;

        public Vector3D Normal;

        public VertexAttributes()
        {
        }

        public VertexAttributes(float z, float u, float v, float nx, float ny, float nz)
        {
            Z = z;

            UV = new Vector2D(u, v);
            Normal = new Vector3D(nx, ny, nz);
        }
    }
}
