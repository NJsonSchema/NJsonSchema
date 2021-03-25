//-----------------------------------------------------------------------
// <copyright file="JsonSchemaTypeAttribute.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Specifies the type to use for JSON Schema generation.</summary>
    [AttributeUsage(
        AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property | AttributeTargets.Class,
        AllowMultiple = false)]
    public class JsonSchemaTypeAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaTypeAttribute"/> class.</summary>
        /// <param name="type">The type of the schema.</param>
        public JsonSchemaTypeAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>Gets or sets the response type.</summary>
        public Type Type { get; }

        /// <summary>Gets or sets a value indicating whether the schema can be null (default: null = unchanged).</summary>
        public bool IsNullable
        {
            get => IsNullableRaw ?? false;
            set => IsNullableRaw = value;
        }

        /// <summary>Gets the raw nullable information.</summary>
        public bool? IsNullableRaw { get; internal set; } // required because attribute properties cannot be bool?
    }
}