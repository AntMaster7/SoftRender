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
        private Matrix4D modelView;

        private Matrix4D projection;

        private Matrix3D invMvMatrix;

        private Vector3D lightSource;

        public VertexShader(Matrix4D modelView, Matrix4D projection, Matrix3D invMvMatrix, Vector3D lightSource)
        {
            this.modelView = modelView;
            this.projection = projection;
            this.invMvMatrix = invMvMatrix;
            this.lightSource = lightSource;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexShaderOutput Run(Vector3D v, Vector3D n)
        {
            var viewPos = modelView * v;

            return new VertexShaderOutput()
            {
                OutputVertex = projection * viewPos,
                OutputNormal = n * invMvMatrix,
                LightDirection = new Vector3D(lightSource.X - viewPos.X, lightSource.Y - viewPos.Y, lightSource.Z - viewPos.Z)
            };
        }
    }
}
