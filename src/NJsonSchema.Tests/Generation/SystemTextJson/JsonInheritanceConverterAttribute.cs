#if !NET461

using System;
using System.Text.Json.Serialization;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    /// <summary>Defines the class as inheritance base class and adds a discriminator property to the serialized object.</summary>
    public class JsonInheritanceConverterAttribute : JsonConverterAttribute
    {
        /// <summary>Gets the discriminator property name.</summary>
        public string DiscriminatorName { get; }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverterAttribute"/> class.</summary>
        /// <param name="baseType"></param>
        /// <param name="discriminatorName"></param>
        public JsonInheritanceConverterAttribute(Type baseType, string discriminatorName = "discriminator")
            : base(typeof(JsonInheritanceConverter<>).MakeGenericType(baseType))
        {
            DiscriminatorName = discriminatorName;
        }
    }
}

#endif