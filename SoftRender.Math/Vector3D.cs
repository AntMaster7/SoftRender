namespace SoftRender.SRMath
{
    public struct Vector3D
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3D operator -(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3D operator *(Vector3D v, float scalar)
        {
            return new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vector3D operator /(Vector3D v, float scalar)
        {
            if (System.Math.Abs(scalar) < 1e-6)
            {
                throw new ArgumentException("Cannot divide by zero");
            }
            return new Vector3D(v.X / scalar, v.Y / scalar, v.Z / scalar);
        }

        public static float DotProduct(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2)
        {
            float x = v1.Y * v2.Z - v1.Z * v2.Y;
            float y = v1.Z * v2.X - v1.X * v2.Z;
            float z = v1.X * v2.Y - v1.Y * v2.X;
            return new Vector3D(x, y, z);
        }

        public float Magnitude()
        {
            return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public Vector3D Normalize()
        {
            float magnitude = Magnitude();
            if (System.Math.Abs(magnitude) < 1e-6)
            {
                throw new ArgumentException("Cannot normalize a zero vector");
            }
            return new Vector3D(X / magnitude, Y / magnitude, Z / magnitude);
        }

        public override string ToString()
        {
            return $"{X} {Y} {Z}";
        }
    }
}
