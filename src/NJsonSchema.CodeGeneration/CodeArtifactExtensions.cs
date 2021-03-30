//-----------------------------------------------------------------------
// <copyright file="CodeArtifactCollection.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Code artifact extensions.</summary>
    public static class CodeArtifactExtensions
    {
        /// <summary>Concatenates the results.</summary>
        /// <returns>The result.</returns>
        public static string Concatenate(this IEnumerable<CodeArtifact> artifacts)
        {
            return ConversionUtilities.TrimWhiteSpaces(string.Join("\n\n", artifacts.Select(p => p.Code)));
        }

        /// <summary>Reorders the results so that base classes are always before child classes.</summary>
        /// <param name="results">The results.</param>
        /// <returns>The reordered results.</returns>
        public static IEnumerable<CodeArtifact> OrderByBaseDependency(this IEnumerable<CodeArtifact> results)
        {
            var newResults = new List<CodeArtifact>(results);

            // we need new list to iterate as we modify the original
            var resultIterator = results as List<CodeArtifact> ?? newResults.ToList();
            foreach (var result in resultIterator)
            {
                if (!string.IsNullOrEmpty(GetActualBaseName(result.BaseTypeName)))
                {
                    var index = newResults.IndexOf(result);
                    var baseResult = result;
                    do
                    {
                        var actualBaseName = GetActualBaseName(baseResult.BaseTypeName);
                        baseResult = null;
                        for (var baseIndex = 0; baseIndex < newResults.Count; baseIndex++)
                        {
                            var candidate = newResults[baseIndex];
                            if (candidate.TypeName == actualBaseName)
                            {
                                baseResult = candidate;
                                if (baseIndex > index)
                                {
                                    newResults.RemoveAt(baseIndex);
                                    newResults.Insert(index, candidate);
                                }
                                break;
                            }
                        }
                    } while (baseResult != null);
                }
            }
            return newResults;
        }

        private static string GetActualBaseName(string baseTypeName)
        {
            if (baseTypeName == null)
            {
                return null;
            }

            // resolve arrays
            if (baseTypeName.EndsWith("[]"))
            {
                return baseTypeName.Substring(0, baseTypeName.Length - 2);
            }

            // resolve lists
            if (baseTypeName.IndexOf('<') > -1)
            {
                return Regex.Replace(baseTypeName, ".*\\<(.*)\\>", m => m.Groups[1].Value);
            }

            return baseTypeName;
        }
    }
}