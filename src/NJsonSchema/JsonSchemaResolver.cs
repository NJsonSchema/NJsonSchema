//-----------------------------------------------------------------------
// <copyright file="JsonSchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NJsonSchema.Generation;

namespace NJsonSchema
{
    /// <summary>Manager which resolves types to schemas and appends missing schemas to the root object.</summary>
    public class JsonSchemaResolver
    {
        private readonly Dictionary<string, JsonSchema4> _mappings = new Dictionary<string, JsonSchema4>();
        private readonly JsonSchemaGeneratorSettings _settings;

        private JsonSchema4 RootSchema => (JsonSchema4)RootObject;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaResolver" /> class.</summary>
        /// <param name="rootObject">The root schema.</param>
        /// <param name="settings">The settings.</param>
        public JsonSchemaResolver(object rootObject, JsonSchemaGeneratorSettings settings)
        {
            _settings = settings;
            RootObject = rootObject; 
        }

        /// <summary>Gets the root object.</summary>
        public object RootObject { get; }

        /// <summary>Determines whether the specified type has a schema.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns><c>true</c> when the mapping exists.</returns>
        public bool HasSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings.ContainsKey(GetKey(type, isIntegerEnumeration));
        }

        /// <summary>Gets the schema for a given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns>The schema.</returns>
        public JsonSchema4 GetSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings[GetKey(type, isIntegerEnumeration)];
        }

        /// <summary>Adds a schema to type mapping.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="InvalidOperationException">Added schema is not a JsonSchema4 instance.</exception>
        public virtual void AddSchema(Type type, bool isIntegerEnumeration, JsonSchema4 schema)
        {
            if (schema.GetType() != typeof(JsonSchema4))
                throw new InvalidOperationException("Added schema is not a JsonSchema4 instance.");

            if (schema != RootObject)
                AppendSchema(schema, _settings.SchemaNameGenerator.Generate(type));

            _mappings.Add(GetKey(type, isIntegerEnumeration), schema);
        }

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="schema">The schema to append.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException">The root schema cannot be appended.</exception>
        public virtual void AppendSchema(JsonSchema4 schema, string typeNameHint)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (schema == RootObject)
                throw new ArgumentException("The root schema cannot be appended.");

            if (!RootSchema.Definitions.Values.Contains(schema))
            {
                var typeName = _settings.TypeNameGenerator.Generate(schema, typeNameHint, RootSchema.Definitions.Keys);
                if (!string.IsNullOrEmpty(typeName) && !RootSchema.Definitions.ContainsKey(typeName))
                    RootSchema.Definitions[typeName] = schema;
                else
                    RootSchema.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = schema;
            }
        }

        /// <summary>Gets all the schemas.</summary>
        public IEnumerable<JsonSchema4> Schemas => _mappings.Values;

        private string GetKey(Type type, bool isIntegerEnum)
        {
            return type.FullName + (isIntegerEnum ? ":Integer" : string.Empty);
        }
    }
}