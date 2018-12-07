using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class ObjectValidationTests
    {
        [Fact]
        public void When_token_is_not_object_then_validation_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty();

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.ObjectExpected, errors.First().Kind);
        }

        [Fact]
        public void When_required_property_is_missing_then_it_should_be_in_error_list()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Single(errors);
            Assert.Equal("Foo", errors.First().Property);
            Assert.Equal("#/Foo", errors.First().Path);
            Assert.Equal(ValidationErrorKind.PropertyRequired, errors.First().Kind);
        }

        [Fact]
        public void When_property_matches_one_of_the_types_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                Type = JsonObjectType.Number | JsonObjectType.Null
            };

            var token = new JObject();
            token["Foo"] = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_optional_property_is_missing_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = false,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_string_property_is_available_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue("Bar");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_string_property_required_but_integer_provided_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.StringExpected, errors.First().Kind);
            Assert.Equal("Foo", errors.First().Property);
            Assert.Equal("#/Foo", errors.First().Path);
        }
        
        [Fact]
        public async Task When_type_property_has_integer_type_then_it_is_validated_correctly()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(
                @"{
              ""$schema"": ""http://json-schema.org/draft-06/schema#"",
              ""type"": ""object"",
              ""additionalProperties"": false,
              ""properties"": {
                ""type"" : {""type"" : ""integer""}
              }
            }");

            //// Act
            var errors = schema.Validate(
                @"{
              ""type"": 1
            }");

            //// Assert
            Assert.Equal(0, errors.Count);
        }
    }
}