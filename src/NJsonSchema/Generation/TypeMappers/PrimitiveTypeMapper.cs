//-----------------------------------------------------------------------
// <copyright file="PrimitiveTypeMapper.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>Maps .NET type to a generated JSON Schema describing a primitive value.</summary>
    public class PrimitiveTypeMapper : ITypeMapper
    {
        private readonly Action<JsonSchema4> _transformer;

        /// <summary>Initializes a new instance of the <see cref="PrimitiveTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="transformer">The transformer.</param>
        public PrimitiveTypeMapper(Type mappedType, Action<JsonSchema4> transformer)
        {
            _transformer = transformer;
            MappedType = mappedType;
        }

        /// <summary>Gets the mapped type.</summary>
        public Type MappedType { get; }

        /// <summary>Gets a value indicating whether to use a JSON Schema reference for the type.</summary>
        public bool UseReference { get; } = false;

        /// <summary>Gets the schema for the mapped type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="context">The context.</param>
#pragma warning disable 1998
        public async Task GenerateSchemaAsync(JsonSchema4 schema, TypeMapperContext context)
#pragma warning restore 1998
        {
            _transformer(schema);
        }
    }
}