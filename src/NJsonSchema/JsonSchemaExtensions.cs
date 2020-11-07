//-----------------------------------------------------------------------
// <copyright file="JsonSchemaExtensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.Visitors;

namespace NJsonSchema
{
    /// <summary>JSON Schema graph utilities.</summary>
    public static class JsonSchemaExtensions
    {
        /// <summary>Gets a value indicating whether this schema inherits from the given parent schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="parentSchema">The possible parent schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns>true or false.</returns>
        public static bool InheritsSchema(this JsonSchema schema, JsonSchema parentSchema, object rootObject)
        {
            return parentSchema != null && schema.ActualSchema
                .GetAllInheritedSchemas(rootObject)
                .Concat(new List<JsonSchema> { schema })
                .Any(s => s.ActualSchema == parentSchema.ActualSchema) == true;
        }

        /// <summary>Gets the inherited/parent schema (most probable base schema in allOf).</summary>
        /// <remarks>Used for code generation.</remarks>
        public static JsonSchema GetInheritedSchema(this JsonSchema schema, object rootObject)
        {
            if (schema.HasReference)
                return null;

            if (schema.AllOf == null || schema.AllOf.Count == 0)
                return rootObject != null ? schema.GetOneOfInheritedSchema(rootObject) : null;

            if (schema.AllOf.Count == 1)
                return schema.AllOf.First().ActualSchema;

            if (schema.AllOf.Any(s => s.HasReference && !s.ActualSchema.IsAnyType))
                return schema.AllOf.First(s => s.HasReference && !s.ActualSchema.IsAnyType).ActualSchema;

            if (schema.AllOf.Any(s => s.Type.HasFlag(JsonObjectType.Object) && !s.ActualSchema.IsAnyType))
                return schema.AllOf.First(s => s.Type.HasFlag(JsonObjectType.Object) && !s.ActualSchema.IsAnyType).ActualSchema;

            return schema.AllOf.First(s => !s.ActualSchema.IsAnyType)?.ActualSchema ??
                (rootObject != null ? schema.GetOneOfInheritedSchema(rootObject) : null);
        }

        /// <summary>Gets the list of all inherited/parent schemas.</summary>
        /// <remarks>Used for code generation.</remarks>
#if !LEGACY
        public static IReadOnlyCollection<JsonSchema> GetAllInheritedSchemas(this JsonSchema schema, object rootObject)
#else
        public static ICollection<JsonSchema> GetAllInheritedSchemas(this JsonSchema schema, object rootObject)
#endif
        {
            var inheritedSchema = schema.GetInheritedSchema(rootObject) != null ?
                new List<JsonSchema> { schema.GetInheritedSchema(rootObject) } :
                new List<JsonSchema>();

            return inheritedSchema.Concat(inheritedSchema.SelectMany(s => s.GetAllInheritedSchemas(rootObject))).ToList();
        }

        /// <summary>Determines whether the given schema is the parent schema of this schema (i.e. super/base class).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="inheritedSchema">The possible subtype schema.</param>
        /// <param name="rootObject">The root object needed to scan oneOf properties.</param>
        /// <returns>true or false</returns>
        public static bool Inherits(this JsonSchema schema, JsonSchema inheritedSchema, object rootObject)
        {
            inheritedSchema = inheritedSchema.ActualSchema;
            return schema.GetInheritedSchema(rootObject)?.ActualSchema == inheritedSchema || 
                schema.GetInheritedSchema(rootObject)?.Inherits(inheritedSchema, rootObject) == true;
        }

        /// <summary>Gets the discriminator or discriminator of an inherited schema (or null).</summary>
        public static OpenApiDiscriminator GetBaseDiscriminator(this JsonSchema schema, object rootObject)
        {
            return schema.DiscriminatorObject ?? schema.GetInheritedSchema(rootObject)?.ActualSchema.GetBaseDiscriminator(rootObject);
        }

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
                    DerivedSchemas.Add(schema, typeNameHint);

                return schema;
            }
        }


        /// <summary>Gets the schemas which are inherited via oneOf.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static JsonSchema GetOneOfInheritedSchema(this JsonSchema schema, object rootObject)
        {
            return schema.GetOneOfInheritedSchemas(rootObject)?.FirstOrDefault().Key;
        }

        /// <summary>Gets the schemas which are inherited via oneOf.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static IDictionary<JsonSchema, string> GetOneOfInheritedSchemas(this JsonSchema schema, object rootObject)
        {
            var visitor = new InheritedSchemaVisitor(schema);
            visitor.Visit(rootObject);
            return visitor.InheritedSchemas;
        }

        private class InheritedSchemaVisitor : JsonSchemaVisitorBase
        {
            private readonly JsonSchema _schema;

            public Dictionary<JsonSchema, string> InheritedSchemas { get; } = new Dictionary<JsonSchema, string>();

            public InheritedSchemaVisitor(JsonSchema schema)
            {
                _schema = schema;
            }

            protected override JsonSchema VisitSchema(JsonSchema schema, string path, string typeNameHint)
            {
                if (schema.OneOf.Any(s => s.ActualSchema == _schema.ActualSchema))
                    InheritedSchemas.Add(schema, typeNameHint);

                return schema;
            }
        }
    }
}