﻿using SoftRender.SRMath;
using System.Diagnostics;

namespace SoftRender.Graphics
{
    public class Renderer
    {
        private readonly IRasterizer rasterizer;
        private readonly Stopwatch frameTimer = new Stopwatch();

        public int Iterations { get; set; } = 10;

        public VertexShader? VertexShader { get; set; }

        public ISampler? Texture { get; set; }

        public Renderer(IRasterizer rasterizer)
        {
            this.rasterizer = rasterizer;
        }

        public TimeSpan Render(Vector3D[] Vertices, VertexAttributes[] Attributes)
        {
            Debug.Assert(Texture != null);
            Debug.Assert(VertexShader != null);
            Debug.Assert(Vertices.Length % 3 == 0);

            Span<Vector4D> cs = new Vector4D[Vertices.Length];
            Span<VertexAttributes> at = new VertexAttributes[Vertices.Length];
            Span<Vector3D> ns = new Vector3D[Vertices.Length];
            Span<VertexShaderOutput> vso = new VertexShaderOutput[Vertices.Length];

            // allows for quicker access
            var vertexShader = VertexShader;
            var texture = Texture;

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

            FastRasterizer fastRasterizer = (FastRasterizer)rasterizer;

            frameTimer.Reset();
            frameTimer.Start();

            for (int iter = 0; iter < Iterations; iter++)
            {
                for (int i = 0; i < cs.Length; i += 3)
                {
                    fastRasterizer.Face = i / 3;
                    fastRasterizer.Rasterize(vso.Slice(i, 3), texture);
                }
            }

            frameTimer.Stop();

            return frameTimer.Elapsed;
        }
    }
}
