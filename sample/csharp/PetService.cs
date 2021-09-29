using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Spark.HttpJson.Microsoft.Spark.Sql.Types;
using Spark.HttpJson.Protocol;
using Spark.HttpJson.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sample
{
    class PetServiceOptions
    {
        public string Entity { get; set; }
        public int PartitionSize { get; set; }
        public float MinimumWeightInKg { get; set; }
    }

    class PetServicePartitionInfo
    {
        public int Offset { get; set; }
        public int Count { get; set; }
    }

    class PetInfo
    {
        public string OwnerName { get; set; }
        public string PetName { get; set; }
        public float PetWeightInKg { get; set; }
    }

    class PetService : SimpleHttpJsonService<
        HttpContext,
        PetServiceOptions,
        PetServicePartitionInfo,
        PetInfo>
    {
        private readonly List<PetInfo> _pets;

        public PetService() : base("Pets")
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

        public override PetServiceOptions ParseOptions(
            HttpContext context, IRequestWithOptions request)
        {
            var options = request.ParseOptions<PetServiceOptions>("myservice.");
            options.PartitionSize = Math.Max(1, options.PartitionSize);
            return options;
        }

        public override DataType GetSchema(HttpContext context, PetServiceOptions options)
        {
            Console.WriteLine("GetSchema: {0}", JsonConvert.SerializeObject(options));
            return base.GetSchema(context, options);
        }

        public override IEnumerable<PetServicePartitionInfo> ListPartitions(
            HttpContext context, PetServiceOptions options)
        {
            Console.WriteLine("ListPartitions: {0}", JsonConvert.SerializeObject(options));
            for (var offset = 0; offset < _pets.Count; offset += options.PartitionSize)
            {
                yield return new PetServicePartitionInfo
                {
                    Offset = offset, 
                    Count = Math.Min(options.PartitionSize, _pets.Count - offset),
                };
            }
        }

        public override IEnumerable<PetInfo> ReadPartition(
            HttpContext context, PetServiceOptions options, PetServicePartitionInfo partition)
        {
            Console.WriteLine("ReadPartition: {0} {1}",
                JsonConvert.SerializeObject(options),
                JsonConvert.SerializeObject(partition));
            int count = 0;
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