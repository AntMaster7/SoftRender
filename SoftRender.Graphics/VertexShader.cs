using SoftRender.SRMath;
using System.Runtime.CompilerServices;

namespace SoftRender.Graphics
{
    public struct VertexShaderOutput
    {
        public Vector3D LightDirection;

        public Vector4D OutputVertex;

        public Vector3D OutputNormal;
    }

    public class VertexShader
    {
        private Matrix4D mvpMatrix;

        private Matrix3D invMvMatrix;

        private Vector3D lightSource;

        public VertexShader(Matrix4D mvpMatrix, Matrix3D invMvMatrix, Vector3D lightSource)
        {
            this.mvpMatrix = mvpMatrix;
            this.invMvMatrix = invMvMatrix;
            this.lightSource = lightSource;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexShaderOutput Run(Vector3D v, Vector3D n)
        {
            return new VertexShaderOutput()
            {
                OutputVertex = mvpMatrix * v,
                OutputNormal = n * invMvMatrix,
                LightDirection = lightSource - v,
            };
        }
    }
}
