//-----------------------------------------------------------------------
// <copyright file="ObjectTypeMapper.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>Maps .NET type to a generated JSON Schema describing an object.</summary>
    public class ObjectTypeMapper : ITypeMapper
    {
        private readonly Func<JsonSchemaGenerator, JsonSchemaResolver, Task<JsonSchema4>> _schemaFactory;

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schema">The schema.</param>
        public ObjectTypeMapper(Type mappedType, JsonSchema4 schema)
            : this(mappedType, (schemaGenerator, schemaResolver) => schema)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schemaFactory">The schema factory.</param>
        public ObjectTypeMapper(Type mappedType, Func<JsonSchemaGenerator, JsonSchemaResolver, JsonSchema4> schemaFactory)
#pragma warning disable 1998
            : this(mappedType, async (schemaGenerator, schemaResolver) => schemaFactory(schemaGenerator, schemaResolver))
#pragma warning restore 1998
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schemaFactory">The schema factory.</param>
        public ObjectTypeMapper(Type mappedType, Func<JsonSchemaGenerator, JsonSchemaResolver, Task<JsonSchema4>> schemaFactory)
        {
            _schemaFactory = schemaFactory;
            MappedType = mappedType;
        }

        /// <summary>Gets the mapped type.</summary>
        public Type MappedType { get; }

        /// <summary>Gets a value indicating whether to use a JSON Schema reference for the type.</summary>
        public bool UseReference => true;

        /// <summary>Gets the schema for the mapped type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="context">The context.</param>
#pragma warning disable 1998
        public async Task GenerateSchemaAsync(JsonSchema4 schema, TypeMapperContext context)
#pragma warning restore 1998
        {
            if (!context.JsonSchemaResolver.HasSchema(MappedType, false))
                context.JsonSchemaResolver.AddSchema(MappedType, false, await _schemaFactory(context.JsonSchemaGenerator, context.JsonSchemaResolver));

            schema.Reference = context.JsonSchemaResolver.GetSchema(MappedType, false);
        }
    }
}