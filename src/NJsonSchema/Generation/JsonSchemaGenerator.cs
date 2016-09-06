//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using NJsonSchema.Converters;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a <see cref="JsonSchema4"/> object for a given type. </summary>
    public class JsonSchemaGenerator
    {
        private static readonly Dictionary<string, string> DataTypeFormats = new Dictionary<string, string>
        {
            {"DateTime", JsonFormatStrings.DateTime},
            {"Date", JsonFormatStrings.Date},
            {"Time", JsonFormatStrings.Time},
            {"EmailAddress", JsonFormatStrings.Email},
            {"PhoneNumber", JsonFormatStrings.Phone},
            {"Url", JsonFormatStrings.Uri}
        };

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.</summary>
        /// <param name="settings">The settings.</param>
        public JsonSchemaGenerator(JsonSchemaGeneratorSettings settings)
        {
            Settings = settings;
        }

        /// <summary>Gets the settings.</summary>
        public JsonSchemaGeneratorSettings Settings { get; }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema4 Generate(Type type)
        {
            return Generate<JsonSchema4>(type, null, new SchemaResolver(), new JsonSchemaDefinitionAppender(null, null));
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver"></param>
        /// <param name="schemaDefinitionAppender"></param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema4 Generate(Type type, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            return Generate<JsonSchema4>(type, null, schemaResolver, schemaDefinitionAppender);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent property or parameter attributes.</param>
        /// <param name="schemaResolver"></param>
        /// <param name="schemaDefinitionAppender"></param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public virtual TSchemaType Generate<TSchemaType>(Type type, IEnumerable<Attribute> parentAttributes,
            ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
            where TSchemaType : JsonSchema4, new()
        {
            var schema = HandleSpecialTypes<TSchemaType>(type, schemaResolver);
            if (schema != null)
                return schema;

            schema = new TSchemaType();

            if (schemaDefinitionAppender.RootObject == null)
                schemaDefinitionAppender.RootObject = schema;

            ApplyExtensionDataAttributes(schema, type, parentAttributes);

            var typeDescription = JsonObjectTypeDescription.FromType(type, parentAttributes, Settings.DefaultEnumHandling);
            if (typeDescription.Type.HasFlag(JsonObjectType.Object))
            {
                if (typeDescription.IsDictionary)
                {
                    typeDescription.ApplyType(schema);
                    GenerateDictionary(type, schema, schemaResolver, schemaDefinitionAppender);
                }
                else
                {
                    if (schemaResolver.HasSchema(type, false))
                    {
                        schema.SchemaReference = schemaResolver.GetSchema(type, false);
                        return schema;
                    }

                    if (schema.GetType() == typeof(JsonSchema4))
                    {
                        typeDescription.ApplyType(schema);

                        schema.TypeNameRaw = ReflectionExtensions.GetTypeName(type);
                        schema.Description = GetDescription(type.GetTypeInfo(), type.GetTypeInfo().GetCustomAttributes());

                        GenerateObject(type, schema, schemaResolver, schemaDefinitionAppender);
                    }
                    else
                    {
                        schema.SchemaReference = Generate<JsonSchema4>(type, parentAttributes, schemaResolver, schemaDefinitionAppender);
                        return schema;
                    }
                }
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                var isIntegerEnumeration = typeDescription.Type == JsonObjectType.Integer;
                if (schemaResolver.HasSchema(type, isIntegerEnumeration))
                {
                    schema.SchemaReference = schemaResolver.GetSchema(type, isIntegerEnumeration);
                    return schema;
                }

                if (schema.GetType() == typeof(JsonSchema4))
                {
                    LoadEnumerations(type, schema, typeDescription);

                    typeDescription.ApplyType(schema);

                    schema.TypeNameRaw = ReflectionExtensions.GetTypeName(type);
                    schema.Description = type.GetXmlDocumentation();

                    schemaResolver.AddSchema(type, isIntegerEnumeration, schema);
                }
                else
                {
                    schema.SchemaReference = Generate<JsonSchema4>(type, parentAttributes, schemaResolver, schemaDefinitionAppender);
                    return schema;
                }
            }
            else if (typeDescription.Type.HasFlag(JsonObjectType.Array))
            {
                typeDescription.ApplyType(schema);

                var genericTypeArguments = ReflectionExtensions.GetGenericTypeArguments(type);
                var itemType = genericTypeArguments.Length == 0 ? type.GetElementType() : genericTypeArguments[0];
                if (itemType == null)
                {
                    var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
                    if (jsonSchemaAttribute?.ArrayItem != null)
                        schema.Item = Generate(jsonSchemaAttribute.ArrayItem, schemaResolver, schemaDefinitionAppender);
                    else
                        schema.Item = JsonSchema4.CreateAnySchema();
                }
                else
                    schema.Item = Generate(itemType, schemaResolver, schemaDefinitionAppender);
            }
            else
                typeDescription.ApplyType(schema);

            return schema;
        }

        private void ApplyExtensionDataAttributes<TSchemaType>(TSchemaType schema, Type type, IEnumerable<Attribute> parentAttributes)
            where TSchemaType : JsonSchema4, new()
        {
            if (parentAttributes == null)
            {
                // class
                var extensionDataAttributes = type.GetTypeInfo().GetCustomAttributes<JsonSchemaExtensionDataAttribute>().ToArray();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
            else
            {
                // property or parameter
                var extensionDataAttributes = parentAttributes.OfType<JsonSchemaExtensionDataAttribute>().ToArray();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
        }

        private TSchemaType HandleSpecialTypes<TSchemaType>(Type type, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == type);
            if (typeMapper != null)
            {
                var schema = typeMapper.GetSchema<TSchemaType>(this, schemaResolver);
                if (schema != null)
                    return schema;
            }

            if (type == typeof(JObject) || type == typeof(JToken) || type == typeof(object))
                return JsonSchema4.CreateAnySchema<TSchemaType>();

            return null;
        }

        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        private void GenerateDictionary<TSchemaType>(Type type, TSchemaType schema, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
            where TSchemaType : JsonSchema4, new()
        {
            var genericTypeArguments = ReflectionExtensions.GetGenericTypeArguments(type);

            var valueType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object);
            if (valueType == typeof(object))
                schema.AdditionalPropertiesSchema = JsonSchema4.CreateAnySchema();
            else
                schema.AdditionalPropertiesSchema = Generate(valueType, schemaResolver, schemaDefinitionAppender);

            schema.AllowAdditionalProperties = true;
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="type">The types.</param>
        /// <param name="schema">The properties</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="schemaDefinitionAppender"></param>
        protected virtual void GenerateObject<TSchemaType>(Type type, TSchemaType schema, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
            where TSchemaType : JsonSchema4, new()
        {
            schemaResolver.AddSchema(type, false, schema);
            schema.AllowAdditionalProperties = false;

            GeneratePropertiesAndInheritance(type, schema, schemaResolver, schemaDefinitionAppender);
            if (Settings.GenerateKnownTypes)
                GenerateKnownTypes(type, schemaResolver, schemaDefinitionAppender);
        }

        private void GeneratePropertiesAndInheritance(Type type, JsonSchema4 schema, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            var properties = GetTypeProperties(type);

#if !LEGACY
            var declaredFields = type.GetTypeInfo().DeclaredFields
                .Where(f => f.IsPublic);

            var declaredProperties = type.GetTypeInfo().DeclaredProperties
                .Where(p => p.GetMethod?.IsPublic == true || p.SetMethod?.IsPublic == true);
#else
            var declaredFields = type.GetTypeInfo()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            var declaredProperties = type.GetTypeInfo()
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetGetMethod()?.IsPublic == true || p.GetSetMethod()?.IsPublic == true);
#endif

            foreach (var property in declaredProperties.Where(p => properties == null || properties.Contains(p.Name)))
                LoadPropertyOrField(property, property.PropertyType, type, schema, schemaResolver, schemaDefinitionAppender);

            foreach (var field in declaredFields.Where(p => properties == null || properties.Contains(p.Name)))
                LoadPropertyOrField(field, field.FieldType, type, schema, schemaResolver, schemaDefinitionAppender);

            GenerateInheritance(type, schema, schemaResolver, schemaDefinitionAppender);
        }

        private void GenerateKnownTypes(Type type, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            foreach (dynamic knownTypeAttribute in type.GetTypeInfo().GetCustomAttributes().Where(a => a.GetType().Name == "KnownTypeAttribute"))
            {
                var typeDescription = JsonObjectTypeDescription.FromType(knownTypeAttribute.Type, null, Settings.DefaultEnumHandling);
                var isIntegerEnum = typeDescription.Type == JsonObjectType.Integer;

                if (!schemaResolver.HasSchema(knownTypeAttribute.Type, isIntegerEnum))
                {
                    var knownSchema = Generate(knownTypeAttribute.Type, schemaResolver, schemaDefinitionAppender);
                    schemaDefinitionAppender.Append(knownSchema.ActualSchema);
                }
            }
        }

        private void GenerateInheritance(Type type, JsonSchema4 schema, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            GenerateInheritanceDiscriminator(type, schema);

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                if (Settings.FlattenInheritanceHierarchy)
                    GeneratePropertiesAndInheritance(baseType, schema, schemaResolver, schemaDefinitionAppender);
                else
                {
                    var baseSchema = Generate(baseType, schemaResolver, schemaDefinitionAppender);
                    schema.AllOf.Add(baseSchema);
                }
            }
        }

        private void GenerateInheritanceDiscriminator(Type type, JsonSchema4 schema)
        {
            if (!Settings.FlattenInheritanceHierarchy)
            {
                var discriminator = TryGetInheritanceDiscriminator(type.GetTypeInfo().GetCustomAttributes(false).OfType<Attribute>());
                if (!string.IsNullOrEmpty(discriminator))
                {
                    if (schema.Properties.ContainsKey(discriminator))
                        throw new InvalidOperationException("The JSON property '" + discriminator + "' is defined multiple times on type '" + type.FullName + "'.");

                    schema.Discriminator = discriminator;
                    schema.Properties[discriminator] = new JsonProperty
                    {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };
                }
            }
        }

        private string TryGetInheritanceDiscriminator(IEnumerable<Attribute> typeAttributes)
        {
            dynamic jsonConverterAttribute = typeAttributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType.Name == "JsonInheritanceConverter")
                {
                    if (jsonConverterAttribute.ConverterParameters != null && jsonConverterAttribute.ConverterParameters.Length > 0)
                        return jsonConverterAttribute.ConverterParameters[0];
                    return JsonInheritanceConverter.DefaultDiscriminatorName;
                }
            }
            return null;
        }

        /// <summary>Gets the properties of the given type or null to take all properties.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The property names or null for all.</returns>
        protected virtual string[] GetTypeProperties(Type type)
        {
            if (type == typeof(Exception))
                return new[] { "InnerException", "Message", "Source", "StackTrace" };

            return null;
        }

        private void LoadEnumerations(Type type, JsonSchema4 schema, JsonObjectTypeDescription typeDescription)
        {
            schema.Type = typeDescription.Type;
            schema.Enumeration.Clear();
            schema.EnumerationNames.Clear();

            foreach (var enumName in Enum.GetNames(type))
            {
                var attributes = type.GetTypeInfo().GetDeclaredField(enumName).GetCustomAttributes(); // EnumMember only checked if StringEnumConverter is used
                if (typeDescription.Type == JsonObjectType.Integer)
                {
                    var value = Convert.ChangeType(Enum.Parse(type, enumName), Enum.GetUnderlyingType(type));
                    schema.Enumeration.Add(value);
                }
                else
                {
                    dynamic enumMemberAttribute = TryGetAttribute(attributes, "System.Runtime.Serialization.EnumMemberAttribute");
                    if (enumMemberAttribute != null && !string.IsNullOrEmpty(enumMemberAttribute.Value))
                        schema.Enumeration.Add((string)enumMemberAttribute.Value);
                    else
                        schema.Enumeration.Add(enumName);
                }

                dynamic displayAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Name))
                {
                    schema.EnumerationNames.Add(displayAttribute.Name);

                }
                else {
                    schema.EnumerationNames.Add(enumName);
                }
            }
        }

        private void LoadPropertyOrField(MemberInfo property, Type propertyType, Type parentType, JsonSchema4 parentSchema, ISchemaResolver schemaResolver, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            var attributes = property.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
            var propertyTypeDescription = JsonObjectTypeDescription.FromType(propertyType, attributes, Settings.DefaultEnumHandling);

            if (IsPropertyIgnored(parentType, attributes) == false)
            {
                JsonProperty jsonProperty;

                if (propertyType.Name == "Nullable`1")
#if !LEGACY
                    propertyType = propertyType.GenericTypeArguments[0];
#else
                    propertyType = propertyType.GetGenericArguments()[0];
#endif

                var typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == propertyType);
                var useSchemaReference =
                    typeMapper?.UseReference != false &&
                    !Settings.TypeMappers.Any(m => m is PrimitiveTypeMapper) &&
                    !propertyTypeDescription.IsDictionary &&
                    (propertyTypeDescription.Type.HasFlag(JsonObjectType.Object) || propertyTypeDescription.IsEnum);

                if (useSchemaReference)
                {
                    var propertySchema = Generate<JsonSchema4>(propertyType, attributes, schemaResolver, schemaDefinitionAppender);

                    // The schema is automatically added to Definitions if it is missing in JsonPathUtilities.GetJsonPath()
                    if (Settings.NullHandling == NullHandling.JsonSchema)
                    {
                        jsonProperty = new JsonProperty();
                        jsonProperty.OneOf.Add(new JsonSchema4
                        {
                            SchemaReference = propertySchema.ActualSchema
                        });
                    }
                    else
                    {
                        jsonProperty = new JsonProperty
                        {
                            SchemaReference = propertySchema.ActualSchema
                        };
                    }
                }
                else
                    jsonProperty = Generate<JsonProperty>(propertyType, attributes, schemaResolver, schemaDefinitionAppender);

                var propertyName = JsonPathUtilities.GetPropertyName(property, Settings.DefaultPropertyNameHandling);
                if (parentSchema.Properties.ContainsKey(propertyName))
                    throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + parentType.FullName + "'.");

                parentSchema.Properties.Add(propertyName, jsonProperty);

                var requiredAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RequiredAttribute");
                var jsonPropertyAttribute = attributes.OfType<JsonPropertyAttribute>().SingleOrDefault();

                var hasJsonNetAttributeRequired = jsonPropertyAttribute != null && (
                    jsonPropertyAttribute.Required == Required.Always ||
                    jsonPropertyAttribute.Required == Required.AllowNull);

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || hasJsonNetAttributeRequired)
                    parentSchema.RequiredProperties.Add(propertyName);

                var isJsonNetAttributeNullable = jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.AllowNull;

                var isNullable = !hasRequiredAttribute && (propertyTypeDescription.IsNullable || isJsonNetAttributeNullable);
                if (isNullable)
                {
                    if (Settings.NullHandling == NullHandling.JsonSchema)
                    {
                        if (useSchemaReference)
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.Null });
                        else if (jsonProperty.Type == JsonObjectType.None)
                        {
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.None });
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.Null });
                        }
                        else
                            jsonProperty.Type = jsonProperty.Type | JsonObjectType.Null;
                    }
                }
                else if (Settings.NullHandling == NullHandling.Swagger)
                {
                    if (!parentSchema.RequiredProperties.Contains(propertyName))
                        parentSchema.RequiredProperties.Add(propertyName);
                }

                dynamic readOnlyAttribute = TryGetAttribute(attributes, "System.ComponentModel.ReadOnlyAttribute");
                if (readOnlyAttribute != null)
                    jsonProperty.IsReadOnly = readOnlyAttribute.IsReadOnly;

                jsonProperty.Description = GetDescription(property, attributes);

                ApplyPropertyAnnotations(jsonProperty, parentType, attributes, propertyTypeDescription);
            }
        }

        private static bool IsPropertyIgnored(Type parentType, Attribute[] propertyAttributes)
        {
            if (propertyAttributes.Any(a => a is JsonIgnoreAttribute))
                return true;

            var hasDataContractAttribute = parentType.GetTypeInfo().GetCustomAttributes().Any(a => a.GetType().Name == "DataContractAttribute");
            if (hasDataContractAttribute && propertyAttributes.All(a => a.GetType().Name != "DataMemberAttribute") && !propertyAttributes.Any(a => a is JsonPropertyAttribute))
                return true;

            return false;
        }

        /// <summary>Applies the property annotations to the JSON property.</summary>
        /// <param name="jsonProperty">The JSON property.</param>
        /// <param name="parentType">The type of the parent.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="propertyTypeDescription">The property type description.</param>
        public void ApplyPropertyAnnotations(JsonSchema4 jsonProperty, Type parentType, IList<Attribute> attributes,
            JsonObjectTypeDescription propertyTypeDescription)
        {
            // TODO: Refactor out

            dynamic displayAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.DisplayAttribute");
            if (displayAttribute != null && displayAttribute.Name != null)
                jsonProperty.Title = displayAttribute.Name;

            dynamic defaultValueAttribute = TryGetAttribute(attributes, "System.ComponentModel.DefaultValueAttribute");
            if (defaultValueAttribute != null && defaultValueAttribute.Value != null)
                jsonProperty.Default = ConvertDefaultValue(parentType, attributes, defaultValueAttribute);

            dynamic regexAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
                jsonProperty.Pattern = regexAttribute.Pattern;

            if (propertyTypeDescription.Type == JsonObjectType.Number ||
                propertyTypeDescription.Type == JsonObjectType.Integer)
            {
                dynamic rangeAttribute = TryGetAttribute(attributes,
                    "System.ComponentModel.DataAnnotations.RangeAttribute");
                if (rangeAttribute != null)
                {
                    if (rangeAttribute.Minimum != null)
                        jsonProperty.Minimum = rangeAttribute.Minimum;
                    if (rangeAttribute.Maximum != null)
                        jsonProperty.Maximum = rangeAttribute.Maximum;
                }

                var multipleOfAttribute = attributes.OfType<MultipleOfAttribute>().SingleOrDefault();
                if (multipleOfAttribute != null)
                    jsonProperty.MultipleOf = multipleOfAttribute.MultipleOf;
            }

            dynamic minLengthAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.MinLengthAttribute");
            if (minLengthAttribute != null && minLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MinLength = minLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MinItems = minLengthAttribute.Length;
            }

            dynamic maxLengthAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.MaxLengthAttribute");
            if (maxLengthAttribute != null && maxLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MaxLength = maxLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MaxItems = maxLengthAttribute.Length;
            }

            dynamic stringLengthAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.StringLengthAttribute");
            if (stringLengthAttribute != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                {
                    jsonProperty.MinLength = stringLengthAttribute.MinimumLength;
                    jsonProperty.MaxLength = stringLengthAttribute.MaximumLength;
                }
            }

            dynamic dataTypeAttribute = TryGetAttribute(attributes,
                "System.ComponentModel.DataAnnotations.DataTypeAttribute");
            if (dataTypeAttribute != null)
            {
                var dataType = dataTypeAttribute.DataType.ToString();
                if (DataTypeFormats.ContainsKey(dataType))
                    jsonProperty.Format = DataTypeFormats[dataType];
            }
        }

        private object ConvertDefaultValue(Type parentType, IEnumerable<Attribute> propertyAttributes, dynamic defaultValueAttribute)
        {
            if (((Type)defaultValueAttribute.Value.GetType()).GetTypeInfo().IsEnum)
            {
                if (JsonObjectTypeDescription.IsStringEnum(parentType, propertyAttributes, Settings.DefaultEnumHandling))
                    return defaultValueAttribute.Value.ToString();
                else
                    return (int)defaultValueAttribute.Value;
            }
            else
                return defaultValueAttribute.Value;
        }

        private string GetDescription(MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = TryGetAttribute(attributes, "System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && descriptionAttribute.Description != null)
                return descriptionAttribute.Description;
            else
            {
                dynamic displayAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && displayAttribute.Description != null)
                    return displayAttribute.Description;
                else
                {
                    var summary = memberInfo.GetXmlDocumentation();
                    if (summary != string.Empty)
                        return summary;
                }
            }

            return null;
        }

        private Attribute TryGetAttribute(IEnumerable<Attribute> attributes, string attributeType)
        {
            return attributes.FirstOrDefault(a => a.GetType().FullName == attributeType);
        }
    }
}