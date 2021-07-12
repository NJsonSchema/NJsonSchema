//-----------------------------------------------------------------------
// <copyright file="DefaultTypeNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public virtual string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (string.IsNullOrEmpty(typeNameHint) && !string.IsNullOrEmpty(schema.DocumentPath))
            {
                typeNameHint = schema.DocumentPath.Replace("\\", "/").Split('/').Last();
            }

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
            {
                typeName = GenerateAnonymousTypeName(typeNameHint, reservedTypeNames);
            }

            return RemoveIllegalCharacters(typeName);
        }

        /// <summary>Generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        protected virtual string Generate(JsonSchema schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(typeNameHint) && schema.HasTypeNameTitle)
            {
                typeNameHint = schema.Title;
            }

            var lastSegment = typeNameHint;
            var lastDotIndex = typeNameHint?.LastIndexOf('.') ?? -1;
            if (lastDotIndex > -1)
            {
                lastSegment = typeNameHint.Substring(lastDotIndex + 1);
            }
            return ConversionUtilities.ConvertToUpperCamelCase(lastSegment ?? "Anonymous", true);
        }

        private string GenerateAnonymousTypeName(string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (!string.IsNullOrEmpty(typeNameHint))
            {
                if (TypeNameMappings.ContainsKey(typeNameHint))
                {
                    typeNameHint = TypeNameMappings[typeNameHint];
                }

                typeNameHint = typeNameHint.Split('.').Last();

                if (!reservedTypeNames.Contains(typeNameHint) && !ReservedTypeNames.Contains(typeNameHint))
                {
                    return typeNameHint;
                }

                var count = 1;
                do
                {
                    count++;
                } while (reservedTypeNames.Contains(typeNameHint + count));

                return typeNameHint + count;
            }

            return GenerateAnonymousTypeName("Anonymous", reservedTypeNames);
        }

        /// <summary>
        /// Replaces all characters that are not normals letters, numbers or underscore, with an underscore.
        /// Will prepend an underscore if the first characters is a number.
        /// In case there are this would result in multiple underscores in a row, strips down to one underscore.
        /// Will trim any underscores at the end of the type name.
        /// </summary>
        private static string RemoveIllegalCharacters(string typeName)
        {
            // TODO: Find a way to support unicode characters up to 3.0
           
            // first check if all are valid and we skip altogether
            var invalid = false;
            for (var i = 0; i < typeName.Length; i++)
            {
                var c = typeName[i];
                if (i == 0 && (!IsEnglishLetterOrUnderScore(c) || char.IsDigit(c)))
                {
                    invalid = true;
                    break;
                }

                if (!IsEnglishLetterOrUnderScore(c) && !char.IsDigit(c))
                {
                    invalid = true;
                    break;
                }
            }

            if (!invalid)
            {
                return typeName;
            }
            
            return DoRemoveIllegalCharacters(typeName);
        }

        private static string DoRemoveIllegalCharacters(string typeName)
        {
            var firstCharacter = typeName[0];
            var regexInvalidCharacters = new Regex("\\W");

            var legalTypeName = new StringBuilder(typeName);
            if (!IsEnglishLetterOrUnderScore(firstCharacter) || firstCharacter == '_')
            {
                if (!regexInvalidCharacters.IsMatch(firstCharacter.ToString()))
                {
                    legalTypeName.Insert(0, "_");
                }
                else
                {
                    legalTypeName[0] = '_';
                }
            }

            var illegalMatches = regexInvalidCharacters.Matches(legalTypeName.ToString());

            for (int i = illegalMatches.Count - 1; i >= 0; i--)
            {
                var illegalMatchIndex = illegalMatches[i].Index;
                legalTypeName[illegalMatchIndex] = '_';
            }

            var regexMoreThanOneUnderscore = new Regex("[_]{2,}");

            var legalTypeNameString = regexMoreThanOneUnderscore.Replace(legalTypeName.ToString(), "_");
            return legalTypeNameString.TrimEnd('_');
        }
        
        private static bool IsEnglishLetterOrUnderScore(char c)
        {
            return (c>='A' && c<='Z') || (c>='a' && c<='z') || c == '_';
        }
    }
}