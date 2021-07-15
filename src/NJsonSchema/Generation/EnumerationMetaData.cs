using Newtonsoft.Json;

namespace NJsonSchema.Generation {
    /// <summary>
    /// Additional meta data for enumerations.
    /// </summary>
    /// <remarks>
    /// An enum value doesn't have a title or description associated with it in the JSON schema specification.
    /// </remarks>
    public class EnumerationMetaData {
        /// <summary>
        /// Surrogate for the "title" keyword as described in <see href="https://json-schema.org/draft/2020-12/json-schema-validation.html#rfc.section.9.1">the specification</see>.
        /// </summary>
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = 0)]
        public string Title { get; set; }
        
        /// <summary>
        /// Surrogate for the "description" keyword as described in <see href="https://json-schema.org/draft/2020-12/json-schema-validation.html#rfc.section.9.1">the specification</see>.
        /// </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = 0)]
        public string Description { get; set; }
    }
}