using System.Drawing;

namespace SoftRender
{
    public interface IRasterizer
    {
        void Rasterize(Point[] face);
    }
}