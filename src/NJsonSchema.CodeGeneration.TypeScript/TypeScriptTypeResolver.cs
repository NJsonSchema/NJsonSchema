//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Manages the generated types and converts JSON types to TypeScript types. </summary>
    public class TypeScriptTypeResolver : TypeResolverBase
    {
        private const string UnionPipe = " | ";

        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public TypeScriptTypeResolver(TypeScriptGeneratorSettings settings)
            : base(settings)
        {
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema. Returns the type name with a 'I' prefix if the feature is supported for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public string ResolveConstructorInterfaceName(JsonSchema schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, true);
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public override string Resolve(JsonSchema schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, false);
        }

        /// <summary>Gets a value indicating whether the schema supports constructor conversion.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The result.</returns>
        public bool SupportsConstructorConversion(JsonSchema schema)
        {
            return schema?.ActualSchema.ResponsibleDiscriminatorObject == null;
        }

        /// <summary>Checks whether the given schema should generate a type.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>True if the schema should generate a type.</returns>
        protected override bool IsDefinitionTypeSchema(JsonSchema schema)
        {
            if (schema.IsDictionary && !Settings.InlineNamedDictionaries)
            {
                return true;
            }

            return base.IsDefinitionTypeSchema(schema);
        }

        private string Resolve(JsonSchema schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            schema = GetResolvableSchema(schema);

            // Primitive schemas (no new type)

            if (schema.ActualTypeSchema.IsAnyType &&
                schema.InheritedSchema == null && // not in inheritance hierarchy
                schema.AllOf.Count == 0 &&
                !Types.Keys.Contains(schema) &&
                !schema.HasReference)
            {
                return "any";
            }

            var type = schema.ActualTypeSchema.Type;
            if (type == JsonObjectType.None && schema.ActualTypeSchema.IsEnumeration)
            {
                type = schema.ActualTypeSchema.Enumeration.All(v => v is int) ?
                    JsonObjectType.Integer :
                    JsonObjectType.String;
            }

            if (type.IsNumber())
            {
                return "number";
            }

            if (type.IsInteger() && !schema.ActualTypeSchema.IsEnumeration)
            {
                return ResolveInteger(schema.ActualTypeSchema, typeNameHint);
            }

            if (type.IsBoolean())
            {
                return "boolean";
            }

            if (type.IsString() && !schema.ActualTypeSchema.IsEnumeration)
            {
                return ResolveString(schema.ActualTypeSchema, typeNameHint);
            }

            if (schema.IsBinary)
            {
                return "any";
            }

            // Type generating schemas

            if (schema.ActualTypeSchema.IsEnumeration)
            {
                return GetOrGenerateTypeName(schema, typeNameHint);
            }

            if (schema.Type.IsArray())
            {
                return ResolveArrayOrTuple(schema, typeNameHint, addInterfacePrefix);
            }

            if (schema.IsDictionary)
            {
                var prefix = addInterfacePrefix &&
                    SupportsConstructorConversion(schema.AdditionalPropertiesSchema) &&
                    schema.AdditionalPropertiesSchema?.ActualSchema.Type.IsObject() == true ? "I" : "";

                var valueType = ResolveDictionaryValueType(schema, "any");
                if (valueType != "any")
                {
                    valueType = prefix + valueType;
                }

                var defaultType = "string";
                var resolvedType = ResolveDictionaryKeyType(schema, defaultType);
                if (resolvedType != defaultType)
                {
                    var keyType = Settings.TypeScriptVersion >= 2.1m ? prefix + resolvedType : defaultType;
                    if (keyType != defaultType && schema.DictionaryKey.ActualTypeSchema.IsEnumeration)
                    {
                        if (Settings.EnumStyle == TypeScriptEnumStyle.Enum)
                        {
                            return $"{{ [key in keyof typeof {keyType}]?: {valueType}; }}";
                        }
                        else if (Settings.EnumStyle == TypeScriptEnumStyle.StringLiteral)
                        {
                            return $"{{ [key in {keyType}]?: {valueType}; }}";
                        }
                        
                        throw new ArgumentOutOfRangeException(nameof(Settings.EnumStyle), Settings.EnumStyle, "Unknown enum style");
                    }

                    return $"{{ [key: {keyType}]: {valueType}; }}";
                }

                return $"{{ [key: {resolvedType}]: {valueType}; }}";
            }

            if (Settings.UseLeafType &&
                schema.DiscriminatorObject == null &&
                schema.ActualTypeSchema.DiscriminatorObject != null)
            {
                var types = schema.ActualTypeSchema.ActualDiscriminatorObject.Mapping
                    .Select(m => Resolve(
                        m.Value,
                        typeNameHint,
                        addInterfacePrefix
                    ));

                return string.Join(UnionPipe, types);
            }

            return (addInterfacePrefix && !schema.ActualTypeSchema.IsEnumeration && SupportsConstructorConversion(schema) ? "I" : "") +
                GetOrGenerateTypeName(schema, typeNameHint);
        }

        private string ResolveString(JsonSchema schema, string typeNameHint)
        {
            // TODO: Make this more generic (see DataConversionGenerator.IsDate)
            if (Settings.DateTimeType == TypeScriptDateTimeType.Date)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "Date";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "Date";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "string";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "string";
                }
            }
            else if (Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                     Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "moment.Moment";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "moment.Duration";
                }
            }
            else if (Settings.DateTimeType == TypeScriptDateTimeType.Luxon)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "DateTime";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "DateTime";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "DateTime";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "Duration";
                }
            }
            else if (Settings.DateTimeType == TypeScriptDateTimeType.DayJS)
            {
                if (schema.Format == JsonFormatStrings.Date)
                {
                    return "dayjs.Dayjs";
                }

                if (schema.Format == JsonFormatStrings.DateTime)
                {
                    return "dayjs.Dayjs";
                }

                if (schema.Format == JsonFormatStrings.Time)
                {
                    return "dayjs.Dayjs";
                }

                if (schema.Format == JsonFormatStrings.TimeSpan)
                {
                    return "dayjs.Dayjs";
                }
            }

            return "string";
        }

        private string ResolveInteger(JsonSchema schema, string typeNameHint)
        {
            return "number";
        }

        private string ResolveArrayOrTuple(JsonSchema schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema.Item != null)
            {
                var isObject = schema.Item?.ActualSchema.Type.IsObject() == true;
                var isDictionary = schema.Item?.ActualSchema.IsDictionary == true;
                var prefix = addInterfacePrefix && SupportsConstructorConversion(schema.Item) && isObject && !isDictionary ? "I" : "";

                if (Settings.UseLeafType)
                {
                    var itemTypes = Resolve(schema.Item, true, typeNameHint) // TODO: Make typeNameHint singular if possible
                        .Split(new[] { UnionPipe }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => GetNullableItemType(schema, prefix + x))
                        .ToList();

                    var itemType = string.Join(UnionPipe, itemTypes);

                    // is TypeUnion
                    if (itemTypes.Count > 1)
                    {
                        itemType = string.Format("({0})", itemType);
                    }

                    return string.Format("{0}[]", itemType);
                }
                else
                {
                    var itemType = prefix + Resolve(schema.Item, true, typeNameHint);
                    return string.Format("{0}[]", GetNullableItemType(schema, itemType)); // TODO: Make typeNameHint singular if possible
                }
            }

            if (schema.Items != null && schema.Items.Count > 0)
            {
                var tupleTypes = schema.Items
                    .Select(s => GetNullableItemType(s, Resolve(s, false, null)))
                    .ToArray();

                return string.Format("[" + string.Join(", ", tupleTypes) + "]");
            }

            return "any[]";
        }

        private string GetNullableItemType(JsonSchema schema, string itemType)
        {
            if (Settings.SupportsStrictNullChecks && schema.Item.IsNullable(Settings.SchemaType))
            {
                return string.Format("({0} | {1})", itemType, Settings.NullValue.ToString().ToLowerInvariant());
            }

            return itemType;
        }
    }
}
