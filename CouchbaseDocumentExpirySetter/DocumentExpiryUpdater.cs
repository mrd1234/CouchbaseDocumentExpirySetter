using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core;

namespace CouchbaseDocumentExpirySetter
{
    public class DocumentExpiryUpdater
    {
        private Options Options { get; }

        private int MaxActiveTasks { get; } = 100;

        public DocumentExpiryUpdater(Options options)
        {
            Options = options;

            ClusterHelper.Initialize(options.BuildClientConfiguration());
        }

        public Task UpdateExpiryForDocumentsInBuckets()
        {
            var updates = new List<Task>();

            var buckets = Options.GetBucketDetails();

            foreach (var bucket in buckets)
            {
                Console.WriteLine($"Processing documents in {bucket.Key}...");

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

                var documents = await WebHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials());
                while (documents.TotalRows != 0)
                {
                    documents.Expiry = TimeSpan.FromMinutes(Options.ExpiryMinutes);
                    await SetExpiryForBucketDocuments(bucket, documents);

                    processedDocuments += documents.TotalRows;

                    if (documents.Expiry.TotalSeconds.Equals(0)) address = Options.BuildRestUrl(bucketName, processedDocuments);

                    documents = await WebHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials());
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine($"Processing {processedDocuments} documents in {bucketName} bucket took {sw.Elapsed.TotalSeconds} seconds");
            }
        }

        private Task SetExpiryForBucketDocuments(IBucket bucket, DocumentList documentList)
        {
            var runningTasks = new List<Task>();

            foreach (var document in documentList.Rows)
            {
                runningTasks.Add(SetDocumentExpiryAsync(bucket, document.Id, documentList.Expiry));
            }

            // Wait for any tasks still running to complete
            return TaskHelper.WaitAllThrottledAsync(runningTasks, MaxActiveTasks);
        }

        private static Task SetDocumentExpiryAsync(IBucket bucket, string documentId, TimeSpan ttl)
        {
            //Console.WriteLine($"Updating expiry for {documentId}");
            //return bucket.TouchAsync(documentId, ttl);

            bucket.Touch(documentId, ttl);
            return Task.CompletedTask;
        }
    }
}
