//-----------------------------------------------------------------------
// <copyright file="ChildSchemaValidationError.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation
{
    /// <summary>A subschema validation error. </summary>
    public class ChildSchemaValidationError : ValidationError
    {
        /// <summary>Initializes a new instance of the <see cref="ValidationError"/> class. </summary>
        /// <param name="kind">The error kind. </param>
        /// <param name="property">The property name. </param>
        /// <param name="path">The property path. </param>
        /// <param name="errors">The error list. </param>
        /// <param name="token">The token that failed to validate. </param>
        /// <param name="schema">The schema that contains the validation rule.</param>
#if !LEGACY
        public ChildSchemaValidationError(ValidationErrorKind kind, string property, string path, IReadOnlyDictionary<JsonSchema4, ICollection<ValidationError>> errors, JToken token, JsonSchema4 schema)
#else
        public ChildSchemaValidationError(ValidationErrorKind kind, string property, string path, IDictionary<JsonSchema4, ICollection<ValidationError>> errors, JToken token, JsonSchema4 schema)
#endif
            : base(kind, property, path, token, schema)
        {
            Errors = errors;
        }

        /// <summary>Gets the errors for each validated subschema. </summary>
#if !LEGACY
        public IReadOnlyDictionary<JsonSchema4, ICollection<ValidationError>> Errors { get; private set; }
#else
        public IDictionary<JsonSchema4, ICollection<ValidationError>> Errors { get; private set; }
#endif

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var output = string.Format("{0}: {1}\n", Kind, Path);
            foreach (var error in Errors)
            {
                output += "{\n";
                foreach (var validationError in error.Value)
                {
                    output += string.Format("  {0}\n", validationError.ToString().Replace("\n", "\n  "));
                }
                output += "}\n";
            }
            return output;
        }
    }
}