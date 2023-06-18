namespace SoftRender.SRMath
{
    public class Matrix3D
    {
        public float M11;
        public float M12;
        public float M13;

        public float M21;
        public float M22;
        public float M23;

        public float M31;
        public float M32;
        public float M33;

        public Matrix3D(float m11, float m12, float m13,
                        float m21, float m22, float m23,
                        float m31, float m32, float m33)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M31 = m31;
            M32 = m32;
            M33 = m33;
        }

        public static Matrix3D CreateIdentity()
        {
            return new Matrix3D(
                1.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 1.0f);
        }

        public static Matrix3D CreateZero()
        {
            return new Matrix3D(
                0, 0, 0,
                0, 0, 0,
                0, 0, 0);
        }

        public static Matrix3D operator +(Matrix3D m1, Matrix3D m2)
        {
            float m11 = m1.M11 + m2.M11;
            float m12 = m1.M12 + m2.M12;
            float m13 = m1.M13 + m2.M13;

            float m21 = m1.M21 + m2.M21;
            float m22 = m1.M22 + m2.M22;
            float m23 = m1.M23 + m2.M23;

            float m31 = m1.M31 + m2.M31;
            float m32 = m1.M32 + m2.M32;
            float m33 = m1.M33 + m2.M33;

            return new Matrix3D(m11, m12, m13,
                                m21, m22, m23,
                                m31, m32, m33);
        }

        public static Matrix3D operator *(Matrix3D m1, Matrix3D m2)
        {
            float m11 = m1.M11 * m2.M11 + m1.M12 * m2.M21 + m1.M13 * m2.M31;
            float m12 = m1.M11 * m2.M12 + m1.M12 * m2.M22 + m1.M13 * m2.M32;
            float m13 = m1.M11 * m2.M13 + m1.M12 * m2.M23 + m1.M13 * m2.M33;

            float m21 = m1.M21 * m2.M11 + m1.M22 * m2.M21 + m1.M23 * m2.M31;
            float m22 = m1.M21 * m2.M12 + m1.M22 * m2.M22 + m1.M23 * m2.M32;
            float m23 = m1.M21 * m2.M13 + m1.M22 * m2.M23 + m1.M23 * m2.M33;

            float m31 = m1.M31 * m2.M11 + m1.M32 * m2.M21 + m1.M33 * m2.M31;
            float m32 = m1.M31 * m2.M12 + m1.M32 * m2.M22 + m1.M33 * m2.M32;
            float m33 = m1.M31 * m2.M13 + m1.M32 * m2.M23 + m1.M33 * m2.M33;

            return new Matrix3D(m11, m12, m13,
                                m21, m22, m23,
                                m31, m32, m33);
        }

        public static Vector3D operator *(Matrix3D m, Vector3D v)
        {
            float x = m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z;
            float y = m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z;
            float z = m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z;

            return new Vector3D(x, y, z);
        }

        public static Vector3D operator *(Vector3D v, Matrix3D m)
        {
            float x = m.M11 * v.X + m.M21 * v.Y + m.M31 * v.Z;
            float y = m.M12 * v.X + m.M22 * v.Y + m.M32 * v.Z;
            float z = m.M13 * v.X + m.M23 * v.Y + m.M33 * v.Z;

            return new Vector3D(x, y, z);
        }

        public float GetDeterminant()
        {
            return M11 * (M22 * M33 - M32 * M23) -
                   M12 * (M21 * M33 - M31 * M23) +
                   M13 * (M21 * M32 - M31 * M22);
        }

        public Matrix3D Inverse()
        {
            // Chat gpt helped with inlining

            // Math is explained at https://www.onlinemathstutor.org/post/3x3_inverses

            // Calculate the cross products
            float bxcX = M22 * M33 - M23 * M32;
            float bxcY = M23 * M31 - M21 * M33;
            float bxcZ = M21 * M32 - M22 * M31;

            float cxaX = M32 * M13 - M33 * M12;
            float cxaY = M33 * M11 - M31 * M13;
            float cxaZ = M31 * M12 - M32 * M11;

            float axbX = M12 * M23 - M13 * M22;
            float axbY = M13 * M21 - M11 * M23;
            float axbZ = M11 * M22 - M12 * M21;

            // Calculate the determinant of the original matrix (via dot product of 'a' with 'b x c')
            float det = M11 * bxcX + M12 * bxcY + M13 * bxcZ;

            // Get the inverse of the determinant
            float idet = 1 / det;

            // Return the transposed matrix of cofactors scaled by 1 / det
            return new Matrix3D(
                idet * bxcX, idet * bxcY, idet * bxcZ,
                idet * cxaX, idet * cxaY, idet * cxaZ,
                idet * axbX, idet * axbY, idet * axbZ);
        }
    }
}
