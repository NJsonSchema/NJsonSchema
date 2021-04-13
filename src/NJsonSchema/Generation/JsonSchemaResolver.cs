//-----------------------------------------------------------------------
// <copyright file="JsonSchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NJsonSchema.Generation
{
    /// <summary>Manager which resolves types to schemas and appends missing schemas to the root object.</summary>
    public class JsonSchemaResolver : JsonSchemaAppender
    {
        private readonly Dictionary<string, JsonSchema> _mappings = new Dictionary<string, JsonSchema>();
        private readonly JsonSchemaGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaResolver" /> class.</summary>
        /// <param name="rootObject">The root schema.</param>
        /// <param name="settings">The settings.</param>
        public JsonSchemaResolver(object rootObject, JsonSchemaGeneratorSettings settings)
            : base(rootObject, settings.TypeNameGenerator)
        {
            _settings = settings;
        }

        /// <summary>Determines whether the specified type has a schema.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns><c>true</c> when the mapping exists.</returns>
        public bool HasSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings.ContainsKey(GetTypeKey(type, isIntegerEnumeration));
        }

        /// <summary>Gets the schema for a given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns>The schema.</returns>
        public JsonSchema GetSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings[GetTypeKey(type, isIntegerEnumeration)];
        }

        /// <summary>Adds a schema to type mapping.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="InvalidOperationException">Added schema is not a JsonSchema4 instance.</exception>
        public virtual void AddSchema(Type type, bool isIntegerEnumeration, JsonSchema schema)
        {
            if (schema.GetType() != typeof(JsonSchema))
            {
                throw new InvalidOperationException("Added schema is not a JsonSchema4 instance.");
            }

            if (schema != RootObject)
            {
                AppendSchema(schema, _settings.SchemaNameGenerator.Generate(type), type);
            }

            _mappings.Add(GetTypeKey(type, isIntegerEnumeration), schema);
        }

        /// <summary>Gets all the schemas.</summary>
        public IEnumerable<JsonSchema> Schemas => _mappings.Values;

        /// <summary>
        /// Gets the mapping key for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnum">Specifies whether the type is an integer enum.</param>
        /// <returns>The mapping key.</returns>
        protected virtual string GetTypeKey(Type type, bool isIntegerEnum)
        {
            return type.FullName + (isIntegerEnum ? ":Integer" : string.Empty);
        }
    }
}