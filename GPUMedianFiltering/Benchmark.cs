using System;
using BenchmarkDotNet.Attributes;

namespace GPUMedianFiltering
{
    [SimpleJob(launchCount: 1, warmupCount: 0, targetCount: 1)]
    public class Benchmark
    {
        private MedianFilter _filter;
        private Image _image;
        [GlobalSetup]
        public void GlobalSetup()
        {
            _image = new Image(Program.Path);
            _filter = new MedianFilter(_image);
        }

        [Params(1, 4, 8, 16, 32)] public int Threads;

        [Benchmark]
        public void TestOnCPu() => _filter.CalculateOnCPU(Threads);

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _image?.Dispose();
        }
    }
}