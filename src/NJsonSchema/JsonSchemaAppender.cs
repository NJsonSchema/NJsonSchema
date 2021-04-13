//-----------------------------------------------------------------------
// <copyright file="JsonSchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema
{
    /// <summary>Appends a schema to a document (i.e. another schema).</summary>
    public class JsonSchemaAppender
    {
        private readonly ITypeNameGenerator _typeNameGenerator;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAppender" /> class.</summary>
        /// <param name="rootObject">The root schema.</param>
        /// <param name="typeNameGenerator">The type name generator.</param>
        public JsonSchemaAppender(object rootObject, ITypeNameGenerator typeNameGenerator)
        {
            RootObject = rootObject;
            _typeNameGenerator = typeNameGenerator;
        }

        /// <summary>Gets the root object.</summary>
        public object RootObject { get; }

        /// <summary>Gets the root schema.</summary>
        protected JsonSchema RootSchema => (JsonSchema)RootObject;

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="schema">The schema to append.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="type"></param>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException">The root schema cannot be appended.</exception>
        public virtual void AppendSchema(JsonSchema schema, string typeNameHint, Type type)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (schema == RootObject)
            {
                return;
            }

            if (!RootSchema.Definitions.Values.Contains(schema))
            {
                var typeName = _typeNameGenerator.Generate(schema, typeNameHint, RootSchema.Definitions.Keys);
                if (!string.IsNullOrEmpty(typeName) && !RootSchema.Definitions.ContainsKey(typeName))
                {
                    RootSchema.Definitions[typeName] = schema;
                }
                else
                {
                    RootSchema.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = schema;
                }
            }
        }
    }
}