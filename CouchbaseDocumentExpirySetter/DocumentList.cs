namespace CouchbaseDocumentExpirySetter
{
    using System;
    using System.Collections.Generic;

    public class DocumentList
    {
        public string Bucket { get; set; }

        public int TotalRows => Rows.Count;

        public TimeSpan Expiry { get; set; } = TimeSpan.Zero;
        public List<Document> Rows { get; set; } = new List<Document>();
    }
}