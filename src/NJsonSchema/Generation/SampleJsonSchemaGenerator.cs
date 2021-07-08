//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a JSON Schema from sample JSON data.</summary>
    public class SampleJsonSchemaGenerator
    {
        /// <summary>Generates the JSON Schema for the given JSON data.</summary>
        /// <param name="json">The JSON data.</param>
        /// <returns>The JSON Schema.</returns>
        public JsonSchema Generate(string json)
        {
            var token = JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            var schema = new JsonSchema();
            Generate(token, schema, schema, "Anonymous");
            return schema;
        }

        private void Generate(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            if (schema != rootSchema && token.Type == JTokenType.Object)
            {
                JsonSchema referencedSchema = null;
                if (token is JObject obj)
                {
                    var properties = obj.Properties();

                    referencedSchema = rootSchema.Definitions
                        .Select(t => t.Value)
                        .FirstOrDefault(s =>
                            s.Type == JsonObjectType.Object &&
                            properties.All(p => s.Properties.ContainsKey(p.Name)));
                }
                
                if (referencedSchema == null)
                {
                    referencedSchema = new JsonSchema();
                    AddSchemaDefinition(rootSchema, referencedSchema, typeNameHint);
                }

                schema.Reference = referencedSchema;
                GenerateWithoutReference(token, referencedSchema, rootSchema, typeNameHint);
                return;
            }

            GenerateWithoutReference(token, schema, rootSchema, typeNameHint);
        }

        private void GenerateWithoutReference(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            if (token == null)
            {
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    GenerateObject(token, schema, rootSchema);
                    break;

                case JTokenType.Array:
                    GenerateArray(token, schema, rootSchema, typeNameHint);
                    break;

                case JTokenType.Date:
                    schema.Type = JsonObjectType.String;
                    schema.Format = token.Value<DateTime>() == token.Value<DateTime>().Date
                        ? JsonFormatStrings.Date
                        : JsonFormatStrings.DateTime;
                    break;

                case JTokenType.String:
                    schema.Type = JsonObjectType.String;
                    break;

                case JTokenType.Boolean:
                    schema.Type = JsonObjectType.Boolean;
                    break;

                case JTokenType.Integer:
                    schema.Type = JsonObjectType.Integer;
                    break;

                case JTokenType.Float:
                    schema.Type = JsonObjectType.Number;
                    break;

                case JTokenType.Bytes:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Byte;
                    break;

                case JTokenType.TimeSpan:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.TimeSpan;
                    break;

                case JTokenType.Guid:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Guid;
                    break;

                case JTokenType.Uri:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Uri;
                    break;
            }

            if (schema.Type == JsonObjectType.String && Regex.IsMatch(token.Value<string>(), "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$"))
            {
                schema.Format = JsonFormatStrings.Date;
            }

            if (schema.Type == JsonObjectType.String && Regex.IsMatch(token.Value<string>(), "^[0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
            {
                schema.Format = JsonFormatStrings.TimeSpan;
            }

            if (schema.Type == JsonObjectType.String && schema.Format == null && DateTime.TryParse(token.Value<string>(), null, System.Globalization.DateTimeStyles.RoundtripKind, out _))
            {
                schema.Format = JsonFormatStrings.DateTime;
            }
        }

        private void GenerateObject(JToken token, JsonSchema schema, JsonSchema rootSchema)
        {
            schema.Type = JsonObjectType.Object;
            foreach (var property in ((JObject)token).Properties())
            {
                var propertySchema = new JsonSchemaProperty();
                var propertyName = property.Value.Type == JTokenType.Array ? ConversionUtilities.Singularize(property.Name) : property.Name;
                var typeNameHint = ConversionUtilities.ConvertToUpperCamelCase(propertyName, true);

                Generate(property.Value, propertySchema, rootSchema, typeNameHint);
                schema.Properties[property.Name] = propertySchema;
            }
        }

        private void GenerateArray(JToken token, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            schema.Type = JsonObjectType.Array;

            var itemSchemas = ((JArray)token).Select(item =>
            {
                var itemSchema = new JsonSchema();
                GenerateWithoutReference(item, itemSchema, rootSchema, typeNameHint);
                return itemSchema;
            }).ToList();

            if (itemSchemas.Count == 0)
            {
                schema.Item = new JsonSchema();
            }
            else if (itemSchemas.GroupBy(s => s.Type).Count() == 1)
            {
                MergeAndAssignItemSchemas(rootSchema, schema, itemSchemas, typeNameHint);
            }
            else
            {
                schema.Item = itemSchemas.First();
            }
        }

        private void MergeAndAssignItemSchemas(JsonSchema rootSchema, JsonSchema schema, List<JsonSchema> itemSchemas, string typeNameHint)
        {
            var firstItemSchema = itemSchemas.First();
            var itemSchema = new JsonSchema
            {
                Type = firstItemSchema.Type
            };

            if (firstItemSchema.Type == JsonObjectType.Object)
            {
                foreach (var property in itemSchemas.SelectMany(s => s.Properties).GroupBy(p => p.Key))
                {
                    itemSchema.Properties[property.Key] = property.First().Value;
                }
            }

            AddSchemaDefinition(rootSchema, itemSchema, typeNameHint);
            schema.Item = new JsonSchema { Reference = itemSchema };
        }

        private void AddSchemaDefinition(JsonSchema rootSchema, JsonSchema schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(typeNameHint) || rootSchema.Definitions.ContainsKey(typeNameHint))
            {
                rootSchema.Definitions["Anonymous" + (rootSchema.Definitions.Count + 1)] = schema;
            }
            else
            {
                rootSchema.Definitions[typeNameHint] = schema;
            }
        }
    }
}
