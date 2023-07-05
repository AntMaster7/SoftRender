using SoftRender.SRMath;
using System.Diagnostics;

namespace SoftRender.Graphics
{
    public class Renderer
    {
        private readonly IRasterizer rasterizer;
        private readonly Stopwatch frameTimer = new Stopwatch();

        public int Iterations { get; set; } = 100;

        public VertexShader? VertexShader { get; set; }

        public PixelShader? PixelShader { get; set; }

        public Renderer(IRasterizer rasterizer)
        {
            this.rasterizer = rasterizer;
        }

        public TimeSpan Render(Vector3D[] Vertices, VertexAttributes[] Attributes, Light[] lights)
        {
            Debug.Assert(VertexShader != null);
            Debug.Assert(PixelShader != null);
            Debug.Assert(Vertices.Length % 3 == 0);

            Span<VertexShaderOutput> vso = new VertexShaderOutput[Vertices.Length];

            // allows for quicker access
            var vertexShader = VertexShader;

            // var opts = new ParallelOptions
            // {
            //     MaxDegreeOfParallelism = Environment.ProcessorCount,
            // };
            // Parallel.For(0, 100, opts, (iter) =>
            // Task.Factory.StartNew(() =>
            for (int i = 0; i < Vertices.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    vso[i + j] = vertexShader.Run(Vertices[i + j], Attributes[i + j]);
                }
            }

            Rasterizer fastRasterizer = (Rasterizer)rasterizer;

            frameTimer.Reset();
            frameTimer.Start();

            for (int iter = 0; iter < Iterations; iter++)
            {
                for (int i = 0; i < Vertices.Length; i += 3)
                {
                    fastRasterizer.Face = i / 3;
                    fastRasterizer.Rasterize(vso.Slice(i, 3), PixelShader);
                }
            }

            frameTimer.Stop();

            return frameTimer.Elapsed;
        }
    }
}
