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
                PerformUpdate(options);
            }

            Console.WriteLine();
            Console.WriteLine("Game over, thanks for playing...");
            Console.ReadKey();
        }

        private static void PerformUpdate(Options options)
        {
            var updater = new DocumentExpiryUpdater(options);
            updater.UpdateExpiryForDocumentsInBucketsAsync().Wait();
        }
    }
}