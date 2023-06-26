using SoftRender.SRMath;
using System.Runtime.CompilerServices;

namespace SoftRender.Graphics
{
    public struct VertexShaderOutput
    {
        public Vector4D ClipPosition;

        public Vector3D WorldNormal;

        public Vector3D WorldPosition;

        public Vector2D TexCoords;
    }

    public class VertexShader
    {
        private readonly Matrix4D modelMatrix;
        private readonly Matrix4D viewMatrix;
        private readonly Matrix4D projectionMatrix;
        private readonly Matrix4D modelViewProjectionMatrix;
        private readonly Matrix3D invModelMatrix;

        public VertexShader(Matrix4D model, Matrix4D view, Matrix4D projection)
        {
            modelMatrix = model;
            viewMatrix = view;
            projectionMatrix = projection;

            modelViewProjectionMatrix = projectionMatrix * viewMatrix * modelMatrix;
            invModelMatrix = modelMatrix.GetUpperLeft().Inverse();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexShaderOutput Run(Vector3D vertex, VertexAttributes attributes)
        {
            return new VertexShaderOutput()
            {
                ClipPosition = modelViewProjectionMatrix * vertex,
                WorldNormal = attributes.Normal * invModelMatrix,
                WorldPosition = (modelMatrix * vertex).Truncate(),
                TexCoords = attributes.UV,
            };
        }
    }
}
