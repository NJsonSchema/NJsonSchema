using System.Collections.Generic;
using Xunit;

namespace NJsonSchema.Tests.Schema
{
    public class JsonPathUtilitiesGetJsonPathTests
    {
        [Fact]
        public void When_object_is_in_property_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    Property1 = new { },
                    Property2 = objectToSearch
                }
            };

            //// Act
            var path = JsonPathUtilities.GetJsonPath(obj, objectToSearch);

            //// Assert
            Assert.Equal("#/Property/Property2", path);
        }

        [Fact]
        public void When_object_is_in_list_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    List = new List<object>
                    {
                        new { },
                        new { },
                        objectToSearch
                    }
                }
            };

            //// Act
            var path = JsonPathUtilities.GetJsonPath(obj, objectToSearch);

            //// Assert
            Assert.Equal("#/Property/List/2", path);
        }

        [Fact]
        public void When_object_is_in_dictionary_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    List = new Dictionary<string, object>
                    {
                        { "Test1", new { } },
                        { "Test2", new { } },
                        { "Test3", objectToSearch },
                    }
                }
            };

            //// Act
            var path = JsonPathUtilities.GetJsonPath(obj, objectToSearch);

            //// Assert
            Assert.Equal("#/Property/List/Test3", path);
        }

        [Fact]
        public void When_object_is_root_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();

            //// Act
            var path = JsonPathUtilities.GetJsonPath(objectToSearch, objectToSearch);

            //// Assert
            Assert.Equal("#", path);
        }
    }
}
