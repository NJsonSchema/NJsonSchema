//-----------------------------------------------------------------------
// <copyright file="TypeResolverBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type resolver base.</summary>
    public abstract class TypeResolverBase
    {
        private readonly CodeGeneratorSettingsBase _settings;
        private readonly Dictionary<JsonSchema, string> _generatedTypeNames = new Dictionary<JsonSchema, string>();

        /// <summary>Initializes a new instance of the <see cref="TypeResolverBase" /> class.</summary>
        /// <param name="settings">The settings.</param>
        protected TypeResolverBase(CodeGeneratorSettingsBase settings)
        {
            _settings = settings;
        }

        /// <summary>Gets the registered schemas and with their type names.</summary>
        public IDictionary<JsonSchema, string> Types => _generatedTypeNames.ToDictionary(p => p.Key, p => p.Value);

        /// <summary>Tries to resolve the schema and returns null if there was a problem.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public string TryResolve(JsonSchema schema, string typeNameHint)
        {
            return schema != null ? Resolve(schema, false, typeNameHint) : null;
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public abstract string Resolve(JsonSchema schema, bool isNullable, string typeNameHint);

        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public virtual string GetOrGenerateTypeName(JsonSchema schema, string typeNameHint)
        {
            schema = RemoveNullability(schema).ActualSchema;

            RegisterSchemaDefinitions(schema.Definitions);

            if (!_generatedTypeNames.TryGetValue(schema, out var typeNames))
            {
                var reservedTypeNames = new HashSet<string>(_generatedTypeNames.Values);
                typeNames = _settings.TypeNameGenerator.Generate(schema, typeNameHint, reservedTypeNames);
                _generatedTypeNames[schema] = typeNames;
            }

            return typeNames;
        }

        /// <summary>Adds all schemas to the resolver.</summary>
        /// <param name="definitions">The schema definitions.</param>
        public void RegisterSchemaDefinitions(IDictionary<string, JsonSchema> definitions)
        {
            if (definitions != null)
            {
                foreach (var pair in definitions)
                {
                    var schema = pair.Value.ActualSchema;

                    if (IsDefinitionTypeSchema(schema))
                    {
                        GetOrGenerateTypeName(schema, pair.Key);
                    }
                }
            }
        }

        /// <summary>Removes a nullable oneOf reference if available.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The actually resolvable schema</returns>
        public JsonSchema RemoveNullability(JsonSchema schema)
        {
            // TODO: Method on JsonSchema4?
            return schema.OneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema)) ?? schema;
        }

        /// <summary>Gets the actual schema (i.e. when not referencing a type schema or it is inlined)
        /// and removes a nullable oneOf reference if available.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The actually resolvable schema</returns>
        public JsonSchema GetResolvableSchema(JsonSchema schema)
        {
            schema = RemoveNullability(schema);
            return IsDefinitionTypeSchema(schema.ActualSchema) ? schema : schema.ActualSchema;
        }

        /// <summary>Checks whether the given schema generates a new type (e.g. class, enum, class with dictionary inheritance, etc.)
        /// or is an inline type (e.g. string, number, etc.). Warning: Enum will also return true.</summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public bool GeneratesType(JsonSchema schema)
        {
            schema = GetResolvableSchema(schema);
            return schema.HasReference || (schema.IsObject && !schema.IsDictionary && !schema.IsAnyType);
        }

        /// <summary>Checks whether the given schema from definitions should generate a type.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>True if the schema should generate a type.</returns>
        protected virtual bool IsDefinitionTypeSchema(JsonSchema schema)
        {
            if (schema.IsAnyType && _settings.InlineNamedAny)
            {
                return false;
            }

            return !schema.IsTuple &&
                   !schema.IsDictionary &&
                   !schema.IsArray &&
                   (schema.IsEnumeration ||
                    schema.Type == JsonObjectType.None ||
                    schema.Type.HasFlag(JsonObjectType.Object));
        }

        /// <summary>Resolves the type of the dictionary value of the given schema (must be a dictionary schema).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="fallbackType">The fallback type (e.g. 'object').</param>
        /// <returns>The type.</returns>
        protected string ResolveDictionaryValueType(JsonSchema schema, string fallbackType)
        {
            if (schema.AdditionalPropertiesSchema != null)
            {
                return Resolve(schema.AdditionalPropertiesSchema, schema.AdditionalPropertiesSchema.ActualSchema.IsNullable(_settings.SchemaType), null);
            }

            if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any())
            {
                var valueTypes = schema.PatternProperties
                    .Select(p => Resolve(p.Value, p.Value.IsNullable(_settings.SchemaType), null))
                    .Distinct()
                    .ToList();

                if (valueTypes.Count == 1)
                {
                    return valueTypes.First();
                }
            }

            return fallbackType;
        }

        /// <summary>Resolves the type of the dictionary key of the given schema (must be a dictionary schema).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="fallbackType">The fallback type (e.g. 'object').</param>
        /// <returns>The type.</returns>
        protected string ResolveDictionaryKeyType(JsonSchema schema, string fallbackType)
        {
            if (schema.DictionaryKey != null)
            {
                return Resolve(schema.DictionaryKey, schema.DictionaryKey.ActualSchema.IsNullable(_settings.SchemaType), null);
            }

            return fallbackType;
        }
    }
}