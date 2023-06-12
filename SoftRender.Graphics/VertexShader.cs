using SoftRender.SRMath;
using System.Runtime.CompilerServices;

namespace SoftRender.Graphics
{
    public struct VertexShaderOutput
    {
        public Vector3D LightDirection;

        public Vector4D OutputVertex;
    }

    public class VertexShader
    {
        public Matrix4D ProjectionMatrix;

        public Vector3D LightSource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexShaderOutput Run(Vector3D v)
        {
            return new VertexShaderOutput()
            {
                LightDirection = LightSource - v,
                OutputVertex = ProjectionMatrix * v
            };
        }
    }
}
