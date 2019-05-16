using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class ContractResolverTests
    {
#if !NET45
        [Fact]
#endif
        public async Task Properties_should_match_custom_resolver()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>(new JsonSchemaGeneratorSettings
            {
                ContractResolver = new CustomContractResolver()
            });

            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("firstName"));
            Assert.Equal("firstName", schema.Properties["firstName"].Name);

            Assert.False(schema.Properties.ContainsKey("nameLength"));

            Assert.True(schema.Properties.ContainsKey("location"));
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, schema.Properties["location"].Type);
        }

        /// <summary>
        /// A contract resolver that
        ///  - camel cases properties
        ///  - does not serialize properties that are read only. 
        ///  - overrides the array contract if it has a string type converter
        /// </summary>
        public class CustomContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (!prop.Writable && member.GetCustomAttribute<JsonPropertyAttribute>(true) == null)
                    prop.ShouldSerialize = o => false;
                return prop;
            }

            protected override JsonContract CreateContract(Type objectType)
            {
                JsonContract contract = base.CreateContract(objectType);
                // by default a type that can convert to string and that is also an enum will have an array contract, but serialize to a string!. fix  this
                if (contract is JsonArrayContract && typeof(IEnumerable).IsAssignableFrom(objectType) 
                    && CanNonSystemTypeDescriptorConvertString(objectType))
                    contract = CreateStringContract(objectType);

                return contract;
            }

            static HashSet<string> _systemConverters = new HashSet<string>(new[] {
                "System.ComponentModel.ComponentConverter",
                "System.ComponentModel.ReferenceConverter",
                "System.ComponentModel.CollectionConverter" });

            public static bool CanNonSystemTypeDescriptorConvertString(Type type)
            {
                var typeConverter = TypeDescriptor.GetConverter(type); // somehow this does not work in NET45
                // use the objectType's TypeConverter if it has one and can convert to a string
                if (typeConverter != null)
                {
                    Type converterType = typeConverter.GetType();
                    if (!_systemConverters.Contains(converterType.FullName) && converterType != typeof(TypeConverter))
                        return typeConverter.CanConvertTo(typeof(string));
                }
                return false;
            }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public int NameLength => FirstName.Length;
            public LocationPath Location { get; set; }
        }

        /// <summary>
        /// A class that with a custom converter could serialize to a string.
        /// NOTE: The default contract resolver would resolve this as an array contract because it implements IEnumerable
        /// </summary>
        [TypeConverter(typeof(StringConverter<LocationPath>))]
        public class LocationPath : IStringConvertable, IEnumerable<string>
        {
            public ICollection<string> Path { get; set; } = new List<string>();

            public string StringValue
            {
                get { return string.Join("/", Path); ; }
                set { Path = new List<string>(value.Split('/')); }
            }

            public IEnumerator<string> GetEnumerator() => Path.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Path.GetEnumerator();

            public override string ToString() => StringValue;
        }

        interface IStringConvertable
        {
            string StringValue { get; set; }
            string ToString();
        }

        class StringConverter<T> : TypeConverter where T : IStringConvertable, new()
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                => sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value is string)
                    return new T() { StringValue = value.ToString() };
                return base.ConvertFrom(context, culture, value);
            }
        }

        [JsonObject(ItemRequired = Required.Always)]
        public class ClassWithClassLevelAttribute
        {
            public string Abc { get; set; }

            [JsonProperty("Foo")]
            public string Foo { get; set; }

            [JsonProperty("Bar", Required = Required.Default)]
            public string Bar { get; set; }
        }

        [Fact]
        public void When_JsonObject_ItemRequired_is_always_then_property_without_attribute_is_required_when_deserializing()
        {
            // Check whether JsonObjectAttribute.ItemRequired controls property without JsonPropertyAttribute or JsonPropertyAttribute.Required

            // Act
            Assert.Throws<JsonSerializationException>(() =>
            {
                // Exception: Foo not set
                JsonConvert.DeserializeObject<ClassWithClassLevelAttribute>("{ \"Abc\": \"abc\" }");
            });

            Assert.Throws<JsonSerializationException>(() =>
            {
                // Exception: Abc not set
                JsonConvert.DeserializeObject<ClassWithClassLevelAttribute>("{ \"Foo\": \"abc\" }");
            });
        }

        [Fact]
        public void When_JsonObject_ItemRequired_is_always_then_property_with_attribute_is_optional_when_deserializing()
        {
            // Check whether JsonPropertyAttribute.Required overrides JsonObjectAttribute.ItemRequired

            /// Act
            JsonConvert.DeserializeObject<ClassWithClassLevelAttribute>("{ \"Abc\": \"abc\", \"Foo\": \"abc\" }");
        }

        [Fact]
        public async Task When_JsonObject_ItemRequired_is_always_then_properties_without_attributes_are_required_in_schema()
        {
            // Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithClassLevelAttribute>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Abc"].IsRequired);
            Assert.True(schema.Properties["Foo"].IsRequired);
            Assert.False(schema.Properties["Bar"].IsRequired);
        }
    }
}
