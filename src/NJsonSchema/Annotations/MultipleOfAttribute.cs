//-----------------------------------------------------------------------
// <copyright file="MultipleOfAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Attribute to set the multipleOf parameter of a JSON Schema.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MultipleOfAttribute : Attribute 
    {
        /// <summary>Initializes a new instance of the <see cref="MultipleOfAttribute"/> class.</summary>
        /// <param name="multipleOf">The multipleOf value.</param>
        public MultipleOfAttribute(double multipleOf)
        {
            MultipleOf = (decimal) multipleOf;
        }

        /// <summary>Initializes a new instance of the <see cref="MultipleOfAttribute"/> class.</summary>
        /// <param name="multipleOf">The multipleOf value.</param>
        public MultipleOfAttribute(decimal multipleOf)
        {
            MultipleOf = multipleOf;
        }

        /// <summary>Gets the value whose modulo the the JSON value must be zero.</summary>
        public decimal MultipleOf { get; private set; }
    }
}
