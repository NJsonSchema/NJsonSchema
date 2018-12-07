using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class FlattenInheritanceHierarchyTests
    {
        public class Person
        {
            public string Name { get; set; }
        }

        public class Teacher : Person
        {
            public string Class { get; set; }
        }

        [Fact]
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync(typeof(Teacher), settings);
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("Name"));
            Assert.True(schema.Properties.ContainsKey("Class"));
        }

        public interface IFoo : IBar, IBaz
        {
            string Foo { get; set; }
        }

        public interface IBar
        {
            string Bar { get; set; }
        }

        public interface IBaz
        {
            string Baz { get; set; }
        }

        public interface ISame
        {
            string Bar { get; set; }
        }

        [Fact]
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_interface_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String,
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<IFoo>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(3, schema.Properties.Count);
            Assert.True(schema.Properties.ContainsKey("Foo"));
            Assert.True(schema.Properties.ContainsKey("Bar"));
            Assert.True(schema.Properties.ContainsKey("Baz"));
        }

        public class Test
        {
            public int? Id { get; set; }

            public Metadata Info { get; set; }
        }

        public class Metadata : Dictionary<string, string> { }

        [Fact]
        public async Task When_class_inherits_from_dictionary_then_flatten_inheritance_and_generate_abstract_properties_works()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Test>(settings);
            var data = schema.ToJson();

            //// Assert
        }

        [Fact]
        public async Task When_class_inherits_from_dictionary_then_flatten_inheritance_works()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Test>(settings);
            var data = schema.ToJson();

            //// Assert
        }

        public class A : B
        {
            public string Aaa { get; set; }
        }

        [JsonSchemaFlatten]
        public class B : C
        {
            public string Bbb { get; set; }
        }

        public class C
        {
            public string Ccc { get; set; }
        }

        [Fact]
        public async Task When_JsonSchemaFlattenAttribute_is_used_on_class_then_inherited_classed_are_merged()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings();

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<A>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Definitions.ContainsKey("B"));
            Assert.False(schema.Definitions.ContainsKey("C"));

            Assert.True(schema.ActualProperties.ContainsKey("Aaa"));
            Assert.True(schema.Definitions["B"].Properties.ContainsKey("Bbb"));
            Assert.True(schema.Definitions["B"].Properties.ContainsKey("Ccc"));
        }
    }
}
