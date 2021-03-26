//-----------------------------------------------------------------------
// <copyright file="CSharpValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Converts the default value to a TypeScript identifier.</summary>
    public class CSharpValueGenerator : ValueGeneratorBase
    {
        private readonly CSharpGeneratorSettings _settings;
        private readonly List<string> _typesWithStringConstructor = new List<string>()
        {
            "System.Guid",
            "System.Uri"
        }; 

        /// <summary>Initializes a new instance of the <see cref="CSharpValueGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public CSharpValueGenerator(CSharpGeneratorSettings settings)
            : base(settings)
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
        public override string GetDefaultValue(JsonSchema schema, bool allowsNull, string targetType, string typeNameHint, bool useSchemaDefault, TypeResolverBase typeResolver)
        {
            var value = base.GetDefaultValue(schema, allowsNull, targetType, typeNameHint, useSchemaDefault, typeResolver);
            if (value == null)
            {
                if (schema.Default != null && useSchemaDefault)
                {
                    if (_typesWithStringConstructor.Contains(targetType))
                    {
                        var stringLiteral = GetDefaultAsStringLiteral(schema);
                        return $"new {targetType}({stringLiteral})";
                    }

                    if (targetType == "System.DateTime" || targetType == "System.DateTime?")
                    {
                        var stringLiteral = GetDefaultAsStringLiteral(schema);
                        return $"System.DateTime.Parse({stringLiteral})";
                    }
                }

                var isOptional = (schema as JsonSchemaProperty)?.IsRequired == false;

                schema = schema.ActualSchema;
                if (schema != null && allowsNull == false && isOptional == false)
                {
                    if (schema.Type.IsArray() ||
                        schema.Type.IsObject())
                    {
                        targetType = !string.IsNullOrEmpty(_settings.DictionaryInstanceType)
                            ? targetType.Replace(_settings.DictionaryType + "<", _settings.DictionaryInstanceType + "<")
                            : targetType;

                        targetType = !string.IsNullOrEmpty(_settings.ArrayInstanceType)
                            ? targetType.Replace(_settings.ArrayType + "<", _settings.ArrayInstanceType + "<")
                            : targetType;

                        return $"new {targetType}()";
                    }
                }
            }

            return value;
        }

        /// <summary>Converts the default value to a C# number literal. </summary>
        /// <param name="type">The JSON type.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="format">Optional schema format</param>
        /// <returns>The C# number literal.</returns>
        public override string GetNumericValue(JsonObjectType type, object value, string format)
        {
            if (value != null)
            {
                switch (format)
                {
                    case JsonFormatStrings.Byte:
                        return "(byte)" + Convert.ToByte(value).ToString(CultureInfo.InvariantCulture);
                    case JsonFormatStrings.Integer:
                        return Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);
                    case JsonFormatStrings.Long:
                        return Convert.ToInt64(value) + "L";
                    case JsonFormatStrings.Double:
                        return ConvertNumberToString(value) + "D";
                    case JsonFormatStrings.Float:
                        return ConvertNumberToString(value) + "F";
                    case JsonFormatStrings.Decimal:
                        return ConvertNumberToString(value) + "M";
                    default:
                        return type.IsInteger() ?
                            ConvertNumberToString(value) :
                            ConvertNumberToString(value) + "D";
                }
            }

            return null;
        }

        /// <summary>Gets the enum default value.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="actualSchema">The actual schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <returns>The enum default value.</returns>
        protected override string GetEnumDefaultValue(JsonSchema schema, JsonSchema actualSchema, string typeNameHint, TypeResolverBase typeResolver)
        {
            return _settings.Namespace + "." + base.GetEnumDefaultValue(schema, actualSchema, typeNameHint, typeResolver);
        }
    }
}
