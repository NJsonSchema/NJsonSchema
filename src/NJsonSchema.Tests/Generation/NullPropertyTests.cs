using System.Threading.Tasks;
using NJsonSchema.Annotations;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class NullPropertyTests
    {
        public class ClassRoom
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int? Size { get; set; }
        }

        [Fact]
        public async Task When_property_is_nullable_then_property_schema_type_is_also_null()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassRoom>();
            
            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.False(schema.Properties["Id"].IsRequired);
            Assert.False(schema.Properties["Name"].IsRequired);
            Assert.False(schema.Properties["Size"].IsRequired);

            Assert.False(schema.Properties["Id"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["Name"].Type.HasFlag(JsonObjectType.Null));
            Assert.True(schema.Properties["Size"].Type.HasFlag(JsonObjectType.Null));
        }

        public class NotNullAttributeClass
        {
            public string Foo { get; set; }

            [NotNull]
            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_NotNullAttribute_is_available_then_property_is_not_nullable()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<NotNullAttributeClass>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Foo"].IsNullable(SchemaType.JsonSchema));
            Assert.False(schema.Properties["Bar"].IsNullable(SchemaType.JsonSchema));
        }
    }
}