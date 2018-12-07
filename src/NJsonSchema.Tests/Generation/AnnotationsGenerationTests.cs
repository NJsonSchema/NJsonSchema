using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Annotations;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AnnotationsGenerationTests
    {
        public class AnnotationClass
        {
            public MyPoint Point { get; set; }

            [JsonSchema(JsonObjectType.String, Format = "point")]
            public AnnotationClass ClassAsString { get; set; }

            [JsonSchema(JsonObjectType.String, Format = "point")]
            public class MyPoint
            {
                public decimal X { get; set; }

                public decimal Y { get; set; }
            }
        }

        [Fact]
        public async Task When_class_annotation_is_available_then_type_and_format_can_be_customized()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<AnnotationClass>();
            var data = schema.ToJson();

            //// Act
            var property = schema.Properties["Point"];

            //// Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("point", property.Format);
        }

        [Fact]
        public async Task When_property_annotation_is_available_then_type_and_format_can_be_customized()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<AnnotationClass>();
            var data = schema.ToJson();

            //// Act
            var property = schema.Properties["ClassAsString"];

            //// Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("point", property.Format);
        }

        public class DateAttributeClass
        {
            [JsonSchemaDate]
            public DateTime Date { get; set; }
        }

        [Fact]
        public async Task When_DateTime_property_has_JsonSchemaDate_attribute_then_format_and_type_is_correct()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DateAttributeClass>();
            var data = schema.ToJson();

            //// Act
            var property = schema.Properties["Date"];

            //// Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("date", property.Format);
        }

        public class MultipleOfClass
        {
            [MultipleOf(4.5)]
            public double Number { get; set; }
        }

        [Fact]
        public async Task When_multipleOf_attribute_is_available_then_value_is_set_in_schema()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MultipleOfClass>();
            var property = schema.Properties["Number"];

            //// Assert
            Assert.Equal(4.5m, property.MultipleOf.Value);
        }

        public class SimpleClass
        {
            [JsonProperty("number")]
            public decimal Number { get; set; }

            public SimpleClass(decimal number)
            {
                Number = number;
            }
        }

        [Fact]
        public async Task When_multipleOf_is_fraction_then_it_is_validated_correctly()
        {
            //// Arrange
            List<SimpleClass> testClasses = new List<SimpleClass>();
            for (int i = 0; i < 100; i++)
            {
                testClasses.Add(new SimpleClass((decimal)(0.1 * i)));
            }

            string jsonData = JsonConvert.SerializeObject(testClasses, Formatting.Indented);
            var schema = await JsonSchema4.FromJsonAsync(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""object"",
    ""properties"": {
      ""number"": {
        ""type"": ""number"",
          ""multipleOf"": 0.1,
          ""minimum"": 0.0,
          ""maximum"": 4903700.0
      }
    },
    ""required"": [
      ""number""
    ]
  }
}");

            //// Act
            var errors = schema.Validate(jsonData);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel : IEnumerable<string>
        {
            public IEnumerator<string> GetEnumerator()
            {
                return null;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public async Task When_class_has_array_item_type_defined_then_schema_has_this_item_type()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ArrayModel>();

            //// Act
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Item.Type);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel<T> : List<T>
        {
        }

        [Fact]
        public async Task When_class_has_array_item_type_defined_then_schema_has_this_item_type2()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ArrayModel<string>>();

            //// Act
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Item.Type);
        }

        public class MyStructContainer
        {
            public MyStruct Struct { get; set; }

            public MyStruct? NullableStruct { get; set; }
        }

        [JsonSchema(JsonObjectType.String)]
        public struct MyStruct
        {
        }

        [Fact]
        public async Task When_property_is_struct_then_it_is_not_nullable()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyStructContainer>();

            //// Act
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Struct"].Type);
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, schema.Properties["NullableStruct"].Type);
        }

        public class StringLengthAttributeClass
        {
            [StringLength(10, MinimumLength = 5)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_StringLengthAttribute_is_set_then_minLength_and_maxLenght_is_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<StringLengthAttributeClass>();

            //// Assert
            var property = schema.Properties["Foo"];

            Assert.Equal(5, property.MinLength);
            Assert.Equal(10, property.MaxLength);
        }

        public class StringRequiredClass
        {
            [Required(AllowEmptyStrings = false)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_RequiredAttribute_is_set_with_AllowEmptyStrings_false_then_minLength_and_required_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<StringRequiredClass>();

            //// Assert
            var property = schema.Properties["Foo"];

            Assert.Equal(1, property.MinLength);
            Assert.True(property.IsRequired);
        }

        public class DtoRequiredClass
        {
            [Required(AllowEmptyStrings = false)]
            public StringRequiredClass Foo { get; set; }
        }

        [Fact]
        public async Task When_RequiredAttribute_is_set_with_AllowEmptyStrings_false_on_class_property_then_minLength_is_not_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DtoRequiredClass>();
            var json = schema.ToJson();

            //// Assert
            var property = schema.Properties["Foo"];

            Assert.Null(property.MinLength);
            Assert.True(property.IsRequired);
        }

        public class DataTypeAttributeClass
        {
            [DataType(DataType.EmailAddress)]
            public string EmailAddress { get; set; }

            [DataType(DataType.PhoneNumber)]
            public string PhoneNumber { get; set; }

            [DataType(DataType.DateTime)]
            public string DateTime { get; set; }

            [DataType(DataType.Date)]
            public string Date { get; set; }

            [DataType(DataType.Time)]
            public string Time { get; set; }

            [DataType(DataType.Url)]
            public string Url { get; set; }

#if !LEGACY
            [DataType(DataType.Upload)]
            public string Upload { get; set; }
#endif
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_DateTime_then_the_format_property_is_datetime()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["DateTime"];

            Assert.Equal("date-time", property.Format);
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_Date_then_the_format_property_is_date()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["Date"];

            Assert.Equal("date", property.Format);
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_Time_then_the_format_property_is_time()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["Time"];

            Assert.Equal("time", property.Format);
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_EmailAddress_then_the_format_property_is_email()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["EmailAddress"];

            Assert.Equal("email", property.Format);
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_PhoneNumber_then_the_format_property_is_phone()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["PhoneNumber"];

            Assert.Equal("phone", property.Format);
        }

        [Fact]
        public async Task When_DataTypeAttribute_is_Url_then_the_format_property_is_uri()
        {
            var schema = await JsonSchema4.FromTypeAsync<DataTypeAttributeClass>();
            var property = schema.Properties["Url"];

            Assert.Equal("uri", property.Format);
        }

        [JsonSchemaIgnore]
        public class BaseObject
        {
            public string Foo { get; set; }
        }

        public class Person : BaseObject
        {
            public string Bar { get; set; }
        }

        public class Student : Person
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_class_is_ignored_then_it_is_not_in_definitions()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<Student>();
            var json = schema.ToJson();

            /// Assert
            Assert.False(schema.Definitions.ContainsKey("BaseObject"));
        }
    }
}