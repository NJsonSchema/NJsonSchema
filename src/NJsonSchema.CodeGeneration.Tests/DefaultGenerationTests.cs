using System.ComponentModel;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests
{
    public class DefaultGenerationTests
    {
        public class DefaultPropertyGenerationClass
        {
            [DefaultValue("foo")]
            public string Test { get; set; }
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_is_in_schema()
        {
            //// Arrange
            
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Assert
            Assert.Equal("foo", schema.Properties["Test"].Default); 
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_value_is_set_in_generated_INPC_CSharp_code()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Inpc;
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("private string _test = \"foo\";", code);
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_value_is_set_in_generated_Poco_CSharp_code()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Poco;
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public string Test { get; set; } = \"foo\";", code);
        }
    }
}