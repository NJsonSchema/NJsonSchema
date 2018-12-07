using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Conversion
{
    public class TypeToSchemaTests
    {
        [Fact]
        public async Task When_converting_in_round_trip_then_json_should_be_the_same()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Act
            var schemaData1 = JsonConvert.SerializeObject(schema, Formatting.Indented);
            var schema2 = JsonConvert.DeserializeObject<JsonSchema4>(schemaData1);
            var schemaData2 = JsonConvert.SerializeObject(schema2, Formatting.Indented);

            //// Assert
            Assert.Equal(schemaData1, schemaData2);
        }

        [Fact]
        public async Task When_converting_simple_property_then_property_must_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["Integer"].Type);
            Assert.Equal(JsonObjectType.Number, schema.Properties["Decimal"].Type);
            Assert.Equal(JsonObjectType.Number, schema.Properties["Double"].Type);
            Assert.Equal(JsonObjectType.Boolean, schema.Properties["Boolean"].Type);
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, schema.Properties["String"].Type);
            Assert.Equal(JsonObjectType.Array | JsonObjectType.Null, schema.Properties["Array"].Type);
        }

        [Fact]
        public async Task When_converting_nullable_simple_property_then_property_must_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.Equal(JsonObjectType.Integer | JsonObjectType.Null, schema.Properties["NullableInteger"].Type);
            Assert.Equal(JsonObjectType.Number | JsonObjectType.Null, schema.Properties["NullableDecimal"].Type);
            Assert.Equal(JsonObjectType.Number | JsonObjectType.Null, schema.Properties["NullableDouble"].Type);
            Assert.Equal(JsonObjectType.Boolean | JsonObjectType.Null, schema.Properties["NullableBoolean"].Type);
        }

        [Fact]
        public async Task When_converting_property_with_description_then_description_should_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.Equal("Test", schema.Properties["Integer"].Description);
        }

        [Fact]
        public async Task When_converting_required_property_then_it_should_be_required_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.True(schema.Properties["RequiredReference"].IsRequired);
        }

        [Fact]
        public async Task When_converting_regex_property_then_it_should_be_set_as_pattern()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.Equal("regex", schema.Properties["RegexString"].Pattern);
        }

        public class ClassWithRegexDictionaryProperty
        {
            [RegularExpression("^\\d+\\.\\d+\\.\\d+\\.\\d+$")]
            public Dictionary<string, string> Versions { get; set; }
        }

        [Fact]
        public async Task When_dictionary_property_has_regex_attribute_then_regex_is_added_to_additionalProperties()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithRegexDictionaryProperty>();
            var json = schema.ToJson();

            //// Assert
            Assert.Null(schema.Properties["Versions"].Pattern);
            Assert.NotNull(schema.Properties["Versions"].AdditionalPropertiesSchema.ActualSchema.Pattern);
        }

        [Fact]
        public async Task When_converting_range_property_then_it_should_be_set_as_min_max()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.Equal(5, schema.Properties["RangeInteger"].Minimum);
            Assert.Equal(10, schema.Properties["RangeInteger"].Maximum);
        }

        [Fact]
        public async Task When_converting_not_nullable_properties_then_they_should_have_null_type()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.False(schema.Properties["Integer"].IsRequired);
            Assert.False(schema.Properties["Decimal"].IsRequired);
            Assert.False(schema.Properties["Double"].IsRequired);
            Assert.False(schema.Properties["Boolean"].IsRequired);
            Assert.False(schema.Properties["String"].IsRequired);

            Assert.False(schema.Properties["Integer"].Type.HasFlag(JsonObjectType.Null));
            Assert.False(schema.Properties["Decimal"].Type.HasFlag(JsonObjectType.Null));
            Assert.False(schema.Properties["Double"].Type.HasFlag(JsonObjectType.Null));
            Assert.False(schema.Properties["Boolean"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["String"].Type.HasFlag(JsonObjectType.Null));
        }

        [Fact]
        public async Task When_generating_nullable_primitive_properties_then_they_should_have_null_type()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.True(schema.Properties["NullableInteger"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["NullableDecimal"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["NullableDouble"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["NullableBoolean"].Type.HasFlag(JsonObjectType.Null));
        }

        [Fact]
        public async Task When_property_is_renamed_then_the_name_must_be_correct()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("abc"));
            Assert.False(schema.Properties.ContainsKey("ChangedName"));
        }

        [Fact]
        public async Task When_converting_object_then_it_should_be_correct()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();
            var data = schema.ToJson();

            //// Assert
            var property = schema.Properties["Reference"];
            Assert.True(property.IsNullable(SchemaType.JsonSchema));
            Assert.Contains(schema.Definitions, d => d.Key == "MySubtype");
        }
        
        [Fact]
        public async Task When_converting_enum_then_enum_array_must_be_set()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var property = schema.Properties["Color"];

            //// Assert
            Assert.Equal(3, property.ActualTypeSchema.Enumeration.Count); // Color property has StringEnumConverter
            Assert.True(property.ActualTypeSchema.Enumeration.Contains("Red"));
            Assert.True(property.ActualTypeSchema.Enumeration.Contains("Green"));
            Assert.True(property.ActualTypeSchema.Enumeration.Contains("Blue"));
        }

        public class ClassWithJObjectProperty
        {
            public JObject Property { get; set; }
        }

        [Fact]
        public async Task When_type_is_JObject_then_generated_type_is_any()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithJObjectProperty>();
            var schemaData = schema.ToJson();
            var property = schema.Properties["Property"];

            //// Assert
            Assert.True(property.IsNullable(SchemaType.JsonSchema));
            Assert.True(property.ActualTypeSchema.IsAnyType);
            Assert.True(property.ActualTypeSchema.AllowAdditionalItems);
            Assert.Equal(0, property.Properties.Count);
        }

        [Fact]
        public async Task When_converting_array_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("Array");
        }

        [Fact]
        public async Task When_converting_collection_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("Collection");
        }

        [Fact]
        public async Task When_converting_list_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("List");
        }

        private async Task When_converting_smth_then_items_must_correctly_be_loaded(string propertyName)
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            var property = schema.Properties[propertyName];

            Assert.Equal(JsonObjectType.Array | JsonObjectType.Null, property.Type);
            Assert.Equal(JsonObjectType.Object, property.ActualSchema.Item.ActualSchema.Type);
            Assert.Contains(schema.Definitions, d => d.Key == "MySubtype");
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, property.ActualSchema.Item.ActualSchema.Properties["Id"].Type);
        }

        public class MyType
        {
            [System.ComponentModel.Description("Test")]
            public int Integer { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public bool Boolean { get; set; }

            public int? NullableInteger { get; set; }
            public decimal? NullableDecimal { get; set; }
            public double? NullableDouble { get; set; }
            public bool? NullableBoolean { get; set; }

            public string String { get; set; }

            [JsonProperty("abc")]
            public string ChangedName { get; set; }

            [Required]
            public MySubtype RequiredReference { get; set; }

            [RegularExpression("regex")]
            public string RegexString { get; set; }

            [Range(5, 10)]
            public int RangeInteger { get; set; }

            public MySubtype Reference { get; set; }
            public MySubtype[] Array { get; set; }
            public Collection<MySubtype> Collection { get; set; }
            public List<MySubtype> List { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public MyColor Color { get; set; }
        }

        public class MySubtype
        {
            public string Id { get; set; }
        }

        public enum MyColor
        {
            Red,
            Green,
            Blue
        }
    }
}
