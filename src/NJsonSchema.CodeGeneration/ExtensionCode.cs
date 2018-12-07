//-----------------------------------------------------------------------
// <copyright file="ExtensionCode.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Provides access to the extension code.</summary>
    public abstract class ExtensionCode
    {
        /// <summary>Gets the code of the class extension.</summary>
        public Dictionary<string, string> ExtensionClasses { get; protected set; } = new Dictionary<string, string>();

        /// <summary>Gets or sets the imports.</summary>
        public string ImportCode { get; protected set; } = string.Empty;

        /// <summary>Gets the extension code which is inserted at the start of the generated code.</summary>
        public string TopCode { get; protected set; } = string.Empty;

        /// <summary>Gets the extension code which is appended at the end of the generated code.</summary>
        public string BottomCode { get; protected set; }

		/// <summary>Gets the body of the extension class.</summary>
		/// <param name="className">The class name.</param>
		/// <returns>The body code</returns>
		/// <exception cref="InvalidOperationException">The extension class is not defined.</exception>
		public string GetExtensionClassBody(string className)
        {
			if (!ExtensionClasses.ContainsKey(className))
				throw new InvalidOperationException("The extension class '" + className + "' is not defined.");

            var match = Regex.Match(ExtensionClasses[className], "(.*?)class (.*?){(.*)}", RegexOptions.Singleline);
            return match.Groups[3].Value;
        }
    }
}