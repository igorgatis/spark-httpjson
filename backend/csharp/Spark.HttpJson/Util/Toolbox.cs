using Newtonsoft.Json;
using Spark.HttpJson.Protocol;
using System;
using System.Collections.Generic;

namespace Spark.HttpJson.Util
{
    public static class Toolbox
    {
        public static TOptions ParseOptions<TOptions>(
            this IRequestWithOptions request,
            string prefixToRemove = "")
        {
            try
            {
                request.Options = request.Options ?? new Dictionary<string, string>();
                var values = request.Options;
                if (!string.IsNullOrEmpty(prefixToRemove))
                {
                    values = new Dictionary<string, string>();
                    foreach (var item in request.Options)
                    {
                        if (item.Key.StartsWith(prefixToRemove))
                        {
                            var key = item.Key.Substring(prefixToRemove.Length);
                            values[key] = item.Value;
                        }
                    }
                }
                var json = JsonConvert.SerializeObject(values);
                return JsonConvert.DeserializeObject<TOptions>(json);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return default(TOptions);
            }
        }
    }
}
