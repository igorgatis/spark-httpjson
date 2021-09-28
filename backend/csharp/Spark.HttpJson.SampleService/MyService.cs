using Microsoft.Spark.Sql.Types;
using Newtonsoft.Json;
using Spark.HttpJson.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Spark.HttpJson.SampleService
{
    class MyService : IHttpJsonHandler
    {
        class PetInfo
        {
            public string OwnerName { get; set; }
            public string PetName { get; set; }
            public float PetWeightInKg { get; set; }
        }

        private readonly List<PetInfo> _pets;

        public MyService()
        {
            _pets = new List<PetInfo>()
            {
                new PetInfo
                {
                    OwnerName = "Audrey Hepburn",
                    PetName = "Pippin",
                    PetWeightInKg = 12,
                },
                new PetInfo
                {
                    OwnerName = "Salvator Dali",
                    PetName = "Babou",
                    PetWeightInKg = 7,
                },
                new PetInfo
                {
                    OwnerName = "Michael Jackson",
                    PetName = "Bubbles",
                    PetWeightInKg = 10,
                },
            };
        }

        class Options
        {
            public string Entity;
            public int PartitionSize;
            public float MinimumWeightInKg;
        }

        private static string SafeGetOption(IRequestWithOptions request, string key)
        {
            string text = "";
            request.Options?.TryGetValue(key, out text);
            return text;
        }

        private (Options, string) ParseOptions(IRequestWithOptions request)
        {
            try
            {
                var options = new Options
                {
                    Entity = SafeGetOption(request, "myservice.Entity"),
                };

                int.TryParse(
                    SafeGetOption(request, "myservice.PartitionSize"),
                    out options.PartitionSize);
                options.PartitionSize = Math.Max(1, options.PartitionSize);

                float.TryParse(
                    SafeGetOption(request, "myservice.MinimumWeightInKg"),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out options.MinimumWeightInKg);

                return (options, null);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return (null, e.Message);
            }
        }

        public GetTableResponse GetTable(GetTableRequest request)
        {
            var (options, error) = ParseOptions(request);
            if (error != null) new GetTableResponse { Error = error };

            return new GetTableResponse
            {
                Name = "Pets",
                Schema = new StructType(
                    new StructField[]
                    {
                        new StructField("OwnerName", new StringType()),
                        new StructField("PetName", new StringType()),
                        new StructField("PetWeightInKg", new FloatType()),
                    }
                ),
            };
        }

        class PartitionInfo
        {
            public int Offset { get; set; }
            public int Count { get; set; }
        }

        public PlanInputPartitionsResponse PlanInputPartitions(PlanInputPartitionsRequest request)
        {
            var (options, error) = ParseOptions(request);
            if (error != null) new PlanInputPartitionsResponse { Error = error };

            var partitions = new List<PartitionDescriptor>();
            for (var offset = 0; offset < _pets.Count; offset += options.PartitionSize)
            {
                var info = new PartitionInfo
                {
                    Offset = offset, 
                    Count = options.PartitionSize,
                };
                partitions.Add(new PartitionDescriptor 
                {
                    Payload = Encoding.ASCII.GetBytes(
                        JsonConvert.SerializeObject(info)),
                });
            }

            return new PlanInputPartitionsResponse
            {
                Partitions = partitions,
            };
        }

        public IEnumerable<object> ReadPartition(ReadPartitionRequest request)
        {
            var (options, error) = ParseOptions(request);
            if (error == null)
            {
                var partition = JsonConvert.DeserializeObject<PartitionInfo>(
                    Encoding.ASCII.GetString(request.Payload));
                var count = 0;
                foreach (var pet in _pets.Skip(partition.Offset))
                {
                    if (pet.PetWeightInKg >= options.MinimumWeightInKg)
                    {
                        count += 1;
                        yield return pet;
                        if (count >= options.PartitionSize) break;
                    }
                }
            }
        }
    }
}