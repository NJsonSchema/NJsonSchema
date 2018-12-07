using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NJsonSchema.CodeGeneration.TypeScript;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.Samples
{
    public class SampleTests
    {
        public class Person
        {
            [Required]
            public string FirstName { get; set; }

            public string MiddleName { get; set; }

            [Required]
            public string LastName { get; set; }

            public Gender Gender { get; set; }

            [Range(2, 5)]
            public int NumberWithRange { get; set; }

            public DateTime Birthday { get; set; }

            public Company Company { get; set; }

            public Collection<Car> Cars { get; set; }
        }

        public enum Gender
        {
            Male,
            Female
        }

        public class Car
        {
            public string Name { get; set; }

            public Company Manufacturer { get; set; }
        }

        public class Company
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task Demo()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>();
            var schemaJsonData = schema.ToJson();
            var errors = schema.Validate("{}");
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class, TypeScriptVersion = 2.0m });
            var code = generator.GenerateFile();
        }

        [Fact]
        public async Task Demo2()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>();
            var schemaJsonData = schema.ToJson();
            var errors = schema.Validate("{}");
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.0m });
            var code = generator.GenerateFile();
        }


        [Fact]
        public async Task When_JSON_contains_DateTime_is_available_then_string_validator_validates_correctly()
        {
            //// Arrange
            var schemaJson = @"{
            ""$schema"": ""http://json-schema.org/draft-04/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""SimpleDate"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""PatternDate"": {
                    ""type"": ""string"",
                    ""pattern"" : ""(^[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}T[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}Z$|^$)""
                    }
                }
            }";
            var schema = await JsonSchema4.FromJsonAsync(schemaJson);

            var dataJson = @"{
                ""SimpleDate"":""2012-05-18T00:00:00Z"",
                ""PatternDate"":""2012-11-07T00:00:00Z""
            }";

            //// Act
            var errors = schema.Validate(dataJson);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public async Task When_JSON_contains_DateTime_is_available_then_JObject_validator_validates_correctly()
        {
            //// Arrange
            var schemaJson = @"{
            ""$schema"": ""http://json-schema.org/draft-04/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""SimpleDate"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""PatternDate"": {
                    ""type"": ""string"",
                    ""pattern"" : ""(^[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}T[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}Z$|^$)""
                    }
                }
            }";
            var schema = await JsonSchema4.FromJsonAsync(schemaJson);

            var data = JObject.Parse(@"{
                ""SimpleDate"":""2012-05-18T00:00:00Z"",
                ""PatternDate"":""2012-11-07T00:00:00Z""
            }");

            var value = data["SimpleDate"].Value<string>(); // not original format

            //// Act
            var errors = schema.Validate(data);

            //// Assert
            Assert.Equal(0, errors.Count);
        }
    }
}
