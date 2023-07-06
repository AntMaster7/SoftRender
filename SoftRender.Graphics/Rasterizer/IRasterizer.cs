namespace SoftRender.Graphics
{
    public interface IRasterizer
    {
        void Rasterize(Span<VertexShaderOutput> input, PixelShader pixelShader);
    }
}