//-----------------------------------------------------------------------
// <copyright file="JsonTypeDescription.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Namotion.Reflection;
using NJsonSchema.Generation.TypeMappers;

namespace NJsonSchema.Generation
{
    /// <summary>Gets JSON information about a .NET type. </summary>
    public class JsonTypeDescription
    {
        private JsonTypeDescription(ContextualType type, JsonObjectType jsonType, bool isNullable)
        {
            ContextualType = type;
            Type = jsonType;
            IsNullable = isNullable;
        }

        /// <summary>Creates a description for a primitive type or object.</summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonType">The JSON type.</param>
        /// <param name="isNullable">Specifies whether the type is nullable.</param>
        /// <param name="format">The format string (may be null).</param>
        /// <returns>The description.</returns>
        public static JsonTypeDescription Create(ContextualType type, JsonObjectType jsonType, bool isNullable, string format)
        {
            return new JsonTypeDescription(type, jsonType, isNullable)
            {
                Format = format
            };
        }

        /// <summary>Creates a description for a dictionary.</summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonType">The JSON type.</param>
        /// <param name="isNullable">Specifies whether the type is nullable.</param>
        /// <returns>The description.</returns>
        public static JsonTypeDescription CreateForDictionary(ContextualType type, JsonObjectType jsonType, bool isNullable)
        {
            return new JsonTypeDescription(type, jsonType, isNullable)
            {
                IsDictionary = true
            };
        }

        /// <summary>Creates a description for an enumeration.</summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonType">The JSON type.</param>
        /// <param name="isNullable">Specifies whether the type is nullable.</param>
        /// <returns>The description.</returns>
        public static JsonTypeDescription CreateForEnumeration(ContextualType type, JsonObjectType jsonType, bool isNullable)
        {
            return new JsonTypeDescription(type, jsonType, isNullable)
            {
                IsEnum = true
            };
        }

        /// <summary>Gets the actual contextual type.</summary>
        public ContextualType ContextualType { get; }

        /// <summary>Gets the type. </summary>
        public JsonObjectType Type { get; private set; }

        /// <summary>Gets a value indicating whether the object is a generic dictionary.</summary>
        public bool IsDictionary { get; private set; }

        /// <summary>Gets a value indicating whether the type is an enum.</summary>
        public bool IsEnum { get; private set; }

        /// <summary>Gets the format string. </summary>
        public string Format { get; private set; }

        /// <summary>Gets or sets a value indicating whether the type is nullable.</summary>
        public bool IsNullable { get; set; }

        /// <summary>Gets a value indicating whether this is a complex type (i.e. object, dictionary or array).</summary>
        public bool IsComplexType => IsDictionary || Type.IsObject() || Type.IsArray();

        /// <summary>Gets a value indicating whether this is an any type (e.g. object).</summary>
        public bool IsAny => Type == JsonObjectType.None;

        /// <summary>Specifices whether the type requires a reference.</summary>
        /// <param name="typeMappers">The type mappers.</param>
        /// <returns>true or false.</returns>
        public bool RequiresSchemaReference(IEnumerable<ITypeMapper> typeMappers)
        {
            var typeMapper = typeMappers.FirstOrDefault(m => m.MappedType == ContextualType.OriginalType);
            if (typeMapper != null)
            {
                return typeMapper.UseReference;
            }

            return !IsDictionary && (Type.IsObject() || IsEnum);
        }

        /// <summary>Applies the type and format to the given schema.</summary>
        /// <param name="schema">The JSON schema.</param>
        public void ApplyType(JsonSchema schema)
        {
            schema.Type = Type;
            schema.Format = Format;
        }
    }
}