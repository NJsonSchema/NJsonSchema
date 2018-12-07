using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class XmlObjectTests
    {
        private const string StringArray = "StringArray";
        private const string IntArray = "IntArray";
        private const string DoubleArray = "DoubleArray";
        private const string DecimalArray = "DecimalArray";
        private const string InternalItemArray = "InternalItem";
        private const string ShouldBeThisPropertyName = "ShouldBeThisPropertyName";
        private const string Foo = "Foo";

        public class WithoutXmlAttributesDefined
        {
            public string Foo { get; set; }
            public string[] StringArray { get; set; }
            public int[] IntArray { get; set; }
            public double[] DoubleArray { get; set; }
            public decimal[] DecimalArray { get; set; }
            public WithoutXmlAttributeItem[] InternalItem { get; set; }

            public WithXmlAttribute ShouldBeThisPropertyName { get; set; }

            public class WithoutXmlAttributeItem
            {
                public string Name { get; set; }
            }

            [XmlType("ShouldNOTBeThis")]
            public class WithXmlAttribute
            {
                public string Name { get; set; }
            }
            private static WithoutXmlAttributesDefined Data => new WithoutXmlAttributesDefined()
            {
                Foo = "stringvalue",
                StringArray = new[] { "S1" },
                IntArray = new[] { 1 },
                DoubleArray = new double[] { 1 },
                DecimalArray = new decimal[] { 1 },
                InternalItem = new[] { new WithoutXmlAttributeItem() { Name = "Test" } },
                ShouldBeThisPropertyName = new WithXmlAttribute() { Name = "Test2" }
            };

            public static string CreateTestXml()
            {
                return GetSerializedContent(Data);
            }

            public static string CreateTestArrayXml()
            {
                var list = new List<WithoutXmlAttributesDefined> {Data, Data};
                return GetSerializedContent(list);
            }
        }

        /// <summary>
        ///The test verfies that a plain C# class serialized with XML serializer is reflected in the Swagger spec correctly. Below is the example output from CreateTestXML()
        /// 
        ///<WithoutXmlAttributesDefined xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        /// <Foo>stringvalue</Foo>
        /// <StringArray>
        ///   <string>S1</string>
        /// </StringArray>
        /// <IntArray>
        ///   <int>1</int>
        /// </IntArray>
        /// <DoubleArray>
        ///  <double>1</double>
        /// </DoubleArray>
        /// <DecimalArray>
        ///  <decimal>1</decimal>
        /// </DecimalArray>
        /// <InternalItem>
        ///  <WithoutXmlAttributeItem>
        ///  <Name>Test</Name>
        ///  </WithoutXmlAttributeItem>
        /// </InternalItem>
        /// <ShouldBeThisPropertyName>
        ///  <Name>Test2</Name>
        /// </ShouldBeThisPropertyName>
        ///</WithoutXmlAttributesDefined>
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            
            //// Assert
            AssertTypeWithoutXmlAttributes(schema);
        }

        /// <summary>
        /// The test verfies that an array of plain C# class serialized with XML serializer is reflected in the Swagger spec correctly. Below is the example output from CreateTestArrayXML()
        /// 
        ///<ArrayOfWithoutXmlAttributesDefined xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        ///<WithoutXmlAttributesDefined>
        ///<Foo>stringvalue</Foo>
        ///<StringArray>
        ///<string>S1</string>
        ///</StringArray>
        ///<IntArray>
        ///<int>1</int>
        ///</IntArray>
        ///<DoubleArray>
        ///<double>1</double>
        ///</DoubleArray>
        ///<DecimalArray>
        ///<decimal>1</decimal>
        ///</DecimalArray>
        ///<InternalItem>
        ///<WithoutXmlAttributeItem>
        ///<Name>Test</Name>
        ///</WithoutXmlAttributeItem>
        ///</InternalItem>
        ///<ShouldBeThisPropertyName>
        ///<Name>Test2</Name>
        ///</ShouldBeThisPropertyName>
        ///</WithoutXmlAttributesDefined>
        ///<WithoutXmlAttributesDefined>
        ///<Foo>stringvalue</Foo>
        ///<StringArray>
        ///<string>S1</string>
        ///</StringArray>
        ///<IntArray>
        ///<int>1</int>
        ///</IntArray>
        ///<DoubleArray>
        ///<double>1</double>
        ///</DoubleArray>
        ///<DecimalArray>
        ///<decimal>1</decimal>
        ///</DecimalArray>
        ///<InternalItem>
        ///<WithoutXmlAttributeItem>
        ///<Name>Test</Name>
        ///</WithoutXmlAttributeItem>
        ///</InternalItem>
        ///<ShouldBeThisPropertyName>
        ///<Name>Test2</Name>
        ///</ShouldBeThisPropertyName>
        ///</WithoutXmlAttributesDefined>
        ///</ArrayOfWithoutXmlAttributesDefined>
        ///</summary>
        ///<returns></returns>
        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_arraytype_as_parent_with_without_xml_attributes_defined()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined[]>(new JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();
            var schemaObject = JObject.Parse(schemaData);

            //// Assert
            var propertyXml = schemaObject["xml"];
            Assert.Equal("ArrayOfWithoutXmlAttributesDefined", propertyXml["name"]);
            Assert.True(propertyXml["wrapped"].Value<bool>());

            var itemsXml = schemaObject["items"]["xml"];
            Assert.Equal("WithoutXmlAttributesDefined", itemsXml["name"]);

            var definitionSchema = schema.Definitions["WithoutXmlAttributesDefined"];
            AssertTypeWithoutXmlAttributes(definitionSchema);
        }

        private void AssertTypeWithoutXmlAttributes(JsonSchema4 schema)
        {
            Assert.Null(schema.Xml);
            var fooProperty = schema.Properties[Foo];
            var stringArrayProperty = schema.Properties[StringArray];
            var intArrayProperty = schema.Properties[IntArray];
            var doubleArrayProperty = schema.Properties[DoubleArray];
            var decimalArrayProperty = schema.Properties[DecimalArray];
            var internalItemProperty = schema.Properties[InternalItemArray];
            var shouldBeThisNameProperty = schema.Properties[ShouldBeThisPropertyName];

            Assert.Null(internalItemProperty.Xml.Name);
            Assert.Null(stringArrayProperty.Xml.Name);
            Assert.Null(intArrayProperty.Xml.Name);
            Assert.Null(doubleArrayProperty.Xml.Name);
            Assert.Null(decimalArrayProperty.Xml.Name);
            Assert.Null(fooProperty.Xml);

            //https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.True(stringArrayProperty.Xml.Wrapped);
            Assert.True(intArrayProperty.Xml.Wrapped);
            Assert.True(doubleArrayProperty.Xml.Wrapped);
            Assert.True(decimalArrayProperty.Xml.Wrapped);
            Assert.True(internalItemProperty.Xml.Wrapped);

            Assert.Equal("string", stringArrayProperty.Item.Xml.Name);
            Assert.Equal("int", intArrayProperty.Item.Xml.Name);
            Assert.Equal("double", doubleArrayProperty.Item.Xml.Name);
            Assert.Equal("decimal", decimalArrayProperty.Item.Xml.Name);
            Assert.NotNull(internalItemProperty.Item.Xml);
            //https://github.com/swagger-api/swagger-ui/issues/2610
            Assert.NotNull(shouldBeThisNameProperty.Xml); // Make sure that type reference properties have an xml object
            Assert.Equal(shouldBeThisNameProperty.Name, shouldBeThisNameProperty.Xml.Name); // Make sure that the property name and the xml name is the same
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            var schemaObject = JObject.Parse(schemaData);

            var definitionXml = schemaObject["xml"];
            Assert.Null(definitionXml);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.Null(fooPropertyXml);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.True(arrayStringPropertyOuterXml["wrapped"].Value<bool>());
            Assert.Null(arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.Equal("string", arrayStringPropertyItemXml["name"].Value<string>());
        }

        [XmlType(TypeName = "NotTheSameName", Namespace = "http://test.shema.org/type")]
        public class WithXmlAttributesDefined
        {
            [XmlElement(ElementName = "Bar", Namespace = "http://test.shema.org/type")]
            public string Foo { get; set; }

            [XmlAttribute("IsAnAttribute")]
            public string MightBeAAttribute { get; set; }

            [XmlArray(ElementName = "TheStrings", Namespace = "http://test.shema.org/type")]
            [XmlArrayItem(ElementName = "TheString", Namespace = "http://test.shema.org/type")]
            public string[] StringArray { get; set; }

            [XmlArrayItem(ElementName = "TheInt", Namespace = "http://test.shema.org/type")]
            public int[] TheInts { get; set; }

            [XmlArray("ExternalItems")]
            public WithXmlAttributeItem[] InternalItems { get; set; }

            public WithXmlAttributeItem2[] ExternalItems2 { get; set; }

            public WithXmlAttributeProperty ReferenceProperty { get; set; }

            [XmlType("ExternalItem")]
            public class WithXmlAttributeItem
            {
                public string Name { get; set; }
            }

            [XmlType("ExternalItem2")]
            public class WithXmlAttributeItem2
            {
                public string Name { get; set; }
            }

            [XmlType("NotAPropertyName")]
            public class WithXmlAttributeProperty
            {
                public string Name { get; set; }
            }

            private static WithXmlAttributesDefined Data => new WithXmlAttributesDefined()
            {
                Foo = "stringvalue",
                MightBeAAttribute = "stringvalue",
                StringArray = new[] { "S1" },
                TheInts = new[] { 1 },
                InternalItems = new[] { new WithXmlAttributeItem() { Name = "Test" } },
                ReferenceProperty = new WithXmlAttributeProperty() { Name = "Test" },
                ExternalItems2 = new[] { new WithXmlAttributeItem2() { Name = "Test" } },
            };

            public static string CreateTestXml()
            {
                return GetSerializedContent(Data);
            }

            public static string CreateTestArrayXml()
            {
                var items = new List<WithXmlAttributesDefined>
                {
                    Data,
                    Data
                };
                return GetSerializedContent(items);
            }
        }

        /// <summary>
        /// The test verfies that a C# class with Xml* attributes serialized with XML serializer is reflected in the Swagger spec correctly. Below is the example output from CreateTestXML()
        /// 
        ///<NotTheSameName xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" IsAnAttribute="stringValue2">
        /// <Bar xmlns = "http://test.shema.org/type">stringvalue </Bar>
        /// <TheStrings xmlns="http://test.shema.org/type">
        ///  <TheString>S1</TheString>
        /// </TheStrings>
        /// <TheInts xmlns = "http://test.shema.org/type">
        ///   <TheInt>1</TheInt>
        /// </TheInts>
        /// <ExternalItems xmlns="http://test.shema.org/type">
        ///   <ExternalItem>
        ///    <Name>Test</Name>
        ///  </ExternalItem>
        /// </ExternalItems>
        /// <ExternalItems2 xmlns = "http://test.shema.org/type">
        ///   <ExternalItem2>
        ///    <Name>Test</Name>
        ///  </ExternalItem2>
        /// </ExternalItems2>
        /// <ReferenceProperty xmlns="http://test.shema.org/type">
        ///   <Name>Test</Name>
        /// </ReferenceProperty>
        ///</NotTheSameName>
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });
            
            //// Assert
            AssertTypeWithXmlAttributes(schema, schema);
        }

        /// <summary>
        /// The test verfies that an array of a C# class with Xml* attributes serialized with XML serializer is reflected in the Swagger spec correctly. Below is the example output from CreateTestArrayXML()
        /// 
        ///<ArrayOfNotTheSameName xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        ///<NotTheSameName IsAnAttribute = "stringvalue">
        /// <Bar xmlns="http://test.shema.org/type">stringvalue</Bar>
        ///  <TheStrings xmlns = "http://test.shema.org/type">
        ///   <TheString>S1</TheString>
        ///  </TheStrings>
        ///  <TheInts xmlns="http://test.shema.org/type">
        ///    <TheInt>1</TheInt>
        ///  </TheInts>
        ///  <ExternalItems xmlns = "http://test.shema.org/type">
        ///    <ExternalItem>
        ///    <Name>Test</Name>
        ///   </ExternalItem>
        ///  </ExternalItems>
        ///  <ExternalItems2 xmlns="http://test.shema.org/type">
        ///    <ExternalItem2>
        ///     <Name>Test</Name>
        ///    </ExternalItem2>
        ///   </ExternalItems2>
        ///   <ReferenceProperty xmlns = "http://test.shema.org/type">
        ///     <Name>Test</Name>
        ///   </ReferenceProperty>
        ///</NotTheSameName>
        ///<NotTheSameName IsAnAttribute="stringvalue">
        /// <Bar xmlns = "http://test.shema.org/type">stringvalue</Bar>
        /// <TheStrings xmlns="http://test.shema.org/type">
        ///   <TheString>S1</TheString>
        /// </TheStrings>
        /// <TheInts xmlns = "http://test.shema.org/type">
        ///   <TheInt>1</TheInt>
        /// </TheInts>
        /// <ExternalItems xmlns="http://test.shema.org/type">
        ///  <ExternalItem>
        ///    <Name>Test</Name>
        ///  </ExternalItem>
        /// </ExternalItems>
        /// <ExternalItems2 xmlns = "http://test.shema.org/type">
        ///   <ExternalItem2>
        ///     <Name >Test</Name>
        ///   </ExternalItem2>
        /// </ExternalItems2>
        /// <ReferenceProperty xmlns="http://test.shema.org/type">
        ///   <Name>Test</Name>
        /// </ReferenceProperty>
        ///</NotTheSameName>
        ///</ArrayOfNotTheSameName>
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_arraytype_as_parent_with_xml_attributes_defined()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined[]>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });

            var schemaData = schema.ToJson();
            var schemaObject = JObject.Parse(schemaData);

            //Assert
            var propertyXml = schemaObject["xml"];
            Assert.Equal("ArrayOfNotTheSameName", propertyXml["name"]);
            Assert.True(propertyXml["wrapped"].Value<bool>());

            var itemsXml = schemaObject["items"]["xml"];
            Assert.Equal("NotTheSameName", itemsXml["name"]);

            var definitionSchema = schema.Definitions["WithXmlAttributesDefined"];

            AssertTypeWithXmlAttributes(schema, definitionSchema);
        }

        private void AssertTypeWithXmlAttributes(JsonSchema4 schema, JsonSchema4 rootDefinitionSchema)
        {
            Assert.Equal("NotTheSameName", rootDefinitionSchema.Xml.Name);
            Assert.Equal("http://test.shema.org/type", rootDefinitionSchema.Xml.Namespace);

            var stringArrayProperty = rootDefinitionSchema.Properties[StringArray];
            var intArrayProperty = rootDefinitionSchema.Properties["TheInts"];
            var fooProperty = rootDefinitionSchema.Properties["Foo"];
            var externalItemsProperty = rootDefinitionSchema.Properties["InternalItems"];
            var externalItemType = schema.Definitions["WithXmlAttributeItem"];
            var externalItems2Property = rootDefinitionSchema.Properties["ExternalItems2"];
            var externalItem2Type = schema.Definitions["WithXmlAttributeItem2"];
            var attributeProperty = rootDefinitionSchema.Properties["MightBeAAttribute"];
            var referenceProperty = rootDefinitionSchema.Properties["ReferenceProperty"];

            Assert.Equal("Bar", fooProperty.Xml.Name);

            Assert.Equal("TheStrings", stringArrayProperty.Xml.Name);
            Assert.Null(intArrayProperty.Xml.Name);
            // https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.True(stringArrayProperty.Xml.Wrapped);

            Assert.Equal("TheString", stringArrayProperty.Item.Xml.Name);

            Assert.True(attributeProperty.Xml.Attribute);

            Assert.True(externalItemsProperty.Xml.Wrapped);
            Assert.Equal("ExternalItems", externalItemsProperty.Xml.Name);
            Assert.Equal("ExternalItem", externalItemType.Xml.Name);

            Assert.Null(externalItems2Property.Xml.Name);
            Assert.True(externalItems2Property.Xml.Wrapped);
            Assert.Equal("ExternalItem2", externalItem2Type.Xml.Name);

            //https://github.com/swagger-api/swagger-ui/issues/2610
            Assert.NotNull(referenceProperty.Xml); // Make sure that type reference properties have an xml object
            Assert.Equal(referenceProperty.Name, referenceProperty.Xml.Name); // Make sure that the property name and the xml name is the same
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });
            
            var schemaData = schema.ToJson();
            var schemaObject = JObject.Parse(schemaData);

            var definitionXml = schemaObject["xml"];
            Assert.Equal("NotTheSameName", definitionXml["name"]);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.Equal("Bar", fooPropertyXml["name"]);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.Equal("TheStrings", arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.Equal("TheString", arrayStringPropertyItemXml["name"]);

            var referencePropertyXml = schemaObject["properties"]["ReferenceProperty"]["xml"];
            Assert.Equal("ReferenceProperty", referencePropertyXml["name"]);
        }

        public class WithXmlIncorrectAttributesDefined
        {
            [XmlArray(ElementName = "TheStrings", Namespace = "http://test.shema.org/type")]
            [XmlArrayItem(ElementName = "TheString", Namespace = "http://test.shema.org/type")]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_that_are_incorrect()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlIncorrectAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });

            //// Assert
            var fooProperty = schema.Properties["Foo"];

            Assert.Null(fooProperty.Xml);
        }

        [Fact]
        public void When_model_objects_are_created_with_the_example_model_make_sure_that_they_are_serializable()
        {
            WithXmlAttributesDefined.CreateTestXml();
            WithXmlAttributesDefined.CreateTestArrayXml();
            WithoutXmlAttributesDefined.CreateTestXml();
            WithoutXmlAttributesDefined.CreateTestArrayXml();
        }

        private static string GetSerializedContent<T>(T data)
        {
            var types = new List<System.Type>
            {
                typeof(T)
            };
            var serializer = XmlSerializer.FromTypes(types.ToArray()).First();

            var sio = new System.IO.StringWriter();
            serializer.Serialize(sio, data);
            return sio.ToString();
        }
    }
}
