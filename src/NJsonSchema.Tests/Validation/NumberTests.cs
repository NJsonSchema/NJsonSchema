using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class NumberTests
    {
        [Fact]
        public async Task When_double_is_bigger_then_decimal_then_validation_works()
        {
            /// Arrange
            const string json = @"{
                'schema': 'http://json-schema.org/draft-04/schema',
                'title': 'NumberWithCircleVisualisationData',
                'type': 'object',
                'additionalProperties': true,
                'required': [
                    'UpperLimit',
                ],
                'properties': {
                    'UpperLimit': {
                        'type': 'number',
                        'format': 'double',
                    },                            
                }
            }";

            var data = @"{
                'UpperLimit': 1.1111111111111111E+101
            }";

            var validationSchema = JsonSchema.FromJsonAsync(json).Result;

            /// Act
            var errors = validationSchema.Validate(data);

            /// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact(Skip = "Ignored")]
        public async Task When_integer_is_big_integer_then_validation_works()
        {
            // See https://github.com/RicoSuter/NJsonSchema/issues/568

            /// Arrange
            const string json = @"{
  ""type"": ""object"",
  ""properties"": {
    ""property1"": {
      ""type"": ""number""
    }
  },
  ""required"": [""property1""],
  ""additionalProperties"": false,
  ""additionalItems"": false
}";

            var data = @"{
                ""property1"": 34545734242323232423434
            }";

            var validationSchema = JsonSchema.FromJsonAsync(json).Result;

            /// Act
            var errors = validationSchema.Validate(data);

            /// Assert
            Assert.Equal(0, errors.Count);
        }

    }
}
