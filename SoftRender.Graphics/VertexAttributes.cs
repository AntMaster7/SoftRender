namespace SoftRender.Graphics
{
    public class VertexAttributes
    {
        public float Z;

        public int R;
        public int G;
        public int B;

        public VertexAttributes()
        {
        }

        public VertexAttributes(float z, int r, int g, int b)
        {
            Z = z;
            R = r;
            G = g;
            B = b;
        }
    }
}
