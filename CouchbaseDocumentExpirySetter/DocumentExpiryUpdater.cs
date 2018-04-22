namespace CouchbaseDocumentExpirySetter
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Couchbase;
    using Couchbase.Core;

    public class DocumentExpiryUpdater
    {
        private Options Options { get; }

        private int MaxActiveTasks { get; } = 100;

        public DocumentExpiryUpdater(Options options)
        {
            Options = options;

            ClusterHelper.Initialize(options.BuildClientConfiguration());
        }

        public Task UpdateExpiryForDocumentsInBucketsAsync()
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

                //for (var x = 0; x < 50000; x++)
                //{
                //    await bucket.InsertAsync<Options>(new Document<Options> { Id = Guid.NewGuid().ToString() }).ConfigureAwait(false);
                //}

                var sw = new Stopwatch();
                sw.Start();

                var documents = await DocumentHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials()).ConfigureAwait(false);
                while (documents.TotalRows != 0)
                {
                    documents.Expiry = TimeSpan.FromMinutes(Options.ExpiryMinutes);
                    await SetExpiryForBucketDocumentsAsync(bucket, documents).ConfigureAwait(false);

                    processedDocuments += documents.TotalRows;

                    if (documents.Expiry.TotalSeconds.Equals(0)) address = Options.BuildRestUrl(bucketName, processedDocuments);

                    documents = await DocumentHelper.GetDocumentsWithNoExpiryAsync(address, Options.GetCredentials()).ConfigureAwait(false);
                }

                sw.Stop();

                Console.WriteLine();
                Console.WriteLine($"Processing {processedDocuments} documents in {bucketName} bucket took {sw.Elapsed.TotalSeconds} seconds");
            }
        }

        private Task SetExpiryForBucketDocumentsAsync(IBucket bucket, DocumentList documentList)
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

            var result = bucket.Touch(documentId, ttl);
            return Task.CompletedTask;
        }
    }
}
