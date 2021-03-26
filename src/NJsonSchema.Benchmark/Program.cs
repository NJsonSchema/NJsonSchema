using System.Runtime.CompilerServices;

namespace NJsonSchema.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // RunCsharpBenchmark();
            BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
        }

        private static void RunCsharpBenchmark()
        {
            var benchmark = new CsharpGeneratorBenchmark();
            benchmark.Setup().GetAwaiter().GetResult();
            benchmark.GenerateFile();
            RunCode(benchmark);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunCode(CsharpGeneratorBenchmark benchmark)
        {
            for (int i = 0; i < 100; ++i)
            {
                benchmark.GenerateFile();
            }
        }
    }
}