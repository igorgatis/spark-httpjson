using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Spark.HttpJson.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.HttpJson.AspNetCore
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSparkHttpJson(
            this IApplicationBuilder builder,
            string path,
            IHttpJsonHandlerWithContext<HttpContext> handler)
        {
            if (string.IsNullOrEmpty(path))
            {
                builder.MapOperations(handler);
            }
            else
            {
                builder.Map(path, b => b.MapOperations(handler));
            }
            return builder;
        }

        private static IApplicationBuilder MapOperations(
            this IApplicationBuilder builder,
            IHttpJsonHandlerWithContext<HttpContext> handler)
        {
            var processor = new HttpJsonProcessor(handler);

            builder.Map("/GetTable",
                app => app.Run(processor.GetTableAsync));

            builder.Map("/PlanInputPartitions",
                app => app.Run(processor.PlanInputPartitionsAsync));

            builder.Map("/ReadPartition",
                app => app.Run(processor.ReadPartitionAsync));

            return builder;
        }

        public static IApplicationBuilder UseSparkHttpJson(
            this IApplicationBuilder builder,
            IHttpJsonHandlerWithContext<HttpContext> handler)
        {
            return builder.UseSparkHttpJson(null, handler);
        }

        public static IApplicationBuilder UseSparkHttpJson(
            this IApplicationBuilder builder,
            string path,
            IHttpJsonHandler handler)
        {
            return builder.UseSparkHttpJson(path, new HttpJsonHandlerNoContext(handler));
        }

        public static IApplicationBuilder UseSparkHttpJson(
            this IApplicationBuilder builder,
            IHttpJsonHandler handler)
        {
            return builder.UseSparkHttpJson(null, handler);
        }

        class HttpJsonHandlerNoContext : IHttpJsonHandlerWithContext<HttpContext>
        {
            private IHttpJsonHandler _handler;

            public HttpJsonHandlerNoContext(IHttpJsonHandler handler)
            {
                _handler = handler;
            }

            public async Task<GetTableResponse> GetTableAsync(
                HttpContext context, GetTableRequest request)
            {
                return await Task.Run(() => _handler.GetTable(request));
            }

            public async Task<PlanInputPartitionsResponse> PlanInputPartitionsAsync(
                HttpContext context, PlanInputPartitionsRequest request)
            {
                return await Task.Run(() => _handler.PlanInputPartitions(request));
            }

            public async Task<IEnumerable<object>> ReadPartitionAsync(
                HttpContext context, ReadPartitionRequest request)
            {
                return await Task.Run(() => _handler.ReadPartition(request));
            }
        }
    }
}
