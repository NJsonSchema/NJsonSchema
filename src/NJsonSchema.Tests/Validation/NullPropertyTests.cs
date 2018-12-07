using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class NullPropertyTests
    {
        [Fact]
        public void When_property_can_be_null_then_null_is_allowed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Properties["test"] = new JsonProperty
            {
                Type = JsonObjectType.Null | JsonObjectType.Object
            };
            schema.Properties["test"].Properties["foo"] = new JsonProperty
            {
                Type = JsonObjectType.String
            };

            //// Act
            var data = "{ 'test': null }";
            var errors = schema.Validate(data);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        public class NullablePropertyClass
        {
            public QueryRule ReportRules { get; set; }
        }

        public class QueryRule
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task When_property_can_be_null_then_null_is_allowed2()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<NullablePropertyClass>();
            var schemaData = schema.ToJson();

            //// Act
            var data = "{ 'ReportRules': null }";
            var errors = schema.Validate(data);

            //// Assert
            Assert.Equal(0, errors.Count);
        }
    }
}