# HTTP JSON Data Source for Apache Spark

HTTP JSON is protocol specification which makes it easy to source data from your system into Apache Spark using HTTP requests with JSON payloads.

It is useful for scenarios where you need to fetch data from a system for which there is no native Spark connector or reading data directly from the data source is not possible, complex or undesired.

## How it works

Apache Spark uses `spark-httpjson` client library to connect to your backend. It issues a couple of discovery requests asking for data schema and partition list. Then Spark distributes reading tasks among its workers which read partitions in parallel.

Your server needs to implement both discovery and partition reading operations. This repository includes reference implementations for the HTTP handling so you can focus on the actual logic.

Check [protocol/README.md](protocol/README.md) for details on how messages are exchanged.

## Usage

First, make sure you include the library as part of your job, check [Releases](https://github.com/igorgatis/spark-httpjson/releases).

The following code creates a DataFrame using `spark.sources.datasourcev2.httpjson` data source format. It tells to read data from `https://hostname.domain.com:443/spark-api` endpoint using Basic authentication for user `someusername` and password `S3cr3t`. It also includes options which are meant to instruct your backend.

```scala
val df = spark.read
  .format("spark.sources.datasourcev2.httpjson")
  .option("http.url", "https://hostname.domain.com:443/spark-api")
  .option("http.user", "someusername")
  .option("http.password", "S3cr3t")
  .option("http.gzip", "true")
  .option("http.ignorecertificates", "true")
  .option("http.X-My-Header", "blah")
  .option("myservice.Entity", "Pets")
  .option("myservice.PartitionSize", "1000")
  .option("myservice.MinimumWeightInKg", "5")
  .load()
```

The options with `http.` prefix control the behaviour of HTTP requests. Some of them are handled specially:

| Option | Description |
| ------ | ----------- |
| `http.url` | Sets endpoint URL (required). |
| `http.user` | Sets Basic Auth user. |
| `http.password` | Sets Basic Auth password. |
| `http.gzip` | Tells the library to use compressed data (default: `false`). |
| `http.ignorecertificates` | Tells the library should ignore certificate errors (default: `false`). |

All others `http.` prefixed options are passed as regular HTTP headers (eg. `X-My-Header: blah`). This means you can use `http.Authentication` to use a Bearer token or `http.Cookie` to send a cookie as part of the request. **WARNING**: although gzip compression reduces request/response sizes, it does imply on CPU costs. Make sure you try with and without compression to make sure it yields expected performance gains.

All non `http.` prefixed options are passed as JSON payload to requests made to your backend.

## Service Implementations

## C#

* Library: [backend/csharp/Spark.HttpJson.AspNetCore](backend/csharp/Spark.HttpJson.AspNetCore)
* Sample: [backend/csharp/Spark.HttpJson.SampleService](backend/csharp/Spark.HttpJson.SampleService)

## Typescript

> TODO
