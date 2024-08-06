using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Serilog;

class Program
{
    private static readonly string filePath = "/home/kuzu/Desktop/output.txt";
    private static readonly int numberOfWrites = 100000;
    private static readonly int numberOfThreads = 10;

    private static readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private static readonly ManualResetEventSlim doneEvent = new ManualResetEventSlim(false);

    static void Main()
    {
        // Delete the file if it exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 10_000_000, flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        var stopwatch = Stopwatch.StartNew();

        // Start worker tasks
        var workers = new Task[numberOfThreads];
        for (int i = 0; i < numberOfThreads; i++)
        {
            int threadIndex = i; // Capture the loop variable correctly
            workers[i] = Task.Run(() => Worker(threadIndex));
        }

        

        // Wait for all worker tasks to complete
        Task.WaitAll(workers);

        // Signal that no more items will be added to the queue
        doneEvent.Set();



        // Flush and close Serilog
        Log.CloseAndFlush();

        stopwatch.Stop();
        Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("All threads have finished executing. Timestamp written to file.");
    }

    static void Worker(int threadIndex)
    {
        for (int i = 0; i < numberOfWrites; i++)
        {
            long threadId = AppDomain.GetCurrentThreadId();
            // Use Serilog to log messages
            Log.Information("Thread {ThreadIndex} :: {ThreadId} - Write {WriteNumber} - : {Timestamp}", threadIndex, threadId, i + 1, DateTime.Now);
        }

        Console.WriteLine($"Thread {threadIndex} completed its work.");
    }

}
