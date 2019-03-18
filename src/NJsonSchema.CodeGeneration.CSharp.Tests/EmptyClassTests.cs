using NJsonSchema.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class EmptyClassTests
    {
        [Fact]
        public async Task Empty_class_is_generated_when_base_class_is_inpc()
        {
            //// Arrange
            var path = GetTestDirectory() + "/EmptyClasses/EmptyClass.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Inpc });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public partial class ClassWithoutProperties", code);
            Assert.Contains("public partial class ClassWithProperties", code);
            Assert.Contains("public ClassWithoutProperties A", code);
            Assert.Contains("public ClassWithoutProperties B", code);
            Assert.Contains("public ClassWithProperties C", code);
            Assert.Contains("public ClassWithProperties D", code);
        }

        [Fact]
        public async Task Empty_class_is_generated_when_base_class_is_prism()
        {
            //// Arrange
            var path = GetTestDirectory() + "/EmptyClasses/EmptyClass.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Prism });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public partial class ClassWithoutProperties", code);
            Assert.Contains("public partial class ClassWithProperties", code);
            Assert.Contains("public ClassWithoutProperties A", code);
            Assert.Contains("public ClassWithoutProperties B", code);
            Assert.Contains("public ClassWithProperties C", code);
            Assert.Contains("public ClassWithProperties D", code);
        }

        [Fact]
        public async Task Empty_class_is_not_generated_when_base_class_is_poco()
        {
            //// Arrange
            var path = GetTestDirectory() + "/EmptyClasses/EmptyClass.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("public partial class ClassWithoutProperties", code);
            Assert.Contains("public partial class ClassWithProperties", code);
            Assert.Contains("public object A { get; set; } = new object();", code);
            Assert.Contains("public object B { get; set; } = new object();", code);
            Assert.Contains("public ClassWithProperties C { get; set; } = new ClassWithProperties();", code);
            Assert.Contains("public ClassWithProperties D { get; set; } = new ClassWithProperties();", code);
        }

        [Fact]
        public async Task Empty_class_is_not_generated_when_base_class_is_record()
        {
            //// Arrange
            var path = GetTestDirectory() + "/EmptyClasses/EmptyClass.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Record });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("public partial class ClassWithoutProperties", code);
            Assert.Contains("public partial class ClassWithProperties", code);
            Assert.Contains("public object A { get; }", code);
            Assert.Contains("public object B { get; }", code);
            Assert.Contains("public ClassWithProperties C { get; }", code);
            Assert.Contains("public ClassWithProperties D { get; }", code);
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}