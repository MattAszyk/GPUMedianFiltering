using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace GPUMedianFiltering
{
    class Program
    {
        public static string Path = "C:\\lenna.bmp";
        static void Main(string[] args)
        {
            //CPU benchamrk
           BenchmarkRunner.Run<Benchmark>();
            //GPU benchmark
            //We are not using BenchmarkDotNet because our library (managedCUDA) is using CUDA own libraries to calculate
            // the time of execution.
            using (var image = new Image(Path))
            {
                using (var calculator = new MedianFilter(image))
                {
                    calculator.PrepareForGPU();
                    Console.WriteLine($"GPU time in ms: {calculator.CalculateOnGpu()}");
                    calculator.FinalizeGpu();
                }
            }
        }
    }
}