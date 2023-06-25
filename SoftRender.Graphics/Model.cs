using SoftRender.SRMath;

namespace SoftRender.Graphics
{
    public class Model
    {
        public Vector3D[] Vertices;

        public VertexAttributes[] Attributes;

        public ISampler Texture;

        /// <summary>
        /// Gets or sets the world transform matrix.
        /// </summary>
        public Matrix4D Transform = Matrix4D.CreateIdentity();
    }
}
