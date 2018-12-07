//-----------------------------------------------------------------------
// <copyright file="ITemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Interface for a template.</summary>
    public interface ITemplate
    {
        /// <summary>Renders the template.</summary>
        /// <returns>The output.</returns>
        string Render();
    }
}