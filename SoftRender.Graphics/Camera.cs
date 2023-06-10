using SoftRender.SRMath;

namespace SoftRender
{
    public class Camera
    {
        /// <summary>
        /// Gets or sets the aspect ration of the screen. The aspect ratio is defined as width / height.
        /// </summary>
        public float AspectRatio;

        /// <summary>
        /// Gets or sets the vertical field of view in degrees.
        /// </summary>
        public float FieldOfView;

        /// <summary>
        /// Gets or sets the distance of the near plane.
        /// </summary>
        /// <remarks>
        /// Number must be bigger than 0.
        /// </remarks>
        public float NearPlane;

        /// <summary>
        /// Gets or sets the distance of the far plane.
        /// </summary>
        /// <remarks>
        /// Number must be bigger than the near plane distance.
        /// </remarks>
        public float FarPlane;

        public Vector3D Forward = new Vector3D(0, 0, -1);

        public Camera(float aspectRatio, float fieldOfView = 90, float nearPlane = 0.05f, float farPlane = 100f)
        {
            AspectRatio = aspectRatio;
            FieldOfView = fieldOfView;
            NearPlane = nearPlane;
            FarPlane = farPlane;
        }

        /// <summary>
        /// Creates a matrix for the frustum projection. The depth is reversed.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public Matrix4D CreateProjectionMatrix()
        {
            var fovRad = FieldOfView / 180 * System.Math.PI;
            var focal = -1 / (float)System.Math.Tan(fovRad / 2);
            var reverseRange = NearPlane - FarPlane;

            var m = Matrix4D.CreateZero();
            m.M11 = focal / AspectRatio;
            m.M22 = focal;
            m.M33 = NearPlane / reverseRange;
            m.M34 = -(NearPlane * FarPlane) / reverseRange;
            m.M43 = 1;

            return m;
        }
    }
}
