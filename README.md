# CouchbaseDocumentExpirySetter
Sets document expiry for all documents in specified Couchbase buckets.

This tool was created to resolve some disk space issues due to documents added to Couchbase by the https://github.com/OrleansContrib/OrleansCouchbaseProvider having no expiry value set. 

Currently the same expiry value is set for all specified buckets but could be easily extended to set this per bucket or document type.

# Usage

This is a command line tool that takes the following parameters:

-h, --host         Required. The hostname of the Couchbase server

-b, --buckets      Required. The Couchbase bucket(s) to process, eg: bucket1:password1,bucket2:password2

-u, --username     Required. Couchbase username

-p, --password     Required. Password for the specified Couchbase username

-e, --expiry       Required. The number of minutes to set as expiry time for each document

-s, --batchsize    The number of documents to request from Couchbase REST service for processing

-v, --viewname     The Couchbase map/reduce view used to query documents with no expiry value

-a, --apiport      The port the Couchbase REST API responds to

--help             Display this help screen.

--version          Display version information.

# Couchbase requirements

Map/reduce views are used to get the list of documents to update.

To get a list of documents with no expiry value set, you can create a Couchbase map/reduce view with the following code in the map section:

```javascript
function (doc, meta) {
  if(meta.expiration === 0) emit(null, null);
}
```

To get a list of all document ids, put this code in the map section:

```javascript
function (doc, meta) {
  emit(null, null);
}
```
