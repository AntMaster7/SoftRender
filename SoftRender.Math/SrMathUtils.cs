using System.Drawing;

namespace SoftRender.Math
{
    public class SrMathUtils
    {
        /// <summary>
        /// Orders the given points in counter-clockwise order.
        /// </summary>
        /// <param name="a">A reference to point a.</param>
        /// <param name="b">A reference to point b.</param>
        /// <param name="c">A reference to point c.</param>
        public static void OrderCounterclockWise(ref Point a, ref Point b, ref Point c)
        {
            // Order points by x descending e.g. a will be the right most point.
            if (a.X < b.X) Swap(ref a, ref b);
            if (a.X < c.X) Swap(ref b, ref c);
            if (b.X < c.X) Swap(ref b, ref c);

            // Checks if sin b-a-c is positiv or negativ using the perp product of ac and ab.
            // Might have been simpler by comparing slopes, but seems that divison is much more expensive than multiplication.
            if ((b.X - a.X) * (a.Y - c.Y) + (b.Y - a.Y) * (c.X - a.X) > 0) Swap(ref b, ref c);
        }

        /// <summary>
        /// Swaps the two given points.
        /// </summary>
        /// <param name="a">Point a.</param>
        /// <param name="b">Point b.</param>
        public static void Swap(ref Point a, ref Point b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }
    }
}
