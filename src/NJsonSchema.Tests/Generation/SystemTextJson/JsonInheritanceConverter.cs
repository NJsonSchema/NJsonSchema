#if !NET461

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    /// <summary>Defines the class as inheritance base class and adds a discriminator property to the serialized object.</summary>
    public class JsonInheritanceConverter<TBase> : JsonConverter<TBase>
    {
        /// <summary>Gets the list of additional known types.</summary>
        public static IDictionary<string, Type> AdditionalKnownTypes { get; } = new Dictionary<string, Type>();

        private readonly string _discriminatorName;

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{TBase}"/> class.</summary>
        public JsonInheritanceConverter()
        {
            var attribute = typeof(TBase).GetCustomAttribute<JsonInheritanceConverterAttribute>();
            _discriminatorName = attribute?.DiscriminatorName ?? "discriminator";
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{TBase}"/> class.</summary>
        /// <param name="discriminatorName">The discriminator name.</param>
        public JsonInheritanceConverter(string discriminatorName)
        {
            _discriminatorName = discriminatorName;
        }

        /// <summary>Gets the discriminator property name.</summary>
        public virtual string DiscriminatorName => _discriminatorName;

        /// <inheritdoc />
        public override TBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var hasDiscriminator = document.RootElement.TryGetProperty(_discriminatorName, out var discriminator);
            var subtype = GetDiscriminatorType(document.RootElement, typeToConvert, hasDiscriminator ? discriminator.GetString() : null);

            var bufferWriter = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                document.RootElement.WriteTo(writer);
            }

            return (TBase)JsonSerializer.Deserialize(bufferWriter.ToArray(), subtype, options);

            //var bufferWriter = new ArrayBufferWriter<byte>();
            //using (var writer = new Utf8JsonWriter(bufferWriter))
            //{
            //    document.RootElement.WriteTo(writer);
            //}

            //return (TBase)JsonSerializer.Deserialize(bufferWriter.WrittenSpan, subtype, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(_discriminatorName, GetDiscriminatorValue(value.GetType()));

            var bytes = JsonSerializer.SerializeToUtf8Bytes((object)value, options);
            var document = JsonDocument.Parse(bytes);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the discriminator value for the given type.</summary>
        /// <param name="type">The object type.</param>
        /// <returns>The discriminator value.</returns>
        public virtual string GetDiscriminatorValue(Type type)
        {
            var knownType = AdditionalKnownTypes.SingleOrDefault(p => p.Value == type);
            if (knownType.Key != null)
            {
                return knownType.Key;
            }

            var jsonInheritanceAttributeDiscriminator = GetSubtypeDiscriminator(type);
            if (jsonInheritanceAttributeDiscriminator != null)
            {
                return jsonInheritanceAttributeDiscriminator;
            }

            return type.Name;
        }

        /// <summary>Gets the type for the given discriminator value.</summary>
        /// <param name="jObject">The JSON object.</param>
        /// <param name="objectType">The object (base) type.</param>
        /// <param name="discriminatorValue">The discriminator value.</param>
        /// <returns></returns>
        protected virtual Type GetDiscriminatorType(JsonElement jObject, Type objectType, string discriminatorValue)
        {
            if (AdditionalKnownTypes.ContainsKey(discriminatorValue))
            {
                return AdditionalKnownTypes[discriminatorValue];
            }

            var jsonInheritanceAttributeSubtype = GetObjectSubtype(objectType, discriminatorValue);
            if (jsonInheritanceAttributeSubtype != null)
            {
                return jsonInheritanceAttributeSubtype;
            }

            if (objectType.Name == discriminatorValue)
            {
                return objectType;
            }

            var knownTypeAttributesSubtype = GetSubtypeFromKnownTypeAttributes(objectType, discriminatorValue);
            if (knownTypeAttributesSubtype != null)
            {
                return knownTypeAttributesSubtype;
            }

            var typeName = objectType.Namespace + "." + discriminatorValue;
            var subtype = objectType.GetTypeInfo().Assembly.GetType(typeName);
            if (subtype != null)
            {
                return subtype;
            }

            throw new InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminatorValue + "'.");
        }

        private static Type GetSubtypeFromKnownTypeAttributes(Type objectType, string discriminatorValue)
        {
            var type = objectType;
            do
            {
                var knownTypeAttributes = type
                    .GetTypeInfo()
                    .GetCustomAttributes(false)
                    .Where(a => a.GetType().Name == "KnownTypeAttribute");

                foreach (dynamic attribute in knownTypeAttributes)
                {
                    if (attribute.Type != null && attribute.Type.Name == discriminatorValue)
                    {
                        return attribute.Type;
                    }
                    else if (attribute.MethodName != null)
                    {
                        var method = type.GetRuntimeMethod((string)attribute.MethodName, new Type[0]);
                        if (method != null)
                        {
                            var types = (IEnumerable<Type>)method.Invoke(null, new object[0]);
                            foreach (var knownType in types)
                            {
                                if (knownType.Name == discriminatorValue)
                                {
                                    return knownType;
                                }
                            }
                            return null;
                        }
                    }
                }

                type = type.GetTypeInfo().BaseType;
            } while (type != null);

            return null;
        }

        private static Type GetObjectSubtype(Type baseType, string discriminatorName)
        {
            var jsonInheritanceAttributes = baseType
                .GetTypeInfo()
                .GetCustomAttributes(true)
                .OfType<JsonInheritanceAttribute>();

            return jsonInheritanceAttributes.SingleOrDefault(a => a.Key == discriminatorName)?.Type;
        }

        private static string GetSubtypeDiscriminator(Type objectType)
        {
            var jsonInheritanceAttributes = objectType
                .GetTypeInfo()
                .GetCustomAttributes(true)
                .OfType<JsonInheritanceAttribute>();

            return jsonInheritanceAttributes.SingleOrDefault(a => a.Type == objectType)?.Key;
        }
    }
}

#endif