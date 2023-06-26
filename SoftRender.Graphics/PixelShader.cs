using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender.Graphics
{
    public class PixelShader
    {
        private static readonly Vector256<float> MaxColor = Vector256.Create(255f);

        private readonly NearestSampler sampler;

        public Vector256<float> Us;
        
        public Vector256<float> Vs;

        public Vector3DPacket FragmentPositions;

        public Vector3DPacket Normals;

        public PixelShader(NearestSampler sampler)
        {
            this.sampler = sampler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(PixelPacket pixel, VertexShaderOutput input)
        {
            sampler.Sample(Us, Vs, pixel);

            float ambientLightIntensity = 0.0f;

            var rsdiffuse = Avx.ConvertToVector256Single(pixel.Rs);
            var gsdiffuse = Avx.ConvertToVector256Single(pixel.Gs);
            var bsdiffuse = Avx.ConvertToVector256Single(pixel.Bs);

            var rsambient = rsdiffuse * ambientLightIntensity;
            var gsambient = gsdiffuse * ambientLightIntensity;
            var bsambient = bsdiffuse * ambientLightIntensity;

            var illum = Vector256.Create(0.9f);
            
            var dot = LightDirs.Xs * Normals.Xs + LightDirs.Ys * Normals.Ys + LightDirs.Zs * Normals.Zs;
            dot = Avx.Max(dot, FastRasterizer.Zeros);

            var rsdirect = rsdiffuse * illum * dot;
            var gsdirect = gsdiffuse * illum * dot;
            var bsdirect = bsdiffuse * illum * dot;

            pixel.Rs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, rsambient + rsdirect));
            pixel.Gs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, gsambient + gsdirect));
            pixel.Bs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, bsambient + bsdirect));
        }
    }
}
