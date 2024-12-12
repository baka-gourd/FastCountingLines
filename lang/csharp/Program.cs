long CountLinesParallelWithCount(string filePath)
{
    long fileSize = new FileInfo(filePath).Length;
    int numTasks = Environment.ProcessorCount;
    long chunkSize = fileSize / numTasks;
    long totalLineCount = 0;

    Parallel.For(0, numTasks, i =>
    {
        long start = i * chunkSize;
        long end = (i == numTasks - 1) ? fileSize : (start + chunkSize);
        long localLineCount = 0;

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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

        System.Threading.Interlocked.Add(ref totalLineCount, localLineCount);
    });

    return totalLineCount;
}