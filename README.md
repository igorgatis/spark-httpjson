# HTTP JSON Data Source for Apache Spark

This library issues HTTP read requests to a service which implements a simple JSON protocol.

## Usage

The following code creates a DataFrame using this library. Notice that options starting with `http.` prefix are used by the library to configure settings and customize HTTP headers. For example, it will send requests to `https://hostname.domain.tld:443/` using Basic authentication for user `someusername` and password `S3cr3t`. It also ignores certificate errors and uses gzip compression. By convetion, one should adopt a prefix (e.g. `myservice.`) to specify service specific options.

```scala
val df = spark.read
  .format("spark.sources.datasourcev2.httpjson")
  .option("http.url", "https://hostname.domain.tld:443/")
  .option("http.user", "someusername")
  .option("http.password", "S3cr3t")
  .option("http.ignorecertificates", "true")
  .option("http.gzip", "true")
  .option("http.X-My-Header", "blah")
  .option("myservice.Entity", "Pets")
  .option("myservice.PartitionSize", "1000")
  .option("myservice.MinimumWeightInKg", "5")
  .load()
```

### Encryption and Authentication
Although optional, it is recommended to use both HTTPS and some authentication. One may use `.option("http.ignorecertificates", "true")` to ignore certificate errors (eg. for self-signed certificate). As shown in the example above, one may use `.option("http.user", "someusername")` and `.option("http.password", "S3cr3t")` to provide Basic authentication. Alternatively one can use HTTP headers directly like `.option("http.Authentication", "Bearer dEadB33f...")`. Finally, one may use a custom authentication method such as `.option("myservice.Authentication", "some-long-base64-token")`, in which case the backend service should take care of authentication.

### Gzip
One may enable gzip compression by passing `.option("http.gzip", "true")`, this will signal the library to annotate requests with HTTP header `Accept-Encoding: gzip`. Service implementation should inspect this header and maybe reply with compressed content and proper HTTP header response `Content-Encoding: gzip`. This library will uncompress the response body as expectedt. **WARNING**: although gzip compression reduces request/response sizes, it does imply on CPU costs. Make sure you try with and without compression to make sure it yields expected performance gains.

## Service Implementations

TODO: add examples for Typescript, C#, Go(?)

## Protocol

The backend service is an HTTP (or HTTPS) server which implements the following operations:
* `GetTable`: called once and should return the table schema.
* `PlanInputPartitions`: called once and should return a list of partitions.
* `ReadPartition`: called as many times as there are partitions and each time it should return the items for that given partition.

### Request

Each request will look like the following:
```
POST /GetTable HTTP/1.1
Authorization: Basic c29tZXVzZXJuYW1lOlMzY3IzdA==
Accept-Encoding: gzip
X-My-Header: blah
Content-Type: application/json

{
  "options": [
    "myservice.Entity": "Pets",
    "myservice.PartitionSize": "1000",
    "myservice.MinimumWeightInKg": "5"
  ]
}
```
In the example above, backend should read from `Pets` entity repository. It should also return pets weighing at least 5kg. While answering this request, the backend should return `1000` or less items per partition.

Notice that options with prefix `http.` converted into HTTP related fields (eg. URL, `Authorization`, `X-My-Header`) but are not present in body. The options without `http.` prefix  (e.g. `myservice.`) are present in the body. The backend service should parse this JSON and use passed options as needed.

**IMPORTANT**: `options` always maps a **string** key into a **string** value.

### Response

The endpoint for `/GetTable` and `/PlanInputPartitions` should reply with `Content-Type: application/json` and a single JSON object as payload. In case of failure, the response should be a JSON object with an error message: `{"error": "Some error description."}`. The endpoint for `/ReadPartition` is special, it should return `Content-Type: application/json-seq` and the response body should be one JSON object per line, each representing one item read. See https://jsonlines.org/.

#### /GetTable
In case of success, it will container two fields, `name` of the "table" being read and `schema` which is an object - which is a bit tricky to construct. In fact, one should probably use a library to build:
* **C#**: Microsoft.Spark's DataType [API](https://github.com/dotnet/spark/tree/main/src/csharp/Microsoft.Spark/Sql/Types) | [Nuget](https://www.nuget.org/packages/Microsoft.Spark/)

Sample response:
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "name": "Pets",
  "schema": {
    "type":"struct",
    "fields":[{
      "name": "OwnerName",
      "type": "string",
      "nullable": true,
      "metadata": {}
    },{
      "name": "PetName",
      "type": "string",
      "nullable": true,
      "metadata": {}
    },{
      "name": "PetWeightInKg",
      "type": "long",
      "nullable": false,
      "metadata": {}
    }]
  }
}
```

#### /PlanInputPartitions
It will receive the same request as `GetTable` except by the URL path which should be `PlanInputPartitions`. In case of success, it returns a list of *partitions descriptors*. The descriptor for a partition depends on the service implementation. Therefore, from this library point of view, it is opaque data and it is handled as an array of bytes (payload). This array of bytes is sent in the `ReadPartition` call. Usually, it contains enough information to query a segment (a partition) of the data store being read. For example, it could be a JSON, a Key-Value-Pair list or some binary format containing the database name, table name, some query, an offset to start from and a maximum number of elements.

Sample response:
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "partitions": [{
    "payload": "e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIkNvdW50IjoxMDAwfQ==",
  },{
    "payload": "e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIk9mZnNldCI6MTAwMCwiQ291bnQiOjIwMDB9",
  },{
    "payload": "e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIk9mZnNldCI6MjAwMH0=",
  }]
}
```
The base64 strings from the example above decode to:
* `{Table":"Pets","MinWeightInKg":5,"Count":1000}`
* `{Table":"Pets","MinWeightInKg":5,"Offset":1000,"Count":2000}`
* `{Table":"Pets","MinWeightInKg":5,"Offset":2000}`

#### /ReadPartition
Besides the `options` field, its request includes an extra field named `payload` which is an array of bytes representing one of the *partition descriptors* returned by `PlanInputPartitions`. Sample request:
```
POST /GetTable HTTP/1.1
Authorization: Basic c29tZXVzZXJuYW1lOlMzY3IzdA==
Accept-Encoding: gzip
X-My-Header: blah
Content-Type: application/json

{
  "options": [
    "myservice.Entity": "Pets",
    "myservice.PartitionSize": "1000",
    "myservice.MinimumWeightInKg": "5"
  ],
  "payload": "e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIk9mZnNldCI6MjAwMH0="
}
```

In case of success, it returns JSON lines, one line per entity read. Sample response:
```
HTTP/1.1 200 OK
Content-Type: application/json-seq

{"OwnerName":"Audrey Hepburn","PetName":"Pippin","PetWeightInKg":12}
{"OwnerName":"Salvator Dali","PetName":"Babou","PetWeightInKg":7}
{"OwnerName":"Michael Jackson","PetName":"Bubbles","PetWeightInKg":10}
...
```

In the example above, `e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIk9mZnNldCI6MjAwMH0=` decodes to `{Table":"Pets","MinWeightInKg":5,"Offset":2000}` and the JSON objects separated by lines represents the remaning elegible pets from the dataset from offset `2000`.
