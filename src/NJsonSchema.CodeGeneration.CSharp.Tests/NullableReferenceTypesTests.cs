using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class NullableReferenceTypesTests
    {
        private class ClassWithRequiredObject
        {
            public object Property { get; set; }

            [Required]
            [Newtonsoft.Json.JsonProperty("property2", Required = Newtonsoft.Json.Required.Always)]
            public object Property2 { get; set; }
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableReferenceTypes_is_not_set_then_CSharp_property_is_not_nullable()
        {
            //// Arrange
            var schema = JsonSchema.FromType<ClassWithRequiredObject>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object Property { get; set; }", code);
            Assert.Contains("public object Property2 { get; set; }", code);
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property_is_nullable()
        {
            //// Arrange
            var schema = JsonSchema.FromType<ClassWithRequiredObject>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object? Property { get; set; } = default!;", code);
            Assert.Contains("public object Property2 { get; set; } = default!;", code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_not_set_then_CSharp_property()
        {
            //// Arrange

            // CSharpGenerator `new object()`  adds = new object() initializer to property only if it's explicitly marked
            // as having `type: object` in json schema
            var schemaJson = @" 
            {
                ""type"": ""object"",
                ""required"": [
                    ""property2""
                ],
                ""properties"": {
                    ""Property"": {
                        ""x-nullable"": true,
                        ""type"": ""object""
                    },
                    ""property2"": {
                        ""type"": ""object""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object Property { get; set; }", code);
            Assert.Contains("public object Property2 { get; set; } = new object();", code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property()
        {
            //// Arrange

            // CSharpGenerator `new object()`  adds = new object() initializer to property only if it's explicitly marked
            // as having `type: object` in json schema
            var schemaJson = @" 
            {
                ""type"": ""object"",
                ""required"": [
                    ""property2""
                ],
                ""properties"": {
                    ""Property"": {
                        ""x-nullable"": true,
                        ""type"": ""object""
                    },
                    ""property2"": {
                        ""type"": ""object""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object? Property { get; set; } = default!;", code);
            Assert.Contains("public object Property2 { get; set; } = new object();", code);
        }
    }
}