namespace CouchbaseMapReduceViewManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using CommandLine;
    using Couchbase.Configuration.Client;

    public class Options
    {
        [Option('h', "host", Required = true, HelpText = "The hostname of the Couchbase server")]
        public string HostName { get; set; }

        [Option('b', "buckets", Required = true, Separator = ',', HelpText = "The Couchbase bucket(s) to process, eg: bucket1:password1,bucket2:password2")]
        public IEnumerable<string> Buckets { get; set; }

        [Option('u', "username", Required = true, HelpText = "Couchbase username")]
        public string UserName { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password for the specified Couchbase username")]
        public string Password { get; set; }

        [Option('v', "viewname", Required = true, HelpText = "The Couchbase map/reduce view used to get document ids")]
        public string ViewName { get; set; }

        [Option('c', "viewmapcontent", Required = true, HelpText = "The code that will become the map part of the mapreduce view")]
        public string ViewMapContent { get; set; }

        [Option('r', "viewreducecontent", Required = false, HelpText = "The code that will become the reduce part of the mapreduce view")]
        public string ViewReduceContent { get; set; } = string.Empty;

        [Option('a', "apiport", Required = false, HelpText = "The port the Couchbase REST API responds to (default = 8092)")]
        public int Port { get; set; } = 8092;

        public ClientConfiguration BuildClientConfiguration()
        {
            return new ClientConfiguration
            {
                ApiPort = Port,
                Servers = new List<Uri> { new Uri($"http://{HostName.Trim()}") }
            };
        }

        public NetworkCredential GetCredentials()
        {
            return new NetworkCredential(UserName.Trim(), Password.Trim());
        }

        public Dictionary<string, string> GetBucketDetails()
        {
            return Buckets.Where(w => !string.IsNullOrWhiteSpace(w)).Select(bucket => bucket.Split(':')).ToDictionary(bucketDetails => bucketDetails[0].Trim(), bucketDetails => bucketDetails[1]);
        }
    }
}