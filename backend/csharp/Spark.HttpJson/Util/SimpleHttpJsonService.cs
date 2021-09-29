using Newtonsoft.Json;
using Spark.HttpJson.Microsoft.Spark.Sql.Types;
using Spark.HttpJson.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Spark.HttpJson.Util
{
    public abstract class SimpleHttpJsonService<TContext, TOptions, TPartitionInfo, TEntity> 
        : IHttpJsonHandlerWithContext<TContext>
    {
        private string _tableName;

        public SimpleHttpJsonService(string tableName)
        {
            _tableName = tableName;
        }

        public abstract TOptions ParseOptions(TContext context, IRequestWithOptions request);

        public virtual DataType GetSchema(TContext context, TOptions options)
        {
            return SchemaBuilder.Build<TEntity>();
        }

        public abstract IEnumerable<TPartitionInfo> ListPartitions(TContext context, TOptions options);

        public abstract IEnumerable<TEntity> ReadPartition(
            TContext context, TOptions options, TPartitionInfo partition);

        public Task<GetTableResponse> GetTableAsync(TContext context, GetTableRequest request)
        {
            try
            {
                var options = ParseOptions(context, request);
                return Task.FromResult(
                    new GetTableResponse
                    {
                        Name = _tableName,
                        Schema = GetSchema(context, options),
                    });
            }
            catch (Exception e)
            {
                return Task.FromResult(new GetTableResponse { Error = e.Message });
            }
        }

        public Task<PlanInputPartitionsResponse> PlanInputPartitionsAsync(
            TContext context, PlanInputPartitionsRequest request)
        {
            try
            {
                var options = ParseOptions(context, request);
                var partitions = ListPartitions(context, options);
                var response = new PlanInputPartitionsResponse
                {
                    Partitions = new List<PartitionDescriptor>(),
                };
                foreach (var partition in partitions)
                {
                    response.Partitions.Add(new PartitionDescriptor 
                    {
                        Payload = Encoding.ASCII.GetBytes(
                            JsonConvert.SerializeObject(partition)),
                    });
                }
                return Task.FromResult(response);
            }
            catch (Exception e)
            {
                return Task.FromResult(new PlanInputPartitionsResponse { Error = e.Message });
            }
        }

        public Task<IEnumerable<object>> ReadPartitionAsync(
            TContext context, ReadPartitionRequest request)
        {
            var options = ParseOptions(context, request);
            var partition = JsonConvert.DeserializeObject<TPartitionInfo>(
                Encoding.ASCII.GetString(request.Payload));
            var entities = ReadPartition(context, options, partition);
            return Task.FromResult((IEnumerable<object>)entities);
        }
    }
}