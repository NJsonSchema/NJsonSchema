using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class JsonSchemaGeneratorTests
    {
        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel : List<string>
        {
        }

        [Fact]
        public async Task When_type_is_an_array_then_generator_adds_mapping_to_schema_resolver()
        {
            //// Arrange
            var root = new JsonSchema4();
            var settings = new JsonSchemaGeneratorSettings();
            var generator = new JsonSchemaGenerator(settings);
            var schemaResolver = new JsonSchemaResolver(root, settings);

            //// Act
            await generator.GenerateAsync(typeof(ArrayModel), schemaResolver);

            //// Assert
            Assert.True(schemaResolver.HasSchema(typeof(ArrayModel), false));
        }
    }
}
