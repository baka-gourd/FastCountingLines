# Description

## Benchmark Results

<details>
<summary>Code</summary>

```csharp
using BenchmarkDotNet.Attributes;

namespace Nptr.Benchmark;

public class LineCountBenchmarks
{
    private const string FilePath = @"path/to/file.txt"; // 替换为您的大文件路径

    [Benchmark]
    public long CountLines_StreamReader()
    {
        long lineCount = 0;
        using (StreamReader reader = new StreamReader(FilePath))
        {
            while (reader.ReadLine() != null)
            {
                lineCount++;
            }
        }

        return lineCount;
    }

    [Benchmark]
    public long CountLines_FileReadLines()
    {
        return File.ReadLines(FilePath).Count();
    }

    [Benchmark]
    public long CountLines_ByteCounting()
    {
        long lineCount = 0;
        const int bufferSize = 1024 * 1024;
        byte[] buffer = new byte[bufferSize];

        using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        lineCount++;
                    }
                }
            }
        }

        return lineCount;
    }

    [Benchmark]
    public long CountLines_ByteCountingWithSpan()
    {
        long lineCount = 0;
        const int bufferSize = 1024 * 1024;
        Span<byte> buffer = stackalloc byte[bufferSize];

        using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int bytesRead;
            while ((bytesRead = fs.Read(buffer)) > 0)
            {
                var span = buffer.Slice(0, bytesRead);
                foreach (byte b in span)
                {
                    if (b == '\n')
                    {
                        lineCount++;
                    }
                }
            }
        }

        return lineCount;
    }

    [Benchmark]
    public long CountLines_Parallel()
    {
        long fileSize = new FileInfo(FilePath).Length;
        int numTasks = Environment.ProcessorCount;
        long chunkSize = fileSize / numTasks;

        long totalLineCount = 0;
        Parallel.For(0, numTasks, i =>
        {
            long start = i * chunkSize;
            long end = (i == numTasks - 1) ? fileSize : start + chunkSize;

            using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(start, SeekOrigin.Begin);
                byte[] buffer = new byte[1024 * 1024];
                long localLineCount = 0;

                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && fs.Position <= end)
                {
                    for (int j = 0; j < bytesRead; j++)
                    {
                        if (buffer[j] == '\n')
                        {
                            localLineCount++;
                        }
                    }
                }

                Interlocked.Add(ref totalLineCount, localLineCount);
            }
        });

        return totalLineCount;
    }

    [Benchmark]
    public long CountLinesParallelWithSpan()
    {
        long fileSize = new FileInfo(FilePath).Length;
        int numTasks = Environment.ProcessorCount;
        long chunkSize = fileSize / numTasks;
        long totalLineCount = 0;

        Parallel.For(0, numTasks, i =>
        {
            long start = i * chunkSize;
            long end = (i == numTasks - 1) ? fileSize : (start + chunkSize);
            long localLineCount = 0;

            using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(start, SeekOrigin.Begin);
                const int bufferSize = 1024 * 1024;
                byte[] buffer = new byte[bufferSize];

                if (start > 0)
                {
                    fs.Seek(-1, SeekOrigin.Current);
                }

                int bytesRead;
                bool isAtStart = (i == 0);
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && fs.Position <= end + bufferSize)
                {
                    var span = buffer.AsSpan(0, bytesRead);

                    for (int j = 0; j < span.Length; j++)
                    {
                        if (span[j] == '\n')
                        {
                            localLineCount++;
                        }
                    }
                }
            }

            Interlocked.Add(ref totalLineCount, localLineCount);
        });

        return totalLineCount;
    }

    [Benchmark]
    public long CountLinesParallelWithIndexOf()
    {
        long fileSize = new FileInfo(FilePath).Length;
        int numTasks = Environment.ProcessorCount;
        long chunkSize = fileSize / numTasks;
        long totalLineCount = 0;

        Parallel.For(0, numTasks, i =>
        {
            long start = i * chunkSize;
            long end = (i == numTasks - 1) ? fileSize : (start + chunkSize);
            long localLineCount = 0;

            using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(start, SeekOrigin.Begin);
                const int bufferSize = 1024 * 1024;
                byte[] buffer = new byte[bufferSize];

                if (start > 0)
                {
                    fs.Seek(-1, SeekOrigin.Current);
                }

                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && fs.Position <= end + bufferSize)
                {
                    var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
                    while (true)
                    {
                        int newlineIndex = span.IndexOf((byte)'\n');
                        if (newlineIndex == -1)
                            break;

                        // 计数
                        localLineCount++;

                        span = span.Slice(newlineIndex + 1);
                    }
                }
            }

            Interlocked.Add(ref totalLineCount, localLineCount);
        });

        return totalLineCount;
    }

    [Benchmark]
    public long CountLinesParallelWithCount()
    {
        long fileSize = new FileInfo(FilePath).Length;
        int numTasks = Environment.ProcessorCount;
        long chunkSize = fileSize / numTasks;
        long totalLineCount = 0;

        Parallel.For(0, numTasks, i =>
        {
            long start = i * chunkSize;
            long end = (i == numTasks - 1) ? fileSize : (start + chunkSize);
            long localLineCount = 0;

            using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(start, SeekOrigin.Begin);
                const int bufferSize = 1024 * 1024;
                byte[] buffer = new byte[bufferSize];

                if (start > 0)
                {
                    fs.Seek(-1, SeekOrigin.Current);
                }

                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && fs.Position <= end + bufferSize)
                {
                    var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
                    localLineCount += span.Count((byte)'\n');
                }
            }

            Interlocked.Add(ref totalLineCount, localLineCount);
        });

        return totalLineCount;
    }
}
```

</details>

| Method                          | Mean       | Error    | StdDev   |
|-------------------------------- |-----------:|---------:|---------:|
| CountLines_StreamReader         | 4,577.5 ms | 65.80 ms | 61.55 ms |
| CountLines_FileReadLines        | 4,338.6 ms | 67.07 ms | 74.55 ms |
| CountLines_ByteCounting         | 2,988.2 ms | 44.52 ms | 41.64 ms |
| CountLines_ByteCountingWithSpan | 2,865.5 ms | 43.72 ms | 38.76 ms |
| CountLines_Parallel             |   371.0 ms |  7.32 ms | 13.01 ms |
| CountLinesParallelWithSpan      |   386.4 ms |  7.62 ms | 16.07 ms |
| CountLinesParallelWithIndexOf   |   341.6 ms |  6.65 ms | 10.93 ms |
| **CountLinesParallelWithCount**     |   333.4 ms |  6.55 ms |  9.39 ms |

## Summary of Performance

| **Method**                      | **Mean (ms)** | **Relative Speed** | **Description**                      |
|--------------------------------- |--------------:|-------------------:|------------------------------------- |
| CountLines_StreamReader          |  4577.5       | **13.7x slower**   | Basic line-by-line reading           |
| CountLines_FileReadLines         |  4338.6       | **13.0x slower**   | Lazy enumeration of file lines       |
| CountLines_ByteCounting          |  2988.2       | **9.0x slower**    | Counts newlines in file bytes        |
| CountLines_ByteCountingWithSpan  |  2865.5       | **8.6x slower**    | Uses `Span<byte>` for efficiency     |
| CountLines_Parallel              |   371.0       | **1.1x slower**    | Parallel processing of chunks        |
| CountLinesParallelWithSpan       |   386.4       | **1.2x slower**    | Parallel, uses `Span<byte>`          |
| CountLinesParallelWithIndexOf    |   341.6       | **1.02x slower**   | Uses `IndexOf` for faster searching  |
| **CountLinesParallelWithCount**  | **333.4**     | **Fastest (1x)**  | Optimal counting method with `Count` |

---

## Conclusion

1. Best Performer: `CountLinesParallelWithCount`
   - The best method, with an average execution time of 333.4 ms, is nearly 14x faster than the slowest method (`CountLines_StreamReader`).
   - The key to its performance is the use of `Parallel.For` to process multiple chunks simultaneously and the efficient use of `ReadOnlySpan<byte>.Count` to count newlines in a batch, avoiding unnecessary loops and conditional checks.

2. Parallelization is Key:
   - Parallel methods (`CountLines_Parallel`, `CountLinesParallelWithSpan`, `CountLinesParallelWithIndexOf`, and `CountLinesParallelWithCount`) are significantly faster due to concurrent processing on multiple threads.
   - They achieved execution times below 400 ms, which is an order-of-magnitude improvement over serial methods.

3. Optimization Techniques:
   - Methods using `Span<byte>` and `ReadOnlySpan<byte>` reduce memory allocation and GC pressure.
   - Using efficient search methods like `IndexOf` and `Count` is more performant than manual byte-by-byte traversal.
   - Switching from string processing to raw byte processing avoids the cost of character encoding/decoding, improving performance.

4. Recommendation:
   - For small files, simple approaches like `File.ReadLines` are sufficient.
   - For large files, **CountLinesParallelWithCount** is the best option as it achieves maximum speed and efficiency. It combines parallelization with modern C# optimizations (like `Count` and `ReadOnlySpan<byte>`), making it ideal for large, multi-gigabyte files.
   - If concurrency issues (like file locks) are a concern, `CountLines_ByteCountingWithSpan` offers a simpler yet fast option.

By applying modern C# optimization techniques and leveraging parallel computing, the performance can be improved by more than **14x**, turning multi-second processes into sub-second executions.
