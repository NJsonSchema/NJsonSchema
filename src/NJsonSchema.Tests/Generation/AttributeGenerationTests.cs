using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AttributeGenerationTests
    {
#if !LEGACY

        [Fact]
        public async Task When_minLength_and_maxLength_attribute_are_set_on_array_then_minItems_and_maxItems_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Items"];

            //// Assert
            Assert.Equal(3, property.MinItems);
            Assert.Equal(5, property.MaxItems);
        }

        [Fact]
        public async Task When_minLength_and_maxLength_attribute_are_set_on_string_then_minLength_and_maxLenght_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["String"];

            //// Assert
            Assert.Equal(3, property.MinLength);
            Assert.Equal(5, property.MaxLength);
        }

#endif

        [Fact]
        public async Task When_Range_attribute_is_set_on_double_then_minimum_and_maximum_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Double"];

            //// Assert
            Assert.Equal(5.5m, property.Minimum);
            Assert.Equal(10.5m, property.Maximum);
        }

        [Fact]
        public async Task When_Range_attribute_has_double_max_then_max_is_not_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["DoubleOnlyMin"];

            //// Assert
            Assert.Equal(5.5m, property.Minimum);
            Assert.Equal(null, property.Maximum);
        }

        [Fact]
        public async Task When_Range_attribute_is_set_on_integer_then_minimum_and_maximum_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Integer"];

            //// Assert
            Assert.Equal(5, property.Minimum);
            Assert.Equal(10, property.Maximum);
        }

        [Fact]
        public async Task When_display_attribute_is_available_then_name_and_description_are_read()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Display"];

            //// Assert
            Assert.Equal("Foo", property.Title);
            Assert.Equal("Bar", property.Description);
        }

        [Fact]
        public async Task When_description_attribute_is_available_then_description_are_read()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Description"];

            //// Assert
            Assert.Equal("Abc", property.Description);
        }

        [Fact]
        public async Task When_required_attribute_is_available_then_property_is_required()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Required"];

            //// Assert
            Assert.True(property.IsRequired);
        }

        [Fact]
        public async Task When_required_attribute_is_not_available_then_property_is_can_be_null()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["Description"];

            //// Assert
            Assert.False(property.IsRequired);
            Assert.True(property.Type.HasFlag(JsonObjectType.Null));
        }

        [Fact]
        public async Task When_ReadOnly_is_set_then_readOnly_is_set_in_schema()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<AttributeTestClass>();
            var property = schema.Properties["ReadOnly"];

            //// Assert
            Assert.True(property.IsReadOnly);
        }

        public class AttributeTestClass
        {
#if !LEGACY
            [MinLength(3)]
            [MaxLength(5)]
#endif
            public string[] Items { get; set; }

#if !LEGACY
            [MinLength(3)]
            [MaxLength(5)]
#endif
            public string String { get; set; }

            [Range(5.5, 10.5)]
            public double Double { get; set; }

            [Range(5.5, double.MaxValue)]
            public double DoubleOnlyMin { get; set; }

            [Range(5, 10)]
            public int Integer { get; set; }

            [Display(Name = "Foo", Description = "Bar")]
            public string Display { get; set; }

            [System.ComponentModel.Description("Abc")]
            public string Description { get; set; }

            [Required]
            public bool Required { get; set; }

            [ReadOnly(true)]
            public bool ReadOnly { get; set; }
        }

        public class ClassWithTypedRange
        {
            [Range(typeof(decimal), "0", "1")]
            public decimal Foo { get; set; }
        }

        [Fact]
        public async Task When_range_has_type_and_strings_then_it_is_processed_correctly()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithTypedRange>();
            var property = schema.Properties["Foo"];

            //// Assert
            Assert.Equal(0.0m, property.Minimum);
            Assert.Equal(1.0m, property.Maximum);
        }
    }
}