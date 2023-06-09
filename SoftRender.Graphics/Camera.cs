using SoftRender.SRMath;

namespace SoftRender
{
    public class Camera
    {
        /// <summary>
        /// Gets or sets the vertical field of view in degrees.
        /// </summary>
        public float FieldOfView { get; set; }

        /// <summary>
        /// Gets or sets the aspect ration of the screen. The aspect ratio is defined as width / height.
        /// </summary>
        public float AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the distance of the far plane.
        /// </summary>
        /// <remarks>
        /// Number must be bigger than the near plane distance.
        /// </remarks>
        public float FarPlane { get; set; }

        /// <summary>
        /// Gets or sets the distance of the near plane.
        /// </summary>
        /// <remarks>
        /// Number must be bigger than 0.
        /// </remarks>
        public float NearPlane { get; set; }

        /// <summary>
        /// Creates a matrix for the frustum projection. The depth is reversed.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public Matrix4D CreateProjectionMatrix()
        {
            var focal = 1 / (float)System.Math.Tan(AspectRatio / 2);
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
