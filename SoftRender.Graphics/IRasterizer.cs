using SoftRender.Graphics;

namespace SoftRender
{
    public interface IRasterizer
    {
        void Rasterize(Span<VertexShaderOutput> input, ISampler texture);
    }
}