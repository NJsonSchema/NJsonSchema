//-----------------------------------------------------------------------
// <copyright file="ValueGeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Converts the default value to a language specific identifier.</summary>
    public abstract class ValueGeneratorBase
    {
        private readonly CodeGeneratorSettingsBase _settings;
        private readonly List<string> _unsupportedFormatStrings = new List<string>()
        {
            JsonFormatStrings.Date,
            JsonFormatStrings.DateTime,
            JsonFormatStrings.Time,
            JsonFormatStrings.TimeSpan,
            JsonFormatStrings.Uri,
            JsonFormatStrings.Guid,
            JsonFormatStrings.Byte,
#pragma warning disable CS0618 // Type or member is obsolete
            JsonFormatStrings.Uuid,
            JsonFormatStrings.Base64,
#pragma warning restore CS0618 // Type or member is obsolete
        };

        /// <summary>Initializes a new instance of the <see cref="ValueGeneratorBase" /> class.</summary>
        /// <param name="settings">The settings.</param>
        protected ValueGeneratorBase(CodeGeneratorSettingsBase settings)
        {
            _settings = settings;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="allowsNull">Specifies whether the default value assignment also allows null.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <param name="useSchemaDefault">if set to <c>true</c> uses the default value from the schema if available.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <returns>The code.</returns>
        public virtual string GetDefaultValue(JsonSchema schema, bool allowsNull, string targetType, string typeNameHint, bool useSchemaDefault, TypeResolverBase typeResolver)
        {
            if (schema.Default == null || !useSchemaDefault)
            {
                return null;
            }

            var actualSchema = schema is JsonSchemaProperty ? ((JsonSchemaProperty)schema).ActualTypeSchema : schema.ActualSchema;
            if (actualSchema.IsEnumeration && !actualSchema.Type.IsObject() && actualSchema.Type != JsonObjectType.None)
            {
                return GetEnumDefaultValue(schema, actualSchema, typeNameHint, typeResolver);
            }

            if (schema.Type.IsString() && _unsupportedFormatStrings.Contains(schema.Format) == false)
            {
                return GetDefaultAsStringLiteral(schema);
            }
            // TODO: Add conversion for format string, e.g. in C# DateTime.Parse()

            if (schema.Type.IsBoolean())
            {
                return schema.Default.ToString().ToLowerInvariant();
            }

            if (schema.Type.IsInteger() ||
                schema.Type.IsNumber())
            {
                return GetNumericValue(schema.Type, schema.Default, schema.Format);
            }

            return null;
        }

        /// <summary>Converts the default value to a number literal. </summary>
        /// <param name="type">The JSON type.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="format">Optional schema format</param>
        /// <returns>The number literal.</returns>
        public abstract string GetNumericValue(JsonObjectType type, object value, string format);

        /// <summary>Gets the enum default value.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="actualSchema">The actual schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <returns>The enum default value.</returns>
        protected virtual string GetEnumDefaultValue(JsonSchema schema, JsonSchema actualSchema, string typeNameHint, TypeResolverBase typeResolver)
        {
            var typeName = typeResolver.Resolve(actualSchema, false, typeNameHint);

            var index = actualSchema.Enumeration.ToList().IndexOf(schema.Default);
            var enumName = index >= 0 && actualSchema.EnumerationNames?.Count > index
                ? actualSchema.EnumerationNames.ElementAt(index)
                : schema.Default.ToString();

            return typeName.Trim('?') + "." + _settings.EnumNameGenerator.Generate(index, enumName, schema.Default, actualSchema);
        }

        /// <summary>Gets the default value as string literal.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The string literal.</returns>
        protected string GetDefaultAsStringLiteral(JsonSchema schema)
        {
            return "\"" + ConversionUtilities.ConvertToStringLiteral(schema.Default.ToString()) + "\"";
        }

        /// <summary>Converts a number to its string representation.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The string.</returns>
        protected string ConvertNumberToString(object value)
        {
            if (value is byte)
            {
                return ((byte)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is sbyte)
            {
                return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is short)
            {
                return ((short)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is ushort)
            {
                return ((ushort)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is int)
            {
                return ((int)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is uint)
            {
                return ((uint)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is long)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is ulong)
            {
                return ((ulong)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is float)
            {
                return ((float)value).ToString("r", CultureInfo.InvariantCulture);
            }

            if (value is double)
            {
                return ((double)value).ToString("r", CultureInfo.InvariantCulture);
            }

            if (value is decimal)
            {
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is string stringValue && Regex.IsMatch(stringValue, "^[0-9]+(\\.[0-9]+)?$"))
            {
                return (string)value;
            }

            return null;
        }
    }
}
