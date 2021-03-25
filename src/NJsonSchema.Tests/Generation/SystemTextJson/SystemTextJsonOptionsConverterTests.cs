#if !NET461

using Newtonsoft.Json.Converters;
using NJsonSchema.Generation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonOptionsConverterTests
    {
        [Fact]
        public async Task SystemTextJson_WhenEnumsAreSerializedAsStrings_ThenGlobalConverterExists()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };

            // Act
            var settings = SystemTextJsonUtilities.ConvertJsonOptionsToNewtonsoftSettings(options);

            // Assert
            Assert.Contains(settings.Converters, c => c is StringEnumConverter);
        }

        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            [JsonIgnore]
            public string Ignored { get; set; }
        }

        [Fact]
        public async Task SystemTextJson_WhenLowerCamelCasePropertiesAreUsed_ThenCamelCasePropertyNamesContractResolverIsUsed()
        {
            // Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };
            var generator = new JsonSchemaGenerator(settings);

            // Act
            var schema = generator.Generate(typeof(Person));

            // Assert
            Assert.True(schema.Properties.ContainsKey("firstName"));
            Assert.True(schema.Properties.ContainsKey("lastName"));
            Assert.False(schema.Properties.ContainsKey("ignored"));
        }

        [Fact]
        public async Task SystemTextJson_WhenNamingPolicyIsNull_ThenDefaultContractResolverIsUsed()
        {
            // Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                }
            };
            var generator = new JsonSchemaGenerator(settings);

            // Act
            var schema = generator.Generate(typeof(Person));

            // Assert
            Assert.True(schema.Properties.ContainsKey("FirstName"));
            Assert.True(schema.Properties.ContainsKey("LastName"));
        }

        public class AnnotatedPerson
        {
            [JsonPropertyName("first-name")]
            public string FirstName { get; set; }

            [JsonPropertyName("NameLast")]
            public string LastName { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public MyEnum StringEnum { get; set; }

            public MyEnum IntEnum { get; set; }
        }

        public enum MyEnum
        {
            Foo, 
            Bar
        }

        [Fact]
        public async Task SystemTextJson_WhenGeneratingWithCustomPropertyNames_ThenAttributesArePickedUp()
        {
            // Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };
            var generator = new JsonSchemaGenerator(settings);

            // Act
            var schema = generator.Generate(typeof(AnnotatedPerson));

            // Assert
            Assert.True(schema.Properties.ContainsKey("first-name"));
            Assert.True(schema.Properties.ContainsKey("NameLast"));

            Assert.True(schema.Properties["stringEnum"].ActualSchema.Type.HasFlag(JsonObjectType.String));
            Assert.True(schema.Properties["intEnum"].ActualSchema.Type.HasFlag(JsonObjectType.Integer));
        }
    }
}

#endif