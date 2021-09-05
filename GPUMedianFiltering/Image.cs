using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GPUMedianFiltering
{
    public unsafe class Image : IDisposable
    {
        private Bitmap _source;
        private BitmapData _bitmapData;
        private IntPtr ptr;
        public readonly int Stride;
        public readonly int TotalSize;
        public readonly int Width;
        public readonly int Height;
        public byte[] SourceBuffer
        {
            get; 
        }

        public byte[] DestinationBuffer
        {
            get;
        }
        
        public Image(string path = "C:\\lenna.bmp")
        {
            //read bmp into Bitmap and get access to data pointer
            _source = new Bitmap(path);
            Rectangle rect = new Rectangle(0, 0, _source.Width, _source.Height);
            _bitmapData =
                _source.LockBits(rect, ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
            Stride = _bitmapData.Stride;
            Width = _bitmapData.Width;
            Height = _bitmapData.Height;
            
            //Transfer and reserve memory for buffor
            ptr = _bitmapData.Scan0;
            TotalSize = _bitmapData.Height * _bitmapData.Stride;
            SourceBuffer = new byte[TotalSize];
            DestinationBuffer = new byte[TotalSize];
            Marshal.Copy(ptr, SourceBuffer, 0, TotalSize);
        }
        
        
        public void Dispose()
        {
            Marshal.Copy(DestinationBuffer, 0, ptr, TotalSize);
            _source.UnlockBits(_bitmapData);
            _source.Save("result.bmp");
        }
    }
}
