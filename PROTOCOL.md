# Spark HTTP JSON Protocol

The protocol requires the following operations:
* **GetTable**: called once to fetch the table name and schema.
* **PlanInputPartitions**: called once to list partitions.
* **ReadPartition**: called as many times as there are partitions, each time it reads all rows from that partition.

<!-- https://sequencediagram.org/

title Protocol

participantgroup #lightgray Spark
participant Spark+HttpJson
end

participantgroup #lightgray Backend
participant HttpJson
participant Datasource
end

activate Spark+HttpJson

Spark+HttpJson->HttpJson: GetTable({options})
activate HttpJson
HttpJson->Datasource:
activate Datasource
Datasource->HttpJson: Schema
deactivate Datasource
HttpJson->Spark+HttpJson: {name, schema}
deactivate HttpJson

Spark+HttpJson->HttpJson: PlanInputPartitions({options})
activate HttpJson
HttpJson->Datasource:
activate Datasource
Datasource->HttpJson: Partitions
deactivate Datasource
HttpJson->Spark+HttpJson: {partitions}
deactivate HttpJson


deactivate Spark+HttpJson

loop partitions
activate Spark+HttpJson
Spark+HttpJson->HttpJson: ReadPartition({options, partition})
activate HttpJson
HttpJson->Datasource:
activate Datasource
Datasource->HttpJson: Records
deactivate Datasource
HttpJson->Spark+HttpJson: JSON lines
deactivate HttpJson
end
-->

## Payload format

The request for all operations above expect a JSON as input with `Content-Type: application/json`. The response for `GetTable` and `PlanInputPartitions` are also one JSON object for each.

For performance reasons, the response for `ReadPartition` uses JSON lines and `Content-Type: application/json-seq`. Each read record must be encoded as a single line. See See https://jsonlines.org/.

## Operations

### GetTable

The request includes the non `http.` prefixed options under the `options` field. In case of success, the response will include two fields: the `name` of the data source being read and its `schema`. In case of failure, it includes a `error` field with a message.

NOTE: Constructing schema by hand is very error prone, you should use probably use a library to help you on that. Check [backends folder](https://github.com/igorgatis/spark-httpjson/blob/main/backend).

**Request**
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
**Response**
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

### PlanInputPartitions

The request includes the non `http.` prefixed options under the `options` field. In case of success, the response will include a `partitions` field with a list of *partitions descriptors*. In case of failure, it includes a `error` field with a message.

The content of a *partition descriptor* depends on the service implementation. Therefore, from this library point of view, it is opaque data and it is handled as an array of bytes (payload). This array of bytes is sent in the `ReadPartition` call. Usually, it contains enough information to query a segment (a partition) of the data source being read. For example, it could be a JSON, a Key-Value-Pair list or some binary format containing the database name, table name, some query, an offset to start from and a maximum number of elements.

**Request**
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
**Response**
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

### ReadPartition

The request includes the non `http.` prefixed options under the `options` field plus a `payload` field with an array of bytes representing a *partition descriptor* returned from `PlanInputPartitions`. In case of success, it returns JSON lines, one line per record read. In case of failure, it signals with an HTTP error.

**Request**
```
POST /ReadPartition HTTP/1.1
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
**Response**
```
HTTP/1.1 200 OK
Content-Type: application/json-seq

{"OwnerName":"Audrey Hepburn","PetName":"Pippin","PetWeightInKg":12}
{"OwnerName":"Salvator Dali","PetName":"Babou","PetWeightInKg":7}
{"OwnerName":"Michael Jackson","PetName":"Bubbles","PetWeightInKg":10}
...
```

In the example above, `e1RhYmxlIjoiUGV0cyIsIk1pbldlaWdodEluS2ciOjUsIk9mZnNldCI6MjAwMH0=` decodes to `{Table":"Pets","MinWeightInKg":5,"Offset":2000}` and the JSON objects separated by lines represents the remaning elegible pets from the dataset from offset `2000`.
