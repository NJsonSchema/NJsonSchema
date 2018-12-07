//-----------------------------------------------------------------------
// <copyright file="TypeResolverBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
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
        private readonly Dictionary<JsonSchema4, string> _generatedTypeNames = new Dictionary<JsonSchema4, string>();

        /// <summary>Initializes a new instance of the <see cref="TypeResolverBase" /> class.</summary>
        /// <param name="settings">The settings.</param>
        protected TypeResolverBase(CodeGeneratorSettingsBase settings)
        {
            _settings = settings;
        }

        /// <summary>Gets the registered schemas and with their type names.</summary>
        public IDictionary<JsonSchema4, string> Types => _generatedTypeNames.ToDictionary(p => p.Key, p => p.Value);

        /// <summary>Tries to resolve the schema and returns null if there was a problem.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public string TryResolve(JsonSchema4 schema, string typeNameHint)
        {
            return schema != null ? Resolve(schema, false, typeNameHint) : null;
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public abstract string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint);

        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public virtual string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint)
        {
            schema = schema.ActualSchema;
            RegisterSchemaDefinitions(schema.Definitions);

            if (!_generatedTypeNames.ContainsKey(schema))
            {
                var reservedTypeNames = _generatedTypeNames.Values.Distinct().ToList();
                _generatedTypeNames[schema] = _settings.TypeNameGenerator.Generate(schema, typeNameHint, reservedTypeNames);
            }

            return _generatedTypeNames[schema];
        }

        /// <summary>Adds all schemas to the resolver.</summary>
        /// <param name="definitions">The schema definitions.</param>
        public void RegisterSchemaDefinitions(IDictionary<string, JsonSchema4> definitions)
        {
            if (definitions != null)
            {
                foreach (var pair in definitions)
                {
                    var schema = pair.Value.ActualSchema;

                    if (IsTypeSchema(schema))
                    {
                        GetOrGenerateTypeName(schema, pair.Key);
                    }
                }
            }
        }

        /// <summary>Gets the actual schema and removes a nullable oneOf reference if available.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The actually resolvable schema</returns>
        protected JsonSchema4 GetResolvableSchema(JsonSchema4 schema)
        {
            return schema.OneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema ?? schema.ActualSchema;
        }

        /// <summary>Checks whether the given schema should generate a type.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>True if the schema should generate a type.</returns>
        protected virtual bool IsTypeSchema(JsonSchema4 schema)
        {
            return !schema.IsDictionary &&
                   !schema.IsAnyType &&
                   (schema.IsEnumeration ||
                    schema.Type == JsonObjectType.None ||
                    schema.Type.HasFlag(JsonObjectType.Object));
        }

        /// <summary>Resolves the type of the dictionary value of the given schema (must be a dictionary schema).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="fallbackType">The fallback type (e.g. 'object').</param>
        /// <returns>The type.</returns>
        protected string ResolveDictionaryValueType(JsonSchema4 schema, string fallbackType)
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
        protected string ResolveDictionaryKeyType(JsonSchema4 schema, string fallbackType)
        {
            if (schema.DictionaryKey != null)
            {
                return Resolve(schema.DictionaryKey, schema.DictionaryKey.ActualSchema.IsNullable(_settings.SchemaType), null);
            }

            return fallbackType;
        }
    }
}