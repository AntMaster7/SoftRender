namespace SoftRender.SRMath
{
    public class Point2D
    {
        public float X;
        public float Y;
        public float W;

        public Point2D()
        {

        }

        public Point2D(float x, float y, float w)
        {
            X = x;
            Y = y;
            W = w;
        }

        public override string ToString()
        {
            return $"(X={X}, Y={Y}, W={W})";
        }

        public Point2D PerspectiveDivide()
        {
            if (W == 0)
            {
                throw new InvalidOperationException("Point is at infinity.");
            }

            return new Point2D(X / W, Y / W, 1);
        }
    }
}
