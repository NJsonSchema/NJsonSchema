using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Generation;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class EnumGenerationTests
    {
        public class Foo
        {
            public Bar Bar { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Bar Bar2 { get; set; }
        }

        public enum Bar
        {
            A = 0, 
            B = 5, 
            C = 6, 
        }

        [TestMethod]
        public void When_property_is_integer_enum_then_schema_has_enum()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson(); 

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["Bar"].ActualPropertySchema.Type);
            Assert.AreEqual(3, schema.Properties["Bar"].ActualPropertySchema.Enumeration.Count);
            Assert.AreEqual(0, schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(0));
            Assert.AreEqual(5, schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(1));
            Assert.AreEqual(6, schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(2));
        }

        [TestMethod]
        public void When_string_and_integer_enum_used_then_two_refs_are_generated()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Assert
            Assert.IsNotNull(schema.Properties["Bar"].ActualPropertySchema);
            Assert.IsNotNull(schema.Properties["Bar2"].ActualPropertySchema); // must not be a reference but second enum declaration
            Assert.AreNotEqual(schema.Properties["Bar"].ActualPropertySchema, schema.Properties["Bar2"].ActualPropertySchema);
        }

        [TestMethod]
        public void When_property_is_string_enum_then_schema_has_enum()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String
            });

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Bar"].ActualPropertySchema.Type);
            Assert.AreEqual(3, schema.Properties["Bar"].ActualPropertySchema.Enumeration.Count);
            Assert.AreEqual("A", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(0));
            Assert.AreEqual("B", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(1));
            Assert.AreEqual("C", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(2));
        }

        [TestMethod]
        public void When_enum_is_generated_then_names_are_set()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Assert
            Assert.AreEqual(3, schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.Count);
            Assert.AreEqual("A", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(0));
            Assert.AreEqual("B", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(1));
            Assert.AreEqual("C", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(2));
        }


        public class ClassWithStringEnum
        {
            public StringEnum Bar { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum StringEnum
        {
            [Display(Name="Zero Five Six Two")]
            [EnumMember(Value = "0562")]
            _0562,

            [EnumMember(Value = "0532")]
            _0532,

            _0502
        }


        [TestMethod]
        public void When_enum_is_generated_then_names_are_set_from_display_annotation_if_present()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<ClassWithStringEnum>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String
            });

            //// Assert
            Assert.AreEqual(3, schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.Count);
            Assert.AreEqual("Zero Five Six Two", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(0));
            Assert.AreEqual("0562", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(0));
            Assert.AreEqual("_0532", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(1));
            Assert.AreEqual("0532", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(1));
            Assert.AreEqual("_0502", schema.Properties["Bar"].ActualPropertySchema.EnumerationNames.ElementAt(2));
            Assert.AreEqual("_0502", schema.Properties["Bar"].ActualPropertySchema.Enumeration.ElementAt(2));
        }
    }
}