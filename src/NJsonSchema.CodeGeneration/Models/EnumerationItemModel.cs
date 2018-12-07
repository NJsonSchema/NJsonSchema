//-----------------------------------------------------------------------
// <copyright file="EnumerationItemModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>Describes an enumeration entry.</summary>
    public class EnumerationItemModel
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the value.</summary>
        public string Value { get; set; }

        /// <summary>Gets or sets the internal value (e.g. the underlying/system value).</summary>
        public string InternalValue { get; set; }

        /// <summary>Gets or sets the internal flag value (e.g. the underlying/system value).</summary>
        public string InternalFlagValue { get; set; }
    }
}