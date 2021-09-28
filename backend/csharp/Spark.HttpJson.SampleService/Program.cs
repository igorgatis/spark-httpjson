using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Spark.Sql.Types;
using Spark.HttpJson.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace Spark.HttpJson.SampleService
{
    class Program
    {
        static public void Configure(IApplicationBuilder app)
        {
            app.UseSparkHttpJson("/spark-api", new MyService());
        }

        static void Main()
        {
            new WebHostBuilder()
                .UseKestrel()
                .Configure(Configure)
                .UseUrls("http://+:8080")
                .Build()
                .Run();
        }
    }
}
