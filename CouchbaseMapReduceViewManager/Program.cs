namespace CouchbaseMapReduceViewManager
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
                PerformUpdateAsync(options);
            }

            Console.WriteLine();
            Console.WriteLine("Game over, thanks for playing...");
            Console.ReadKey();
        }

        private static void PerformUpdateAsync(Options options)
        {
            var updater = new CouchbaseViewManager(options);
            updater.AddViewsAsync().Wait();
        }
    }
}