using BenchmarkDotNet.Running;

namespace SoftRender.Benchmark
{
    public class Program
    {
        static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}