#if !NET461

using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonInheritanceTests
    {
        public class Apple : Fruit
        {
            public string Foo { get; set; }
        }

        public class Orange : Fruit
        {
            public string Bar { get; set; }
        }

        [JsonInheritance("a", typeof(Apple))]
        [JsonInheritance("o", typeof(Orange))]
        [JsonInheritanceConverter(typeof(Fruit), "k")]
        public class Fruit
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_using_JsonInheritanceAttribute_and_SystemTextJson_then_schema_is_correct()
        {
            //// Act
            var schema = JsonSchema.FromType<Fruit>();
            var data = schema.ToJson();

            //// Assert
            Assert.NotNull(data);
            Assert.Contains(@"""a"": """, data);
            Assert.Contains(@"""o"": """, data);
        }
    }
}

#endif