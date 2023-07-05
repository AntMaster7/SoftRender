using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender.Graphics
{
    public class LightPacket
    {
        public Vector256<float> Xs;
        public Vector256<float> Ys;
        public Vector256<float> Zs;

        public LightPacket()
        {
        }

        public LightPacket(float xs, float ys, float zs)
        {
            Xs = Vector256.Create(xs);
            Ys = Vector256.Create(ys);
            Zs = Vector256.Create(zs);
        }
    }

    public class PixelShader
    {
        public struct PixelShaderInput
        {
            public Vector3DPacket WorldNormals;

            public Vector3DPacket WorldPositions;

            public Vector2DPacket TexCoords;
        }

        private static readonly Vector256<float> MaxColor = Vector256.Create(255f);
        private readonly TextureSampler sampler;
        private readonly LightPacket[] lights;
        private readonly Vector3DPacket lightDirs = new();

        public PixelShader(TextureSampler sampler, LightPacket[] lights)
        {
            this.sampler = sampler;
            this.lights = lights;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(PixelPacket pixel, PixelShaderInput input)
        {
            sampler.Sample(input.TexCoords.Xs, input.TexCoords.Ys, pixel);

            float ambientLightIntensity = 0.0f;

            var rsdiffuse = Avx.ConvertToVector256Single(pixel.Rs);
            var gsdiffuse = Avx.ConvertToVector256Single(pixel.Gs);
            var bsdiffuse = Avx.ConvertToVector256Single(pixel.Bs);

            var rsambient = rsdiffuse * ambientLightIntensity;
            var gsambient = gsdiffuse * ambientLightIntensity;
            var bsambient = bsdiffuse * ambientLightIntensity;

            var rsdirect = Vector256.Create(0.0f);
            var gsdirect = Vector256.Create(0.0f);
            var bsdirect = Vector256.Create(0.0f);

            for (int i = 0; i < lights.Length; i++)
            {
                var lightPos = lights[i];

                var illum = Vector256.Create(0.9f);
                lightDirs.Xs = lightPos.Xs - input.WorldPositions.Xs;
                lightDirs.Ys = lightPos.Ys - input.WorldPositions.Ys;
                lightDirs.Zs = lightPos.Zs - input.WorldPositions.Zs;
                var invSqrt = Avx.ReciprocalSqrt(lightDirs.Xs * lightDirs.Xs + lightDirs.Ys * lightDirs.Ys + lightDirs.Zs * lightDirs.Zs);
                lightDirs.Xs *= invSqrt;
                lightDirs.Ys *= invSqrt;
                lightDirs.Zs *= invSqrt;

                var dot = lightDirs.Xs * input.WorldNormals.Xs + lightDirs.Ys * input.WorldNormals.Ys + lightDirs.Zs * input.WorldNormals.Zs;
                dot = Avx.Max(dot, Rasterizer.Zeros);

                rsdirect += rsdiffuse * illum * dot;
                gsdirect += gsdiffuse * illum * dot;
                bsdirect += bsdiffuse * illum * dot;
            }

            pixel.Rs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, rsambient + rsdirect));
            pixel.Gs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, gsambient + gsdirect));
            pixel.Bs = Avx.ConvertToVector256Int32(Avx.Min(MaxColor, bsambient + bsdirect));
        }
    }
}
