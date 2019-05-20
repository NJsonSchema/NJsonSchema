using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Converters;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class InheritanceTests
    {
        public class MyContainer
        {
            public EmptyClassInheritingDictionary CustomDictionary { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public sealed class EmptyClassInheritingDictionary : Dictionary<string, object>
        {
        }

        [Fact]
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyContainer>();
            var data = schema.ToJson();

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];

            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.DoesNotContain("class CustomDictionary :", code);
            Assert.Contains("public EmptyClassInheritingDictionary CustomDictionary", code);
            Assert.Contains("public partial class EmptyClassInheritingDictionary : System.Collections.Generic.Dictionary<string, object>", code);
        }

        [KnownType(typeof(MyException))]
        [JsonConverter(typeof(JsonInheritanceConverter), "kind")]
        public class ExceptionBase : Exception
        {
            public string Foo { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public class MyException : ExceptionBase
        {
            public string Bar { get; set; }
        }

        public class ExceptionContainer
        {
            public ExceptionBase Exception { get; set; }
        }

        [Fact]
        public async Task When_class_with_discriminator_has_base_class_then_csharp_is_generated_correctly()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.Contains("class ExceptionBase : Exception", code);
            Assert.Contains("class MyException : ExceptionBase", code);
        }

        [Fact]
        public async Task When_property_references_any_schema_with_inheritance_then_property_type_is_correct()
        {
            //// Arrange
            var json = @"{
    ""type"": ""object"",
    ""properties"": {
        ""dog"": {
            ""$ref"": ""#/definitions/Dog""
        }
    },
    ""definitions"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""name"": {
                    ""type"": ""string""
                }
            }
        },
        ""Dog"": {
            ""title"": ""Dog"",
            ""description"": """",
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/Pet""
                },
                {
                    ""type"": ""object""
                }
            ]
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("public Dog Dog { get; set; }", code);
        }

        [Fact]
        public async Task When_schema_with_inheritance_is_any_then_no_exception_is_thrown()
        {
            //// Arrange
            var json = @"{
    ""type"": ""object"",
    ""properties"": {
        ""field"": {
            ""$ref"": ""#/definitions/ChildDataType""
        }
    },
    ""definitions"": {
        ""ParentDataType"": {
            ""title"": ""ParentDataType"",
            ""description"": """",
            ""type"": ""object""
        },
        ""ChildDataType"": {
            ""title"": ""ChildDataType"",
            ""description"": """",
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/ParentDataType""
                },
                {
                    ""type"": ""object""
                }
            ]
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("public ChildDataType Field { get; set; }", code);
            Assert.Contains("ChildDataType : ParentDataType", code);
        }
    }
}
