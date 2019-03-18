//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class CSharpTypeResolver : TypeResolverBase
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings)
            : this(settings, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <param name="exceptionSchema">The exception type schema.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings, JsonSchema4 exceptionSchema)
            : base(settings)
        {
            Settings = settings;
            ExceptionSchema = exceptionSchema;
        }

        /// <summary>Gets the exception schema.</summary>
        public JsonSchema4 ExceptionSchema { get; }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public override string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, isNullable, typeNameHint, true);
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <param name="checkForExistingSchema">Checks whether a named schema is already registered.</param>
        /// <returns>The type name.</returns>
        public string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint, bool checkForExistingSchema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            schema = GetResolvableSchema(schema);

            if (schema == ExceptionSchema)
                return "System.Exception";

            // Primitive schemas (no new type)

            if (schema.ActualTypeSchema.IsAnyType && !IsDefinitionTypeSchema(schema.ActualTypeSchema))
                return "object";

            var type = schema.ActualTypeSchema.Type;
            if (type == JsonObjectType.None && schema.ActualTypeSchema.IsEnumeration)
            {
                type = schema.ActualTypeSchema.Enumeration.All(v => v is int) ?
                    JsonObjectType.Integer :
                    JsonObjectType.String;
            }

            if (type.HasFlag(JsonObjectType.Number))
                return ResolveNumber(schema.ActualTypeSchema, isNullable);

            if (type.HasFlag(JsonObjectType.Integer) && !schema.ActualTypeSchema.IsEnumeration)
                return ResolveInteger(schema.ActualTypeSchema, isNullable, typeNameHint);

            if (type.HasFlag(JsonObjectType.Boolean))
                return ResolveBoolean(isNullable);

            if (type.HasFlag(JsonObjectType.String) && !schema.ActualTypeSchema.IsEnumeration)
                return ResolveString(schema.ActualTypeSchema, isNullable, typeNameHint);

            if (schema.IsBinary)
                return "byte[]";

            // Type generating schemas

            if (schema.Type.HasFlag(JsonObjectType.Array))
                return ResolveArrayOrTuple(schema);

            if (schema.IsDictionary)
                return ResolveDictionary(schema);

            if (schema.ActualTypeSchema.IsEnumeration)
                return GetOrGenerateTypeName(schema, typeNameHint) + (isNullable ? "?" : string.Empty);

            return GetOrGenerateTypeName(schema, typeNameHint);
        }

        /// <summary>Checks whether the given schema should generate a type.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>True if the schema should generate a type.</returns>
        protected override bool IsDefinitionTypeSchema(JsonSchema4 schema)
        {
            if ((schema.IsDictionary && !Settings.InlineNamedDictionaries) ||
                (schema.IsArray && !Settings.InlineNamedArrays) ||
                (schema.IsTuple && !Settings.InlineNamedTuples))
            {
                return true;
            }

            if (schema.IsAnyType && (Settings.ClassStyle == CSharpClassStyle.Inpc || Settings.ClassStyle == CSharpClassStyle.Prism))
            {
                return true;
            }

            return base.IsDefinitionTypeSchema(schema);
        }

        private string ResolveString(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.Format == JsonFormatStrings.Date)
                return isNullable && Settings.DateType?.ToLowerInvariant() != "string" ? Settings.DateType + "?" : Settings.DateType;

            if (schema.Format == JsonFormatStrings.DateTime)
                return isNullable && Settings.DateTimeType?.ToLowerInvariant() != "string" ? Settings.DateTimeType + "?" : Settings.DateTimeType;

            if (schema.Format == JsonFormatStrings.Time)
                return isNullable && Settings.TimeType?.ToLowerInvariant() != "string" ? Settings.TimeType + "?" : Settings.TimeType;

            if (schema.Format == JsonFormatStrings.TimeSpan)
                return isNullable && Settings.TimeSpanType?.ToLowerInvariant() != "string" ? Settings.TimeSpanType + "?" : Settings.TimeSpanType;

            if (schema.Format == JsonFormatStrings.Uri)
                return "System.Uri";

#pragma warning disable 618 // used to resolve type from schemas generated with previous version of the library

            if (schema.Format == JsonFormatStrings.Guid || schema.Format == JsonFormatStrings.Uuid)
                return isNullable ? "System.Guid?" : "System.Guid";

            if (schema.Format == JsonFormatStrings.Base64 || schema.Format == JsonFormatStrings.Byte)
                return "byte[]";

#pragma warning restore 618

            return "string";
        }

        private static string ResolveBoolean(bool isNullable)
        {
            return isNullable ? "bool?" : "bool";
        }

        private string ResolveInteger(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.Format == JsonFormatStrings.Byte)
                return isNullable ? "byte?" : "byte";

            if (schema.Format == JsonFormatStrings.Long || schema.Format == "long")
                return isNullable ? "long?" : "long";

            return isNullable ? "int?" : "int";
        }

        private static string ResolveNumber(JsonSchema4 schema, bool isNullable)
        {
            if (schema.Format == JsonFormatStrings.Decimal)
                return isNullable ? "decimal?" : "decimal";

            return isNullable ? "double?" : "double";
        }

        private string ResolveArrayOrTuple(JsonSchema4 schema)
        {
            if (schema.Item != null)
            {
                return string.Format(Settings.ArrayType + "<{0}>", Resolve(schema.Item, schema.Item.IsNullable(Settings.SchemaType), null));
            }

            if (schema.Items != null && schema.Items.Count > 0)
            {
                var tupleTypes = schema.Items
                    .Select(i => Resolve(i, i.IsNullable(Settings.SchemaType), null))
                    .ToArray();

                return string.Format("System.Tuple<" + string.Join(", ", tupleTypes) + ">");
            }

            return Settings.ArrayType + "<object>";
        }

        private string ResolveDictionary(JsonSchema4 schema)
        {
            var valueType = ResolveDictionaryValueType(schema, "object");
            var keyType = ResolveDictionaryKeyType(schema, "string");
            return string.Format(Settings.DictionaryType + "<{0}, {1}>", keyType, valueType);
        }
    }
}
