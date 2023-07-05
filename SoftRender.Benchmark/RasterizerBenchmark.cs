using BenchmarkDotNet.Attributes;
using SoftRender.Math;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SoftRender.Benchmark
{
    public unsafe class RasterizerBenchmark : IDisposable
    {
        private const int Height = 768;
        private const int Width = 1024;

        private readonly Random rnd = new Random();
        private readonly Rasterizer fastRasterizer;
        private readonly SimpleRasterizer slowRasterizer;
        private readonly byte* framebuffer;

        public RasterizerBenchmark()
        {
            framebuffer = (byte*)Marshal.AllocHGlobal(Width * Height * 3);

            //fastRasterizer = new Rasterizer(framebuffer, Width * 3);
            //slowRasterizer = new SimpleRasterizer(framebuffer, Width * 3);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)framebuffer);
        }

        public IEnumerable<object> RandomFace()
        {
            var face = new Point[]
            {
                new Point(rnd.Next(Width), rnd.Next(Height)),
                new Point(rnd.Next(Width), rnd.Next(Height)),
                new Point(rnd.Next(Width), rnd.Next(Height)),
            };

            SrMathUtils.OrderCounterclockWise(ref face[0], ref face[1], ref face[2]);

            yield return face;
        }

        [Benchmark]
        [ArgumentsSource(nameof(RandomFace))]
        public void FastRasterizer(Point[] face)
        {
            //fastRasterizer.Rasterize(face);
        }

        [Benchmark]
        [ArgumentsSource(nameof(RandomFace))]
        public void SlowRasterizer(Point[] face)
        {
            //slowRasterizer.Rasterize(face);
        }
    }
}
