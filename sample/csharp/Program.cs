using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Spark.HttpJson.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace sample
{
    class Program
    {
        static public void Configure(IApplicationBuilder app)
        {
            app.UseSparkHttpJson("/spark-api", new PetService());
        }

        static void Main()
        {
            new WebHostBuilder()
                .UseKestrel()
                .Configure(Configure)
                .UseUrls("http://0.0.0.0:9999")
                .Build()
                .Run();
        }
    }
}
