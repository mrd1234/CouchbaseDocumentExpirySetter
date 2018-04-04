using System;
using CommandLine;

namespace CouchbaseDocumentExpirySetter
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();

            Options options = null;
            Parser.Default.ParseArguments<Options>(args).WithParsed(opts => options = opts);

            if (options != null)
            {
                PerformUpdate(options);
            }

            Console.WriteLine();
            Console.WriteLine("Game over, thanks for playing...");
            Console.ReadKey();
        }

        private static void PerformUpdate(Options options)
        {
            var updater = new DocumentExpiryUpdater(options);
            updater.UpdateExpiryForDocumentsInBuckets().Wait();
        }
    }
}