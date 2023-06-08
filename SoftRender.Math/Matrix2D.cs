namespace SoftRender.SRMath
{
    // chat gpt
    public class Matrix2D
    {
        public float M11;
        public float M12;
        public float M21;
        public float M22;

        public Matrix2D(float m11, float m12, float m21, float m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }

        public static Matrix2D operator+ (Matrix2D m1, Matrix2D m2)
        {
            return new Matrix2D(m1.M11 + m2.M11, m1.M12 + m2.M12, m1.M21 + m2.M21, m1.M22 + m2.M22);
        }

        public static Matrix2D operator- (Matrix2D m1, Matrix2D m2)
        {
            return new Matrix2D(m1.M11 - m2.M11, m1.M12 - m2.M12, m1.M21 - m2.M21, m1.M22 - m2.M22);
        }

        public static Matrix2D operator* (float scalar, Matrix2D m) => m * scalar;

        public static Matrix2D operator* (Matrix2D m, float scalar)
        {
            return new Matrix2D(m.M11 * scalar, m.M12 * scalar, m.M21 * scalar, m.M22 * scalar);
        }

        public static Vector2D operator* (Matrix2D a, Vector2D b)
        {
            float x = a.M11 * b.X + a.M12 * b.Y;
            float y = a.M21 * b.X + a.M22 * b.Y;

            return new Vector2D(x, y);
        }

        public static Matrix2D operator* (Matrix2D a, Matrix2D b)
        {
            float m11 = a.M11 * b.M11 + a.M12 * b.M21;
            float m12 = a.M11 * b.M12 + a.M12 * b.M22;
            float m21 = a.M21 * b.M11 + a.M22 * b.M21;
            float m22 = a.M21 * b.M12 + a.M22 * b.M22;

            return new Matrix2D(m11, m12, m21, m22);
        }

        public static Matrix2D Rotate(float rad)
        {
            float m11 = (float)System.Math.Cos(rad);
            float m12 = (float)-System.Math.Sin(rad);
            float m21 = -m12;
            float m22 = m11;

            return new Matrix2D(m11, m12, m21, m22);
        }

        public float GetDeterminant()
        {
            return M11 * M22 - M12 * M21;
        }
    }
}
