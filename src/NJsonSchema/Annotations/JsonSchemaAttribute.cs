//-----------------------------------------------------------------------
// <copyright file="JsonSchemaAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to specify the JSON Schema type for the given class.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Struct)]
    public class JsonSchemaAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute"/> class.</summary>
        public JsonSchemaAttribute()
        {
            Type = JsonObjectType.None;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute" /> class.</summary>
        /// <param name="name">The identifier of the schema which is used as key in the 'definitions' list.</param>
        public JsonSchemaAttribute(string name) : this()
        {
            Name = name;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute"/> class.</summary>
        /// <param name="type">The JSON Schema type.</param>
        public JsonSchemaAttribute(JsonObjectType type)
        {
            Type = type;
        }

        /// <summary>Gets or sets the name identifier of the schema which is used as key in the 'definitions' list.</summary>
        public string Name { get; set; }

        /// <summary>Gets the JSON Schema type (default: <see cref="JsonObjectType.None"/>, i.e. derived from <see cref="System.Type"/>).</summary>
        public JsonObjectType Type { get; private set; }

        /// <summary>Gets or sets the JSON format type (default: <c>null</c>, i.e. derived from <see cref="System.Type"/>).</summary>
        public string Format { get; set; }

        /// <summary>Gets or sets the array item type.</summary>
        public Type ArrayItem { get; set; }
    }
}