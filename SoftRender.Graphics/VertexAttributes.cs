namespace SoftRender.Graphics
{
    public class VertexAttributes
    {
        public float Z;

        public int R;
        public int G;
        public int B;

        public float U;
        public float V;

        public VertexAttributes()
        {
        }

        public VertexAttributes(float z, int r, int g, int b, float u, float v)
        {
            Z = z;
            R = r;
            G = g;
            B = b;
            U = u;
            V = v;
        }
    }
}
