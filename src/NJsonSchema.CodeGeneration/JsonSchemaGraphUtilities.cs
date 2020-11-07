//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGraphUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using NJsonSchema.Visitors;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>JSON Schema graph utilities.</summary>
    public static class JsonSchemaGraphUtilities
    {
        /// <summary>Gets the derived schemas.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static IDictionary<JsonSchema, string> GetDerivedSchemas(this JsonSchema schema, object rootObject)
        {
            var visitor = new DerivedSchemaVisitor(schema, rootObject);
            visitor.Visit(rootObject);
            return visitor.DerivedSchemas;
        }

        private class DerivedSchemaVisitor : JsonSchemaVisitorBase
        {
            private readonly JsonSchema _baseSchema;
            private readonly object _rootObject;

            public Dictionary<JsonSchema, string> DerivedSchemas { get; } = new Dictionary<JsonSchema, string>();

            public DerivedSchemaVisitor(JsonSchema baseSchema, object rootObject)
            {
                _baseSchema = baseSchema;
                _rootObject = rootObject;
            }

            protected override JsonSchema VisitSchema(JsonSchema schema, string path, string typeNameHint)
            {
                if (schema.Inherits(_baseSchema, _rootObject) && _baseSchema != schema)
                {
                    DerivedSchemas.Add(schema, typeNameHint);
                }

                return schema;
            }
        }
    }
}