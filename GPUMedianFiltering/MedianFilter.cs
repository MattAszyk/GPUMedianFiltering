using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ManagedCuda;
using ManagedCuda.VectorTypes;
using ManagedCuda.NVRTC;

namespace GPUMedianFiltering
{
    public unsafe class MedianFilter : IDisposable
    {
        private readonly Image _source;
        private static CudaDeviceVariable<byte> _deviceSource;
        private static CudaDeviceVariable<byte> _deviceDestination;
        private CudaKernel _kernel;
        private byte[] _ptx;
        private CudaContext _ctx;
        
        public MedianFilter(Image source)
        {
            _source = source;
        }

        public void CalculateOnCPU(int threadAmount = 16)
        {
            int divideInto = (int) Math.Sqrt(threadAmount);
            int widthPerBlock = _source.Width / divideInto;
            int heightPerBlock = _source.Height / divideInto;
            var threads = new List<Task>();
            int xStart = 1;
            int yStart = 1;
            for (int y = 0; y < divideInto; ++y)
            {
                for (int x = 0; x < divideInto; ++x)
                {
                    var xTemp = xStart;
                    var yTemp = yStart;
                    threads.Add(Task.Factory.StartNew(() =>CalculatePerBlock(xTemp, yTemp, widthPerBlock, heightPerBlock)));
                    xStart += widthPerBlock;

                }

                yStart += heightPerBlock;
                xStart = 1;
            }

            Task.WaitAll(threads.ToArray());
        }

        public void PrepareForGPU()
        {
            string filename = "kernel.cu"; //we assume the file is in the same folder...
            string fileToCompile = File.ReadAllText(filename);
            using (var rtc = new CudaRuntimeCompiler(fileToCompile, "CalculatePerPixel"))
            {
                string[] empty = default;
                rtc.Compile(empty);

                string log = rtc.GetLogAsString();

                Console.WriteLine(log);

                 _ptx = rtc.GetPTX();
            }
            _ctx = new CudaContext(0);

            _deviceSource = _source.SourceBuffer;
            _deviceDestination = new CudaDeviceVariable<byte>(_source.TotalSize);
            int threadsPerBlock = 256;
            int blocksPerGrid = (_source.Width * _source.Height + threadsPerBlock - 1) / threadsPerBlock;
            _kernel = _ctx.LoadKernelPTX(_ptx, "CalculatePerPixel");
            _kernel.BlockDimensions = new dim3(threadsPerBlock,1, 1);
            _kernel.GridDimensions = new dim3(blocksPerGrid, 1, 1);
        }
        public float CalculateOnGpu()
        {
            return _kernel.Run(_deviceSource.DevicePointer, _deviceDestination.DevicePointer, _source.TotalSize, _source.Height, _source.Width,
                _source.Stride);
        }

        public void FinalizeGpu()
        {
            _deviceDestination.CopyToHost(_source.DestinationBuffer);
        }
        
        private void FulfillKernel(int _x, int _y, List<(byte B, byte G, byte R, byte A)> kernelTable)
        {
            int pos;
            int iterator = 0;
            for (int y = _y - 1; y <= _y + 1; ++y)
            {
                for (int x = _x - 1; x <= _x + 1; ++x)
                {
                    pos = y * _source.Stride + 4 * x;
                    kernelTable[iterator++] = (_source.SourceBuffer[pos], _source.SourceBuffer[pos+1], _source.SourceBuffer[pos+2], _source.SourceBuffer[pos+3]);
                }
            }
        }

        /// <summary>
        /// Make sure the parameter doesn't go outside Image!
        /// </summary>
        private void SimpleMedianFilteringForBlock(int xStart, int yStart, int xEnd, int yEnd)
        {
            int pos = 0;
            var kernelTable = new List<(byte B, byte G, byte R, byte A)>(9);
            for (int i = 0; i < 9; ++i)
                kernelTable.Add((0, 0, 0,0));
            // We want escape borders values to get our kernel safely
            var tempValues = new byte[4]; //BGRA
            for (int y = yStart; y < yEnd; ++y)
            {
                for (int x = xStart; x < xEnd; ++x)
                {
                    FulfillKernel(x, y, kernelTable);
                    kernelTable.Sort((a, b) => a.B.CompareTo(b.B));
                    tempValues[0] = kernelTable[4].B;
                    kernelTable.Sort((a, b) => a.G.CompareTo(b.G));
                    tempValues[1] = kernelTable[4].G;
                    kernelTable.Sort((a, b) => a.R.CompareTo(b.R));
                    tempValues[2] = kernelTable[4].R;
                    kernelTable.Sort((a, b) => a.A.CompareTo(b.A));
                    tempValues[3] = kernelTable[4].A;
                    tempValues.CopyTo(_source.DestinationBuffer, y * _source.Stride + x * 4 );
                }
            }
        }

        /*
         * Function mark tuple (xStart, yStart) as left upper corner and add width and height to get
         * bottom right one. If right bottom corner is out of image it moved into maximum fixed one.
         */
        private void CalculatePerBlock(int xStart, int yStart, int widthPerBlock, int heightPerBlock)
        {
            int xEnd = xStart + widthPerBlock;
            int yEnd = yStart + heightPerBlock;

            //Outside the image. Nothing to calculate.
            if (xStart >= _source.Width || yStart >= _source.Height)
                return;
            //Boundaries check
            if (xEnd >= _source.Width)
                xEnd = _source.Width - 1;

            if (yEnd >= _source.Height)
                yEnd = _source.Height - 1;

            SimpleMedianFilteringForBlock(xStart, yStart, xEnd, yEnd);
        }

        public void Dispose()
        {
            _deviceSource?.Dispose();
            _deviceDestination?.Dispose();
            _ctx?.Dispose();
        }
    }
}