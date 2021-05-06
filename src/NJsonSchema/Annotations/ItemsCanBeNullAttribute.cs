//-----------------------------------------------------------------------
// <copyright file="ItemsCanBeNullAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to specify that array items or dictionary values are nullable.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue |
                    AttributeTargets.Field)]
    public class ItemsCanBeNullAttribute : Attribute
    {
    }
}