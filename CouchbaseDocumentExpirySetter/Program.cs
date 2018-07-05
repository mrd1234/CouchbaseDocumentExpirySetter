namespace CouchbaseDocumentExpirySetter
{
    using System;

    using CommandLine;

    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();

            Options options = null;
            Parser.Default.ParseArguments<Options>(args).WithParsed(opts => options = opts);

            if (options != null)
            {
                if (options.DocumentLimit.HasValue && options.DocumentLimit < options.BatchSize) options.BatchSize = options.DocumentLimit.Value;

                try
                {
                    var logger = string.IsNullOrEmpty(options.LogFile) ? null : new LogManager(options.LogFile);

                    try
                    {
                        PerformUpdate(options, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.Flush();
                        Console.WriteLine($"A fatal exception was encountered: {ex}");
                    }
                    finally
                    {
                        logger?.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Game over, thanks for playing...");
            Console.ReadKey();
        }

        private static void PerformUpdate(Options options, LogManager logManager)
        {
            var updater = new DocumentExpiryUpdater(options, logManager);
            updater.UpdateExpiryForDocumentsInBucketsAsync().Wait();
        }
    }
}