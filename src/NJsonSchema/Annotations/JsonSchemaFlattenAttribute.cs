//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDateAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to merge all inherited properties into this class/schema.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class JsonSchemaFlattenAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaFlattenAttribute"/> class.</summary>
        public JsonSchemaFlattenAttribute()
        {
            Flatten = true;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAbstractAttribute"/> class.</summary>
        /// <param name="flatten">The explicit flag to override the global setting (i.e. disable the generation for a type).</param>
        public JsonSchemaFlattenAttribute(bool flatten)
        {
            Flatten = flatten;
        }

        /// <summary>Gets or sets a value indicating whether to flatten the given type.</summary>
        public bool Flatten { get; }
    }
}