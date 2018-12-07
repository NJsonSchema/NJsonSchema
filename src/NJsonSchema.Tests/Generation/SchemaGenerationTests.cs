using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class SchemaGenerationTests
    {
        public class Foo
        {
            public Dictionary<string, string> Dictionary { get; set; }

            public Bar Bar { get; set; }

            public DateTimeOffset Time { get; set; }
        }

        public class Bar
        {
            public string Name { get; set; }
        }
        
        [Fact]
        public async Task When_generating_schema_with_object_property_then_additional_properties_are_not_allowed()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Equal(false, schema.Properties["Bar"].ActualTypeSchema.AllowAdditionalProperties);
        }

        [Fact]
        public async Task When_generating_DateTimeOffset_property_then_format_datetime_must_be_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Time"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["Time"].Format);
        }

        [Fact]
        public async Task When_generating_schema_with_dictionary_property_then_it_must_allow_additional_properties()
        {
            //// Arrange
            
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Equal(true, schema.Properties["Dictionary"].ActualSchema.AllowAdditionalProperties);
            Assert.Equal(JsonObjectType.String, schema.Properties["Dictionary"].ActualSchema.AdditionalPropertiesSchema.ActualSchema.Type);
            // "#/definitions/ref_7de8187d_d860_41fa_a17b_3f395c053cae"
        }

        [Fact]
        public async Task When_output_schema_contains_reference_then_schema_reference_path_is_human_readable()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Contains("#/definitions/Bar", schemaData);
        }

        public class DefaultTests
        {
            [DefaultValue(10)]
            public int Number { get; set; }
        }
        
        [Fact]
        public async Task When_default_value_is_set_on_property_then_default_is_set_in_schema()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DefaultTests>();

            //// Act
            var property = schema.Properties["Number"];

            //// Assert
            Assert.Equal(10, property.Default);
        }

        public class DictTest
        {
            public Dictionary<string, object> values { get; set; }
        }

        [Fact]
        public async Task When_dictionary_value_is_null_then_string_values_are_allowed()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DictTest>();
            var schemaData = schema.ToJson();

            var data = @"{
                ""values"": { 
                    ""key"": ""value"", 
                }
            }";

            //// Act
            var errors = schema.Validate(data);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public async Task When_type_is_enumerable_it_should_not_stackoverflow_on_JSON_generation()
        {
            //// Generate JSON
            var schema = await JsonSchema4.FromTypeAsync<IEnumerable<Tuple<string, string>>>();
            var json = schema.ToJson();

            //// Should be reached and not StackOverflowed
            Assert.True(!string.IsNullOrEmpty(json));
        }

        public class FilterDto
        {
            public long FieldId { get; set; }

            public object Value { get; set; }
        }

        [Fact]
        public async Task When_property_is_object_then_it_should_not_be_a_dictonary_but_any()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<FilterDto>();
            var json = schema.ToJson();

            /// Assert
            var property = schema.Properties["Value"].ActualTypeSchema;
            Assert.True(property.IsAnyType);
            Assert.False(property.IsDictionary);
        }

        public class ClassWithStaticProperty
        {
            public static string Foo { get; set; }

            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_property_is_static_then_it_is_ignored()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithStaticProperty>();
            var json = schema.ToJson();

            /// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.ActualProperties.ContainsKey("Bar"));
        }

        // Used as demo for https://github.com/swagger-api/swagger-ui/issues/1056

        //public class TestClass
        //{
        //    [Required] // <= not nullable
        //    public ReferencedClass RequiredProperty { get; set; }

        //    public ReferencedClass NullableProperty { get; set; }
        //}

        //public class ReferencedClass
        //{
        //    public string Test { get; set; }
        //}

        //[Fact]
        //public void Demo()
        //{
        //    var schema = await JsonSchema4.FromTypeAsync<TestClass>();
        //    var json = schema.ToJson();
        //}
    }
}
