using SoftRender.SRMath;

namespace SoftRender.Graphics
{
    public class Light
    {
        public Matrix4D Transform;

        public Vector3D GetWorldPosition() => (Transform * new Vector3D(0, 0, 0)).Truncate();
    }
}
