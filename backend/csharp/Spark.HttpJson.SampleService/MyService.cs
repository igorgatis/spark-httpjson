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
            public string Entity { get; set; }
            public int PartitionSize { get; set; }
            public float MinimumWeightInKg { get; set; }
        }

        private Options ParseOptions(IRequestWithOptions request)
        {
            var options = request.ParseOptions<Options>("myservice.");
            options.PartitionSize = Math.Max(1, options.PartitionSize);
            return options;
        }

        public GetTableResponse GetTable(GetTableRequest request)
        {
            var options = ParseOptions(request);
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
            var options = ParseOptions(request);

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
            var options = ParseOptions(request);
            if (options != null)
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
                        if (count >= partition.Count) break;
                    }
                }
            }
        }
    }
}