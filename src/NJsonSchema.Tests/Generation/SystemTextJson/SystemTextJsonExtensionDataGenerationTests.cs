#if !NET461

using System.Collections.Generic;
using System.Text.Json.Serialization;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonExtensionDataGenerationTests
    {
        public class ClassWithExtensionData
        {
            public string Foo { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        public void SystemTextJson_When_class_has_property_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set()
        {
            //// Act
            var schema = JsonSchema.FromType<ClassWithExtensionData>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3,
                SerializerOptions = new System.Text.Json.JsonSerializerOptions()
            });

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }
    }
}

#endif