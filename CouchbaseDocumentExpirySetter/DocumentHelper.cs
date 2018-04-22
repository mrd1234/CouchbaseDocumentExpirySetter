namespace CouchbaseDocumentExpirySetter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;

    public static class DocumentHelper
    {
        public static async Task<DocumentList> GetDocumentsWithNoExpiryAsync(string address, NetworkCredential credentials)
        {
            var httpRequest = WebRequest.Create(address) as HttpWebRequest;
            httpRequest.Credentials = credentials;

            using (var httpResponse = await httpRequest.GetResponseAsync() as HttpWebResponse)
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Server error (HTTP {httpResponse.StatusCode}: {httpResponse.StatusDescription}).");
                }

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    var deserialiser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    var documentList = deserialiser.Deserialize<DocumentList>(result);

                    if (documentList != null) return documentList;

                    var documents = new JavaScriptSerializer().Deserialize<List<Document>>(result);
                    return new DocumentList { Rows = documents };
                }
            }
        }
    }
}
