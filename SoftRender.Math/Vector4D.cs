﻿using System.Diagnostics;

namespace SoftRender.SRMath
{
    // chat gpt
    public struct Vector4D
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

        public static float Dot(Vector4D v1, Vector4D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z + v1.W * v2.W;
        }

        public float Magnitude()
        {
            return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        }

        public Vector4D Normalize()
        {
            float magnitude = Magnitude();

            if (System.Math.Abs(magnitude) < 1e-6)
            {
                throw new ArgumentException("Cannot normalize a zero vector");
            }

            return new Vector4D(X / magnitude, Y / magnitude, Z / magnitude, W / magnitude);
        }

        public Vector3D PerspectiveDivide()
        {
            //Debug.Assert(W != 0);
            if(W == 0) // TODO: Why does this even happen?
            {
                return new Vector3D(0, 0, 0);
            }
            else if(W == 1)
            {
                return new Vector3D(X, Y, Z);
            }
            else
            {
                return new Vector3D(X / W, Y / W, Z / W);
            }
        }

        public Vector3D Truncate() => new Vector3D(X, Y, Z);
    }
}
