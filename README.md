# Median filtering 

The **median filter** is a non-linear [digital filtering](https://en.wikipedia.org/wiki/Digital_filter "Digital filter") technique, often used to remove [noise](https://en.wikipedia.org/wiki/Signal_noise) from an image or signal. Such [noise reduction](https://en.wikipedia.org/wiki/Noise_reduction "Noise reduction") is a typical pre-processing step to improve the results of later processing (for example, [edge detection](https://en.wikipedia.org/wiki/Edge_detection "Edge detection") on an image). Median filtering is very widely used in digital [image processing](https://en.wikipedia.org/wiki/Image_processing "Image processing") because, under certain conditions, it preserves edges while removing noise (but see the discussion below), also having applications in [signal processing](https://en.wikipedia.org/wiki/Signal_processing "Signal processing"). (1)


# Technologies

- C#
- .NET
- BenchmarkDotNet
- managedCUDA (library for CUDA usage from C#)
- CUDA

## Algorithm conception
Optimalization of median filtering is using Divide-And-Conquer technique. 
| Device|Description |
|---|---|
|CPU|We are dividing photo on N parts (N is amount of used threads)|
|GPU|We are using shared memory from device to get access to read all colors values and calculate one pixel per thread.|
#### Colors:
We are calculating median for each of ARGB value per pixel and deciding which one should be at output image.
## Results



| Input |Result |
|---|---|
| ![lenna_original](https://i.postimg.cc/VLHcRqbW/lenna.png) | ![lenna_result](https://i.postimg.cc/fLMFZy9b/result.png) |


## Benchmarking
Computer data:
CPU: AMD Ryzen 5 4800H ( 8 cores, 16 threads) 2.9Ghz to 4.2Ghz
RAM: 16GB 2300Mhz
GPU: NVidia RTX2060 6GB
### Lenna (512x512)
|    Device| Threads |      Mean |    Error |   StdDev |
|---------- |-------- |----------:|---------:|---------:|
| CPU|       1 | 342.28 ms | 6.261 ms | 5.856 ms |
| CPU|       4 |  99.40 ms | 1.981 ms | 4.348 ms |
| CPU|       8 | 100.43 ms | 1.999 ms | 4.303 ms |
| CPU|      16 |  46.23 ms | 0.899 ms | 0.883 ms |
| CPU|      32 |  48.67 ms | 0.968 ms | 0.905 ms |
|GPU| N/A | 0.833024 ms|N/A|N/A


##### Agriculture and forestry – Altötting, Germany ( 9052 x 4965) (2)

|    Method | Threads |     Mean 
|---------- |-------- |-------- |
| CPU|       1 | 60.653 s | 
| CPU|       4 | 17.720 s |
| CPU|       8 | 16.438 s |
| CPU|      16 |  7.858 s |
| CPU|      32 |  8.161 s |
|GPU| N/A | 0.08512009 s |

##### Mining (11846 x 9945) (2)

|    Method | Threads |     Mean | 
|---------- |-------- |---------:|
| CPU|       1 | 139.89 s |    NA |
| CPU|       4 |  40.80 s |    NA |
| CPU|       8 |  43.31 s |    NA |
| CPU|      16 |  19.19 s |    NA |
| CPU|      32 |  19.40 s |    NA |
|GPU| N/A | 0.1454972 s |

As we can see switching calculations to GPU gives us a lot of improvement.


## Usage of library

    using(var image = Image(path)
    {
	    using(var filter = BasicMedianFilter(image))
	    {
	        //CPU
		    filter.CalculateOnCPU(numberOfThreads);
		    //GPU
			filter.PrepareForGPU(); // this would allocate memory on our GPU and transfer the data
			var timeOfExcecution = filter.CalculateOnGpu();
			calculator.FinalizeGpu(); //transfer the data from device to host
	    }
    }

##### References
1. [Median filter - Wikipedia](https://en.wikipedia.org/wiki/Median_filter)

2. [Free High-Resolution Satellite Images Samples | Effigis](https://effigis.com/en/solutions/satellite-images/satellite-image-samples/)I've decided not to test on 1 thread because it's taking too much time to complete the task.

