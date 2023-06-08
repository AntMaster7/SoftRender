namespace SoftRender
{
    public interface ISampler
    {
        ColorRGB Sample(float u, float v);
    }
}