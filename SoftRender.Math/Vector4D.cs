namespace SoftRender.SRMath
{
    // chat gpt
    public class Vector4D
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vector4D(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        // Vector Addition
        public static Vector4D operator +(Vector4D v1, Vector4D v2)
        {
            return new Vector4D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
        }

        // Vector Subtraction
        public static Vector4D operator -(Vector4D v1, Vector4D v2)
        {
            return new Vector4D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
        }

        // Scalar Multiplication
        public static Vector4D operator *(Vector4D v, float scalar)
        {
            return new Vector4D(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);
        }

        // Scalar Division
        public static Vector4D operator /(Vector4D v, float scalar)
        {
            if (System.Math.Abs(scalar) < 1e-6)
            {
                throw new ArgumentException("Cannot divide by zero");
            }

            return new Vector4D(v.X / scalar, v.Y / scalar, v.Z / scalar, v.W / scalar);
        }

        // Dot Product
        public static float Dot(Vector4D v1, Vector4D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z + v1.W * v2.W;
        }

        // Magnitude (length) of the Vector
        public float Magnitude()
        {
            return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        }

        // Normalization
        public Vector4D Normalize()
        {
            float magnitude = Magnitude();

            if (System.Math.Abs(magnitude) < 1e-6)
            {
                throw new ArgumentException("Cannot normalize a zero vector");
            }

            return new Vector4D(X / magnitude, Y / magnitude, Z / magnitude, W / magnitude);
        }
    }
}
