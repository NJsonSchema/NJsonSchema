using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class FieldGenerationTests
    {
        public class MyTest
        {
            public string MyField;
        }

        [Fact]
        public async Task When_public_field_is_available_then_it_is_added_as_property()
        {
            //// Arrange
            

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyTest>();
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["MyField"].Type.HasFlag(JsonObjectType.String));
        }
    }
}
