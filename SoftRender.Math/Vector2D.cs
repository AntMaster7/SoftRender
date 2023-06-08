using System.Drawing;

namespace SoftRender.SRMath
{
    // chat gpt
    public class Vector2D
    {
        public float X;
        public float Y;

        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2D(Point p) : this(p.X, p.Y)
        {
        }

        public static Vector2D operator +(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2D operator *(Vector2D v, int scalar)
        {
            return new Vector2D(v.X * scalar, v.Y * scalar);
        }

        public float Dot(Vector2D v)
        {
            return X * v.X + Y * v.Y;
        }

        public void Translate(float dx, float dy)
        {
            X += dx;
            Y += dy;
        }

        public Point ToPoint()
        {
            return new Point((int)(X + 0.5), (int)(Y + 0.5));
        }

        public override string ToString()
        {
            return $"(X={X}, Y={Y})";
        }
    }
}
