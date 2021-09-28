using Microsoft.Spark.Sql.Types;
using System;
using System.Collections.Generic;

namespace Spark.HttpJson.Util
{
    public class SchemaBuilder
    {
        private static readonly SchemaBuilder defaultInstance = new SchemaBuilder();

        private static readonly Dictionary<Type, DataType> typeToDataType = new Dictionary<Type, DataType>()
        {
            {typeof(bool), new BooleanType()},

            {typeof(byte), new ByteType()},
            {typeof(short), new ShortType()},
            {typeof(long), new LongType()},
            {typeof(int), new IntegerType()},

            {typeof(float), new FloatType()},
            {typeof(double), new DoubleType()},
            {typeof(decimal), new DecimalType()},

            {typeof(string), new StringType()},
            {typeof(byte[]), new BinaryType()},

            {typeof(Date), new DateType()},
            {typeof(Timestamp), new TimestampType()},
            {typeof(DateTime), new TimestampType()},
        };

        public static DataType Build<TType>()
        {
            return defaultInstance.Build(typeof(TType)); 
        }

        public DataType Build(Type type)
        {
            // Atomic.
            DataType dataType;
            if (typeToDataType.TryGetValue(type, out dataType)) return dataType;

            // Arrays.
            if (type.IsArray)
            {
                return new ArrayType(Build(type.GetElementType()));
            }

            if (type.IsGenericType)
            {
                // Lists.
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    var listType = typeof(IList<>).MakeGenericType(genericArgs);
                    if (listType.IsAssignableFrom(type))
                    {
                        return new ArrayType(Build(genericArgs[0]));
                    }
                }

                // Maps.
                if (genericArgs.Length == 2)
                {
                    var dictType = typeof(IDictionary<,>).MakeGenericType(genericArgs);
                    if (dictType.IsAssignableFrom(type))
                    {
                        return new MapType(Build(genericArgs[0]), Build(genericArgs[1]));
                    }
                }
            }

            // Structs.
            if (typeof(object).IsAssignableFrom(type))
            {
                var fields = new List<StructField>();
                foreach (var field in type.GetFields())
                {
                    fields.Add(new StructField(field.Name, Build(field.FieldType)));
                }
                foreach (var prop in type.GetProperties())
                {
                    fields.Add(new StructField(prop.Name, Build(prop.PropertyType)));
                }
                return new StructType(fields);
            }

            throw new NotSupportedException(string.Format("Type {0} is not supported.", type));
        }
    }
}