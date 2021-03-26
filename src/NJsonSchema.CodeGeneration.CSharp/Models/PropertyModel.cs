//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The CSharp property template model.</summary>
    public class PropertyModel : PropertyModelBase
    {
        private readonly JsonSchemaProperty _property;
        private readonly CSharpGeneratorSettings _settings;
        private readonly CSharpTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="PropertyModel"/> class.</summary>
        /// <param name="classTemplateModel">The class template model.</param>
        /// <param name="property">The property.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(
            ClassTemplateModel classTemplateModel,
            JsonSchemaProperty property,
            CSharpTypeResolver typeResolver,
            CSharpGeneratorSettings settings)
            : base(property, classTemplateModel, typeResolver, settings)
        {
            _property = property;
            _settings = settings;
            _resolver = typeResolver;
        }

        /// <summary>Gets the name of the property.</summary>
        public string Name => _property.Name;

        /// <summary>Gets the type of the property.</summary>
        public override string Type => _resolver.Resolve(_property, _property.IsNullable(_settings.SchemaType), GetTypeNameHint());

        /// <summary>Gets a value indicating whether the property has a description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets the name of the field.</summary>
        public string FieldName => "_" + ConversionUtilities.ConvertToLowerCamelCase(PropertyName, true);

        /// <summary>Gets a value indicating whether the property is nullable.</summary>
        public override bool IsNullable => (_settings.GenerateOptionalPropertiesAsNullable && !_property.IsRequired) || base.IsNullable;

        /// <summary>Gets or sets a value indicating whether empty strings are allowed.</summary>
        public bool AllowEmptyStrings =>
            _property.ActualTypeSchema.Type.IsString() &&
            (_property.MinLength == null || _property.MinLength == 0);

        /// <summary>Gets a value indicating whether this is an array property which cannot be null.</summary>
        public bool HasSetter =>
            (_property.IsNullable(_settings.SchemaType) == false && (
                (_property.ActualTypeSchema.IsArray && _settings.GenerateImmutableArrayProperties) ||
                (_property.ActualTypeSchema.IsDictionary && _settings.GenerateImmutableDictionaryProperties)
            )) == false;

        /// <summary>Gets the json property required.</summary>
        public string JsonPropertyRequiredCode
        {
            get
            {
                if (_settings.RequiredPropertiesMustBeDefined && _property.IsRequired)
                {
                    if (!_property.IsNullable(_settings.SchemaType))
                    {
                        return "Newtonsoft.Json.Required.Always";
                    }
                    else
                    {
                        return "Newtonsoft.Json.Required.AllowNull";
                    }
                }
                else
                {
                    if (!_property.IsNullable(_settings.SchemaType))
                    {
                        return "Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore";
                    }
                    else
                    {
                        return "Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore";
                    }
                }
            }
        }

        /// <summary>Gets a value indicating whether to render a required attribute.</summary>
        public bool RenderRequiredAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations || !_property.IsRequired || _property.IsNullable(_settings.SchemaType))
                {
                    return false;
                }

                return _property.ActualTypeSchema.IsAnyType ||
                       _property.ActualTypeSchema.Type.IsObject() ||
                       _property.ActualTypeSchema.Type.IsString() ||
                       _property.ActualTypeSchema.Type.IsArray();
            }
        }

        /// <summary>Gets a value indicating whether to render a range attribute.</summary>
        public bool RenderRangeAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                {
                    return false;
                }

                if (!_property.ActualTypeSchema.Type.IsNumber() && !_property.ActualTypeSchema.Type.IsInteger())
                {
                    return false;
                }

                return _property.ActualSchema.Maximum.HasValue || _property.ActualSchema.Minimum.HasValue;
            }
        }

        /// <summary>Gets the minimum value of the range attribute.</summary>
        public string RangeMinimumValue
        {
            get
            {
                var schema = _property.ActualSchema;
                var propertyFormat = GetSchemaFormat(schema);
                var format = propertyFormat == JsonFormatStrings.Integer ? JsonFormatStrings.Integer : JsonFormatStrings.Double;
                var type = propertyFormat == JsonFormatStrings.Integer ? "int" : "double";

                var minimum = schema.Minimum;
                if (minimum.HasValue && schema.IsExclusiveMinimum)
                {
                    if (propertyFormat == JsonFormatStrings.Integer || propertyFormat == JsonFormatStrings.Long)
                    {
                        minimum++;
                    }
                    else if (schema.MultipleOf.HasValue)
                    {
                        minimum += schema.MultipleOf;
                    }
                    else
                    {
                        // TODO - add support for doubles, singles and decimals here
                    }
                }
                return minimum.HasValue
                    ? ValueGenerator.GetNumericValue(schema.Type, minimum.Value, format)
                    : type + "." + nameof(double.MinValue);
            }
        }

        /// <summary>Gets the maximum value of the range attribute.</summary>
        public string RangeMaximumValue
        {
            get
            {
                var schema = _property.ActualSchema;
                var propertyFormat = GetSchemaFormat(schema);
                var format = propertyFormat == JsonFormatStrings.Integer ? JsonFormatStrings.Integer : JsonFormatStrings.Double;
                var type = propertyFormat == JsonFormatStrings.Integer ? "int" : "double";

                var maximum = schema.Maximum;
                if (maximum.HasValue && schema.IsExclusiveMaximum)
                {
                    if (propertyFormat == JsonFormatStrings.Integer || propertyFormat == JsonFormatStrings.Long)
                    {
                        maximum--;
                    }
                    else if (schema.MultipleOf.HasValue)
                    {
                        maximum -= schema.MultipleOf;
                    }
                    else
                    {
                        // TODO - add support for doubles, singles and decimals here
                    }
                }

                return maximum.HasValue
                    ? ValueGenerator.GetNumericValue(schema.Type, maximum.Value, format)
                    : type + "." + nameof(double.MaxValue);
            }
        }

        /// <summary>Gets a value indicating whether to render a string length attribute.</summary>
        public bool RenderStringLengthAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                {
                    return false;
                }

                if (_property.IsRequired && _property.MinLength == 1 && _property.MaxLength == null)
                {
                    return false; // handled by RequiredAttribute
                }

                return _property.ActualTypeSchema.Type.IsString() &&
                       (_property.ActualSchema.MinLength.HasValue || _property.ActualSchema.MaxLength.HasValue);
            }
        }

        /// <summary>Gets the minimum value of the string length attribute.</summary>
        public int StringLengthMinimumValue => _property.ActualSchema.MinLength ?? 0;

        /// <summary>Gets the maximum value of the string length attribute.</summary>
        public string StringLengthMaximumValue => _property.ActualSchema.MaxLength.HasValue ? _property.ActualSchema.MaxLength.Value.ToString(CultureInfo.InvariantCulture) : $"int.{nameof(int.MaxValue)}";

        /// <summary>Gets a value indicating whether to render the min length attribute.</summary>
        public bool RenderMinLengthAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                {
                    return false;
                }

                return _property.ActualTypeSchema.Type.IsArray() && _property.ActualSchema.MinItems > 0;
            }
        }

        /// <summary>Gets the value of the min length attribute.</summary>
        public int MinLengthAttribute => _property.ActualSchema.MinItems;

        /// <summary>Gets a value indicating whether to render the max length attribute.</summary>
        public bool RenderMaxLengthAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                {
                    return false;
                }

                return _property.ActualTypeSchema.Type.IsArray() && _property.ActualSchema.MaxItems > 0;
            }
        }

        /// <summary>Gets the value of the max length attribute.</summary>
        public int MaxLengthAttribute => _property.ActualSchema.MaxItems;

        /// <summary>Gets a value indicating whether to render a regular expression attribute.</summary>
        public bool RenderRegularExpressionAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                {
                    return false;
                }

                return _property.ActualTypeSchema.Type.IsString() &&
                       !string.IsNullOrEmpty(_property.ActualSchema.Pattern);
            }
        }

        /// <summary>Gets the regular expression value for the regular expression attribute.</summary>
        public string RegularExpressionValue => _property.ActualSchema.Pattern?.Replace("\"", "\"\"");

        /// <summary>Gets a value indicating whether the property type is string enum.</summary>
        public bool IsStringEnum => _property.ActualTypeSchema.IsEnumeration && _property.ActualTypeSchema.Type.IsString();

        /// <summary>Gets a value indicating whether the property should be formatted like a date.</summary>
        public bool IsDate => _property.ActualSchema.Format == JsonFormatStrings.Date;

        /// <summary>Gets a value indicating whether the property is deprecated.</summary>
        public bool IsDeprecated => _property.IsDeprecated;

        /// <summary>Gets a value indicating whether the property has a deprecated message.</summary>
        public bool HasDeprecatedMessage => !string.IsNullOrEmpty(_property.DeprecatedMessage);

        /// <summary>Gets the deprecated message.</summary>
        public string DeprecatedMessage => _property.DeprecatedMessage;

        private string GetSchemaFormat(JsonSchema schema)
        {
            if (Type == "long" || Type == "long?")
            {
                return JsonFormatStrings.Long;
            }

            if (schema.Format == null)
            {
                switch (schema.Type)
                {
                    case JsonObjectType.Integer:
                        return JsonFormatStrings.Integer;

                    case JsonObjectType.Number:
                        return JsonFormatStrings.Double;
                }
            }

            return schema.Format;
        }
    }
}
