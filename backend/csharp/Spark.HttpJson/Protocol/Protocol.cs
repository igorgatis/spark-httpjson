using Spark.HttpJson.Microsoft.Spark.Sql.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.HttpJson.Protocol
{
    public interface IRequestWithOptions
    {
        Dictionary<string, string> Options { get; set; }
    }

    public sealed class GetTableRequest : IRequestWithOptions
    {
        [JsonProperty("options")]
        public Dictionary<string, string> Options { get; set; }
    }

    public sealed class GetTableResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("schema")]
        public DataType Schema { get; set; }
    }

    public sealed class PlanInputPartitionsRequest : IRequestWithOptions
    {
        [JsonProperty("options")]
        public Dictionary<string, string> Options { get; set; }
    }

    public sealed class PartitionDescriptor
    {
        [JsonProperty("payload")]
        public byte[] Payload { get; set; }
    }

    public sealed class PlanInputPartitionsResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("partitions")]
        public List<PartitionDescriptor> Partitions { get; set; }
    }

    public sealed class ReadPartitionRequest : IRequestWithOptions
    {
        [JsonProperty("options")]
        public Dictionary<string, string> Options { get; set; }

        [JsonProperty("payload")]
        public byte[] Payload { get; set; }
    }

    public interface IHttpJsonHandler
    {
        GetTableResponse GetTable(
            GetTableRequest request);

        PlanInputPartitionsResponse PlanInputPartitions(
            PlanInputPartitionsRequest request);

        IEnumerable<object> ReadPartition(
            ReadPartitionRequest request);
    }

    public interface IHttpJsonHandlerWithContext<TContext>
    {
        Task<GetTableResponse> GetTableAsync(
            TContext context, GetTableRequest request);

        Task<PlanInputPartitionsResponse> PlanInputPartitionsAsync(
            TContext context, PlanInputPartitionsRequest request);

        Task<IEnumerable<object>> ReadPartitionAsync(
            TContext context, ReadPartitionRequest request);
    }
}
