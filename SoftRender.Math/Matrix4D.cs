namespace SoftRender.SRMath
{
    // chat gpt
    public class Matrix4D
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;

        public float M21;
        public float M22;
        public float M23;
        public float M24;

        public float M31;
        public float M32;
        public float M33;
        public float M34;

        public float M41;
        public float M42;
        public float M43;
        public float M44;

        public Matrix4D(float m11, float m12, float m13, float m14,
                        float m21, float m22, float m23, float m24,
                        float m31, float m32, float m33, float m34,
                        float m41, float m42, float m43, float m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        public static Matrix4D CreateIdentity()
        {
            return new Matrix4D(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static Matrix4D CreateZero()
        {
            return new Matrix4D(
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0);
        }

        /// <summary>
        /// Creates a new matrix that rotates vectors around the yaw (y) axis.
        /// </summary>
        /// <param name="angle">The angle in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4D CreateYaw(float angle)
        {
            var cos = (float)System.Math.Cos(angle);
            var sin = (float)System.Math.Sin(angle);

            return new Matrix4D(
                cos, 0.0f, sin, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                -sin, 0.0f, cos, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        /// <summary>
        /// Creates a new matrix that rotates vectors around the yaw (z) axis.
        /// </summary>
        /// <param name="angle">The angle in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4D CreateRoll(float angle)
        {
            var cos = (float)System.Math.Cos(angle);
            var sin = (float)System.Math.Sin(angle);

            return new Matrix4D(
                cos, sin, 0.0f, 0.0f,
                -sin, cos, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static Matrix4D CreateTranslate(float x, float y, float z)
        {
            return new Matrix4D(
                1.0f, 0.0f, 0.0f, x,
                0.0f, 1.0f, 0.0f, y,
                0.0f, 0.0f, 1.0f, z,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static Matrix4D CreateScale(float x, float y, float z)
        {
            return new Matrix4D(
                x, 0.0f, 0.0f, 0.0f,
                0.0f, y, 0.0f, 0.0f,
                0.0f, 0.0f, z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static Matrix4D operator +(Matrix4D m1, Matrix4D m2)
        {
            float m11 = m1.M11 + m2.M11;
            float m12 = m1.M12 + m2.M12;
            float m13 = m1.M13 + m2.M13;
            float m14 = m1.M14 + m2.M14;

            float m21 = m1.M21 + m2.M21;
            float m22 = m1.M22 + m2.M22;
            float m23 = m1.M23 + m2.M23;
            float m24 = m1.M24 + m2.M24;

            float m31 = m1.M31 + m2.M31;
            float m32 = m1.M32 + m2.M32;
            float m33 = m1.M33 + m2.M33;
            float m34 = m1.M34 + m2.M34;

            float m41 = m1.M41 + m2.M41;
            float m42 = m1.M42 + m2.M42;
            float m43 = m1.M43 + m2.M43;
            float m44 = m1.M44 + m2.M44;

            return new Matrix4D(m11, m12, m13, m14,
                                m21, m22, m23, m24,
                                m31, m32, m33, m34,
                                m41, m42, m43, m44);
        }

        public static Matrix4D operator *(Matrix4D m1, Matrix4D m2)
        {
            float m11 = m1.M11 * m2.M11 + m1.M12 * m2.M21 + m1.M13 * m2.M31 + m1.M14 * m2.M41;
            float m12 = m1.M11 * m2.M12 + m1.M12 * m2.M22 + m1.M13 * m2.M32 + m1.M14 * m2.M42;
            float m13 = m1.M11 * m2.M13 + m1.M12 * m2.M23 + m1.M13 * m2.M33 + m1.M14 * m2.M43;
            float m14 = m1.M11 * m2.M14 + m1.M12 * m2.M24 + m1.M13 * m2.M34 + m1.M14 * m2.M44;

            float m21 = m1.M21 * m2.M11 + m1.M22 * m2.M21 + m1.M23 * m2.M31 + m1.M24 * m2.M41;
            float m22 = m1.M21 * m2.M12 + m1.M22 * m2.M22 + m1.M23 * m2.M32 + m1.M24 * m2.M42;
            float m23 = m1.M21 * m2.M13 + m1.M22 * m2.M23 + m1.M23 * m2.M33 + m1.M24 * m2.M43;
            float m24 = m1.M21 * m2.M14 + m1.M22 * m2.M24 + m1.M23 * m2.M34 + m1.M24 * m2.M44;

            float m31 = m1.M31 * m2.M11 + m1.M32 * m2.M21 + m1.M33 * m2.M31 + m1.M34 * m2.M41;
            float m32 = m1.M31 * m2.M12 + m1.M32 * m2.M22 + m1.M33 * m2.M32 + m1.M34 * m2.M42;
            float m33 = m1.M31 * m2.M13 + m1.M32 * m2.M23 + m1.M33 * m2.M33 + m1.M34 * m2.M43;
            float m34 = m1.M31 * m2.M14 + m1.M32 * m2.M24 + m1.M33 * m2.M34 + m1.M34 * m2.M44;

            float m41 = m1.M41 * m2.M11 + m1.M42 * m2.M21 + m1.M43 * m2.M31 + m1.M44 * m2.M41;
            float m42 = m1.M41 * m2.M12 + m1.M42 * m2.M22 + m1.M43 * m2.M32 + m1.M44 * m2.M42;
            float m43 = m1.M41 * m2.M13 + m1.M42 * m2.M23 + m1.M43 * m2.M33 + m1.M44 * m2.M43;
            float m44 = m1.M41 * m2.M14 + m1.M42 * m2.M24 + m1.M43 * m2.M34 + m1.M44 * m2.M44;

            return new Matrix4D(m11, m12, m13, m14,
                                m21, m22, m23, m24,
                                m31, m32, m33, m34,
                                m41, m42, m43, m44);
        }

        public static Vector4D operator *(Matrix4D m, Vector4D v)
        {
            return new Vector4D(
                m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14 * v.W,
                m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24 * v.W,
                m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34 * v.W,
                m.M41 * v.X + m.M42 * v.Y + m.M43 * v.Z + m.M44 * v.W);
        }

        public static Vector4D operator *(Matrix4D m, Vector3D v)
        {
            return new Vector4D(
                m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14,
                m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24,
                m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34,
                m.M41 * v.X + m.M42 * v.Y + m.M43 * v.Z + m.M44);
        }

        public float GetDeterminant()
        {
            float determinant = M11 * GetCofactor(M22, M23, M24, M32, M33, M34, M42, M43, M44) -
                                M12 * GetCofactor(M21, M23, M24, M31, M33, M34, M41, M43, M44) +
                                M13 * GetCofactor(M21, M22, M24, M31, M32, M34, M41, M42, M44) -
                                M14 * GetCofactor(M21, M22, M23, M31, M32, M33, M41, M42, M43);

            return determinant;

            float GetCofactor(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
            {
                return m11 * (m22 * m33 - m23 * m32) -
                       m12 * (m21 * m33 - m23 * m31) +
                       m13 * (m21 * m32 - m22 * m31);
            }
        }

        public Matrix3D GetUpperLeft()
        {
            return new Matrix3D(
                M11, M12, M13,
                M21, M22, M23,
                M31, M32, M33);
        }
    }
}
