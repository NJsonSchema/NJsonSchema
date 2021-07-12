//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
    public class SampleJsonDataGenerator
    {
        /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
        /// <param name="schema">The JSON Schema.</param>
        /// <returns>The JSON token.</returns>
        public JToken Generate(JsonSchema schema)
        {
            return Generate(schema, new HashSet<JsonSchema>());
        }

        private JToken Generate(JsonSchema schema, HashSet<JsonSchema> usedSchemas)
        {
            var property = schema as JsonSchemaProperty;
            schema = schema.ActualSchema;
            if (usedSchemas.Contains(schema))
            {
                return null;
            }

            if (schema.Type.IsObject() ||
                schema.AllOf.Any(s => s.ActualProperties.Any()))
            {
                usedSchemas.Add(schema);

                var properties = schema.ActualProperties.Concat(schema.AllOf.SelectMany(s => s.ActualSchema.ActualProperties));
                var obj = new JObject();
                foreach (var p in properties)
                {
                    obj[p.Key] = Generate(p.Value, usedSchemas);
                }
                return obj;
            }
            else if (schema.Default != null)
            {
                return JToken.FromObject(schema.Default);
            }
            else if (schema.Type.IsArray())
            {
                if (schema.Item != null)
                {
                    var array = new JArray();
                    var item = Generate(schema.Item, usedSchemas);
                    if (item != null)
                    {
                        array.Add(item);
                    }
                    return array;
                }
                else if (schema.Items.Count > 0)
                {
                    var array = new JArray();
                    foreach (var item in schema.Items)
                    {
                        array.Add(Generate(item, usedSchemas));
                    }
                    return array;
                }
            }
            else
            {
                if (schema.IsEnumeration)
                {
                    return JToken.FromObject(schema.Enumeration.First());
                }
                else if (schema.Type.IsInteger())
                {
                    return HandleIntegerType(schema);
                }
                else if (schema.Type.IsNumber())
                {
                    return HandleNumberType(schema);
                }
                else if (schema.Type.IsString())
                {
                    return HandleStringType(schema, property);
                }
                else if (schema.Type.IsBoolean())
                {
                    return JToken.FromObject(false);
                }
            }

            return null;
        }
        private static JToken HandleNumberType(JsonSchema schema)
        {
            if (schema.ExclusiveMinimumRaw != null)
            {
                return JToken.FromObject((decimal)(float.Parse(schema.Minimum.ToString()) + 0.1));
            }
            else if (schema.ExclusiveMinimum != null)
            {
                return JToken.FromObject(decimal.Parse(schema.ExclusiveMinimum.ToString()));
            }
            else if (schema.Minimum.HasValue)
            {
                return decimal.Parse(schema.Minimum.ToString());
            }
            return JToken.FromObject(0.0);
        }

        private static JToken HandleIntegerType(JsonSchema schema)
        {
            if (schema.ExclusiveMinimumRaw != null)
            {
                return JToken.FromObject(Convert.ToInt32(schema.ExclusiveMinimumRaw));
            }
            else if (schema.ExclusiveMinimum != null)
            {
                return JToken.FromObject(Convert.ToInt32(schema.ExclusiveMinimum));
            }
            else if (schema.Minimum.HasValue)
            {
                return Convert.ToInt32(schema.Minimum);
            }
            return JToken.FromObject(0);
        }

        private JToken HandleStringType(JsonSchema schema, JsonSchemaProperty property)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return JToken.FromObject(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"));
            }
            else if (schema.Format == JsonFormatStrings.DateTime)
            {
                return JToken.FromObject(DateTimeOffset.UtcNow.ToString("o"));
            }
            else if (property != null)
            {
                return JToken.FromObject(property.Name);
            }
            else
            {
                return JToken.FromObject("");
            }
        }
    }
}
