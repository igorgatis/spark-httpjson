using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spark.HttpJson.Microsoft.Spark.Sql.Types;
using Spark.HttpJson.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Spark.HttpJson.AspNetCore
{
    class HttpJsonProcessor
    {
        private const string kJsonMimeType = "application/json";
        private const string kJsonSeqMimeType = "application/json-seq";

        private readonly IHttpJsonHandlerWithContext<HttpContext> _handler;
        private readonly JsonSerializer _serializer;

        public HttpJsonProcessor(IHttpJsonHandlerWithContext<HttpContext> handler)
        {
            _handler = handler;
            _serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
            _serializer.Converters.Add(new DataTypeConverter());
        }

        public async Task GetTableAsync(HttpContext context)
        {
            var request = ParseRequest<GetTableRequest>(context);
            if (request == null) return;

            var response = await _handler.GetTableAsync(context, request);
            await ReplyJsonAsync(context, response);
        }

        public async Task PlanInputPartitionsAsync(HttpContext context)
        {
            var request = ParseRequest<PlanInputPartitionsRequest>(context);
            if (request == null) return;

            var response = await _handler.PlanInputPartitionsAsync(context, request);
            await ReplyJsonAsync(context, response);
        }

        public async Task ReadPartitionAsync(HttpContext context)
        {
            var request = ParseRequest<ReadPartitionRequest>(context);
            if (request == null) return;

            var list = await _handler.ReadPartitionAsync(context, request);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = kJsonSeqMimeType;

            using (var writer = new StreamWriter(context.Response.Body))
            {
                foreach (var item in list)
                {
                    using (var jsonWriter = new JsonTextWriter(writer) { CloseOutput = false})
                    {
                        _serializer.Serialize(jsonWriter, item);
                    }
                    writer.Write('\n');
                }
            }
        }

        private TRequest ParseRequest<TRequest>(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return default(TRequest);
            }

            if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mt)
                || !mt.MediaType.Equals(kJsonMimeType, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return default(TRequest);
            }

            using (var reader = new StreamReader(context.Request.Body))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return _serializer.Deserialize<TRequest>(jsonReader);
            }
        }

        private async Task ReplyJsonAsync<TResponse>(
            HttpContext context, TResponse response)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = kJsonMimeType;

            using (var writer = new StreamWriter(context.Response.Body))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                _serializer.Serialize(jsonWriter, response);
            }
        }

        class DataTypeConverter : JsonConverter<DataType>
        {
            public override void WriteJson(
                JsonWriter writer, DataType value, JsonSerializer serializer)
            {
                ((JObject)value.JsonValue).WriteTo(writer);
            }

            public override DataType ReadJson(
                JsonReader reader, Type objectType, DataType existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
