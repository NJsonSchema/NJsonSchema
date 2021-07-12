//-----------------------------------------------------------------------
// <copyright file="ConversionUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NJsonSchema
{
    /// <summary>Provides name conversion utility methods.</summary>
    public class ConversionUtilities
    {
        /// <summary>Converts the first letter to lower case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <param name="firstCharacterMustBeAlpha">Specifies whether to add an _ when the first character is not alpha.</param>
        /// <returns>The converted input.</returns>
        public static string ConvertToLowerCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase((input[0].ToString().ToLowerInvariant() + (input.Length > 1 ? input.Substring(1) : ""))
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        /// <summary>Converts the first letter to upper case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <param name="firstCharacterMustBeAlpha">Specifies whether to add an _ when the first character is not alpha.</param>
        /// <returns>The converted input.</returns>
        public static string ConvertToUpperCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase(Capitalize(input)
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        [MethodImpl((MethodImplOptions) 256)]
        private static string Capitalize(string input)
        {
            if (char.IsUpper(input[0]))
            {
                return input;
            }
            if (input.Length == 1)
            {
                return char.ToUpperInvariant(input[0]).ToString();
            }            
            return char.ToUpperInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>Converts the string to a string literal which can be used in C# or TypeScript code.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The literal.</returns>
        public static string ConvertToStringLiteral(string input)
        {
            var literal = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }
                        break;
                }
            }
            return literal.ToString();
        }

        /// <summary>Converts the input to a camel case identifier.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The converted input. </returns>
        public static string ConvertToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return ConvertDashesToCamelCase(input.Replace(" ", "_").Replace("/", "_"));
        }


        /// <summary>Trims white spaces from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string TrimWhiteSpaces(string text)
        {
            return text?.Trim('\n', '\r', '\t', ' ');
        }

        /// <summary>Removes the line breaks from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string RemoveLineBreaks(string text)
        {
            return text?.Replace("\r", "")
                .Replace("\n", " \n")
                .Replace("\n ", "\n")
                .Replace("  \n", " \n")
                .Replace("\n", "")
                .Trim('\n', '\t', ' ');
        }

        /// <summary>Singularizes the given noun in plural.</summary>
        /// <param name="word">The plural noun.</param>
        /// <returns>The singular noun.</returns>
        public static string Singularize(string word)
        {
            if (word == "people")
            {
                return "Person";
            }

            return word.EndsWith("s") ? word.Substring(0, word.Length - 1) : word;
        }

        /// <summary>Add tabs to the given string.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <returns>The output.</returns>
        public static string Tab(string input, int tabCount)
        {
            return input?.Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount))) ?? string.Empty;
        }

        /// <summary>Converts all line breaks in a string into '\n' and removes white spaces.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <returns>The output.</returns>
        public static string ConvertCSharpDocs(string input, int tabCount)
        {
            input = input?
                .Replace("\r", string.Empty)
                .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "/// ")
                ?? string.Empty;

            // TODO: Support more markdown features here
            var xml = new XText(input).ToString();
            return Regex.Replace(xml, @"^( *)/// ", m => m.Groups[1] + "/// <br/>", RegexOptions.Multiline);
        }

        private static string ConvertDashesToCamelCase(string input)
        {
            var sb = new StringBuilder();
            var caseFlag = false;
            foreach (char c in input)
            {
                if (c == '-')
                {
                    caseFlag = true;
                }
                else if (caseFlag)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    caseFlag = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}