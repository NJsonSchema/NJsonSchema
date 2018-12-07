//-----------------------------------------------------------------------
// <copyright file="DefaultTypeNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema
{
    /// <summary>Converts the last part of the full type name to upper case.</summary>
    public class DefaultTypeNameGenerator : ITypeNameGenerator
    {
        // TODO: Expose as options to UI and cmd line?

        /// <summary>Gets or sets the reserved names.</summary>
        public IEnumerable<string> ReservedTypeNames { get; set; } = new List<string> { "object" };

        /// <summary>Gets the name mappings.</summary>
        public IDictionary<string, string> TypeNameMappings { get; } = new Dictionary<string, string>();

        /// <summary>Generates the type name for the given schema respecting the reserved type names.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="reservedTypeNames">The reserved type names.</param>
        /// <returns>The type name.</returns>
        public virtual string Generate(JsonSchema4 schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (string.IsNullOrEmpty(typeNameHint) && !string.IsNullOrEmpty(schema.DocumentPath))
                typeNameHint = schema.DocumentPath.Replace("\\", "/").Split('/').Last();

            typeNameHint = (typeNameHint ?? "")
                .Replace("[", " Of ")
                .Replace("]", " ")
                .Replace("<", " Of ")
                .Replace(">", " ")
                .Replace(",", " And ")
                .Replace("  ", " ");

            var parts = typeNameHint.Split(' ');
            typeNameHint = string.Join(string.Empty, parts.Select(p => Generate(schema, p)));

            var typeName = Generate(schema, typeNameHint);
            if (string.IsNullOrEmpty(typeName) || reservedTypeNames.Contains(typeName))
                typeName = GenerateAnonymousTypeName(typeNameHint, reservedTypeNames);

            return typeName;
        }

        /// <summary>Generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        protected virtual string Generate(JsonSchema4 schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(typeNameHint) &&
                string.IsNullOrEmpty(schema.Title) == false &&
                Regex.IsMatch(schema.Title, "^[a-zA-Z0-9_]*$"))
            {
                typeNameHint = schema.Title;
            }

            var lastSegment = typeNameHint?.Split('.').Last();
            return ConversionUtilities.ConvertToUpperCamelCase(lastSegment ?? "Anonymous", true);
        }

        private string GenerateAnonymousTypeName(string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (!string.IsNullOrEmpty(typeNameHint))
            {
                if (TypeNameMappings.ContainsKey(typeNameHint))
                    typeNameHint = TypeNameMappings[typeNameHint];

                typeNameHint = typeNameHint.Split('.').Last();

                if (!reservedTypeNames.Contains(typeNameHint) && !ReservedTypeNames.Contains(typeNameHint))
                    return typeNameHint;

                var count = 1;
                do
                {
                    count++;
                } while (reservedTypeNames.Contains(typeNameHint + count));

                return typeNameHint + count;
            }

            return GenerateAnonymousTypeName("Anonymous", reservedTypeNames);
        }
    }
}