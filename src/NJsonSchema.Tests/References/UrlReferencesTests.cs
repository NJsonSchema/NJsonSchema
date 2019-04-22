using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.References
{
    public class UrlReferencesTests
    {
        // see https://github.com/RSuter/NJsonSchema/issues/566

        [Fact]
        public async Task Abc()
        {
            // Arrange

            // Act
            var schema = await JsonSchema4.FromUrlAsync(@"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json");

            // Assert
        }

        [Fact]
        public async Task Def()
        {
            // Arrange

            // see https://github.com/RSuter/NJsonSchema/issues/566

            // Act
            var schema = await
                JsonSchema4.FromUrlAsync(
                    "http://datafactories.schema.management.azure.com/schemas/2015-09-01/Microsoft.DataFactory.Pipeline.json");

            // Assert
        }
    }
}
