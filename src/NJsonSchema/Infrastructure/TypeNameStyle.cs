//-----------------------------------------------------------------------
// <copyright file="TypeNameStyle.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Infrastructure
{
    /// <summary>The type name style.</summary>
    public enum TypeNameStyle
    {
        /// <summary>Only the name of the type.</summary>
        Name,

        /// <summary>The full name of the type including the namespace.</summary>
        FullName
    }
}