/*
Our source have format ARGB per pixel, so we can test two approches here.
1. Kernel calculates all values per pixel
2. Kernel calculates one value (A/R/G/B) separately
*/

extern "C" __device__
void
sort(unsigned char src[], int size = 9)
{
    // as long we have only 9 elements there is no reason to implement more efficient sorting algorithm than bubble sort.
    unsigned char temp;
    for (int i = 0; i < size - 1; ++i)
    {
        for(int j = 0; j < size - i - 1; ++j)
        {
            if(src[j] > src[j+1])
            {
                temp = src[j];
                src[j] = src[j+1];
                src[j+1] = temp;
            }
        }
    }
}


// We want to have one kernel per pixel. Not for per color. So we need move every i * 4;
extern "C" __global__ void
CalculatePerPixel(const unsigned char *source, unsigned char *destination, int totalSize, int height, int width, int stride)
{
    int i = blockDim.x * blockIdx.x + threadIdx.x;
    i *= 4;
    if(i > totalSize) return;
    //Our (X,Y) 
    int h = i / stride;
    int w = (i - h * stride) / 4;
    if(h+1 >= height || h - 1 < 0) return;
    if(w+1 >= width || w - 1 < 0) return;
    
    unsigned char A[9];
    unsigned char R[9];
    unsigned char G[9];
    unsigned char B[9];
    
    //GetKernel
    //byte B, byte G, byte R, byte A
    int pos = 0;
    int index = 0;
    for(int y = h - 1; y <= h + 1; ++y)
    {
        for(int x = w - 1; x <= w + 1; ++x)
        {
            pos = y * stride + 4 * x;
            B[index] = source[pos];
            G[index] = source[pos+1];
            R[index] = source[pos+2];
            A[index] = source[pos+3];
            index++;
        }
    }
    
    sort(A);
    sort(R);
    sort(B);
    sort(G);
    
    *(destination + i++) = B[4];
    *(destination + i++) = G[4];
    *(destination + i++) = R[4];
    *(destination + i++) = A[4];
}
