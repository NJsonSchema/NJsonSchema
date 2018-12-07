//-----------------------------------------------------------------------
// <copyright file="EnumHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Defines the enum handling.</summary>
    public enum EnumHandling
    {
        /// <summary>Generates an integer field without enumeration (except when using StringEnumConverter).</summary>
        Integer,

        /// <summary>Generates a string field with JSON Schema enumeration.</summary>
        String,

        /// <summary>Generates a camel-cased string field with JSON Schema enumeration.</summary>
        CamelCaseString,
    }
}