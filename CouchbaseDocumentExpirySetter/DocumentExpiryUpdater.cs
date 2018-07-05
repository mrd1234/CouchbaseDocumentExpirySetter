namespace CouchbaseDocumentExpirySetter
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Couchbase;
    using Couchbase.Core;
    using Couchbase.IO;

    public class DocumentExpiryUpdater
    {
        private Options Options { get; }

        private int MaxActiveTasks { get; } = 100;

        private LogManager LogManager { get; }

        public DocumentExpiryUpdater(Options options, LogManager logManager)
        {
            Options = options;
            LogManager = logManager;

            var config = options.BuildClientConfiguration();

            ServicePointManager.DefaultConnectionLimit = MaxActiveTasks;
            foreach (var bucketConfig in config.BucketConfigs)
            {
                bucketConfig.Value.PoolConfiguration.MaxSize = MaxActiveTasks;
                bucketConfig.Value.PoolConfiguration.MinSize = MaxActiveTasks;
            }

            ClusterHelper.Initialize(config);
        }

        public Task UpdateExpiryForDocumentsInBucketsAsync()
        {
            var updates = new List<Task>();

            var buckets = Options.GetBucketDetails();

            foreach (var bucket in buckets)
            {
                Console.WriteLine($"Processing documents in bucket '{bucket.Key}'...");
                if (Options.ShowDetails) Console.WriteLine();

                updates.Add(UpdateExpiryForDocumentsInBucketAsync(bucket.Key, bucket.Value));
            }

            return Task.WhenAll(updates);
        }

        private async Task UpdateExpiryForDocumentsInBucketAsync(string bucketName, string password)
        {
            var processedDocuments = 0;

            var address = Options.BuildRestUrl(bucketName, 0);

            using (var bucket = ClusterHelper.GetBucket(bucketName, password))
            {
                var sw = new Stopwatch();
                sw.Start();

                var documents = await DocumentHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials()).ConfigureAwait(false);
                while (documents.TotalRows != 0)
                {
                    documents.Expiry = TimeSpan.FromMinutes(Options.ExpiryMinutes);

                    var limit = Options.DocumentLimit - processedDocuments;
                    await SetExpiryForBucketDocumentsAsync(bucket, documents, limit, Options.ShowDetails).ConfigureAwait(false);

                    processedDocuments += documents.TotalRows;

                    if (Options.DocumentLimit.HasValue && processedDocuments >= Options.DocumentLimit.Value) break;

                    if (documents.Expiry.TotalSeconds.Equals(0)) address = Options.BuildRestUrl(bucketName, processedDocuments);

                    documents = await DocumentHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials()).ConfigureAwait(false);
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine($"Processing {processedDocuments} documents in bucket '{bucketName}' took {sw.Elapsed.TotalSeconds} seconds");
            }
        }

        private Task SetExpiryForBucketDocumentsAsync(IBucket bucket, DocumentList documentList, int? documentLimit, bool showDetails)
        {
            var runningTasks = new List<Task>();

            var processedCount = 0;

            foreach (var document in documentList.Rows)
            {
                runningTasks.Add(SetDocumentExpiryAsync(bucket, document.Id, documentList.Expiry, showDetails));
                processedCount++;

                if (documentLimit.HasValue && processedCount == documentLimit.Value) break;
            }

            // Wait for any tasks still running to complete
            return TaskHelper.WaitAllThrottledAsync(runningTasks, MaxActiveTasks);
        }

        private Task SetDocumentExpiryAsync(IBucket bucket, string documentId, TimeSpan ttl, bool showDetails)
        {
            //Console.WriteLine($"Updating expiry for {documentId}");

            //The async method seems to have issues so I don't consider it reliable at this time
            //var result = bucket.TouchAsync(documentId, ttl);

            var result = bucket.Touch(documentId, ttl);

            if (result.Status == ResponseStatus.Success)
            {
                if (showDetails) Console.WriteLine($"Expiry for document id {documentId} set to {ttl.TotalMinutes} minute(s)");
                if (LogManager == null) return Task.CompletedTask;

                var expectedExpiry = DateTime.Now.Add(ttl);
                LogManager.Log(documentId, ttl, $"{expectedExpiry.ToShortDateString()} {expectedExpiry.ToLongTimeString()}");

                return Task.CompletedTask;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unable to set expiry value for documentId '{documentId}' - {result.Status}");
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}
