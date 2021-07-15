//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema
{
    /// <summary>A base class for describing a JSON schema. </summary>
    public partial class JsonSchema : IDocumentPathProvider
    {
        internal static readonly HashSet<string> JsonSchemaPropertiesCache = new HashSet<string>(typeof(JsonSchema).GetContextualProperties().Select(p => p.Name).ToArray());

        private const SchemaType SerializationSchemaType = SchemaType.JsonSchema;
        private static Lazy<PropertyRenameAndIgnoreSerializerContractResolver> ContractResolver = new Lazy<PropertyRenameAndIgnoreSerializerContractResolver>(
            () => CreateJsonSerializerContractResolver(SerializationSchemaType));

        private IDictionary<string, JsonSchemaProperty> _properties;
        private IDictionary<string, JsonSchemaProperty> _patternProperties;
        private IDictionary<string, JsonSchema> _definitions;

        private ICollection<JsonSchema> _allOf;
        private ICollection<JsonSchema> _anyOf;
        private ICollection<JsonSchema> _oneOf;
        private JsonSchema _not;
        private JsonSchema _dictionaryKey;

        private JsonObjectType _type;
        private JsonSchema _item;
        private ICollection<JsonSchema> _items;

        private bool _allowAdditionalItems = true;
        private JsonSchema _additionalItemsSchema = null;

        private bool _allowAdditionalProperties = true;
        private JsonSchema _additionalPropertiesSchema = null;

        /// <summary>Initializes a new instance of the <see cref="JsonSchema"/> class. </summary>
        public JsonSchema()
        {
            Initialize();

            if (JsonSchemaSerialization.CurrentSchemaType == SchemaType.Swagger2)
            {
                _allowAdditionalProperties = false; // the default for Swagger2 is false (change required when deserializing)
            }
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static JsonSchema CreateAnySchema()
        {
            return new JsonSchema();
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static TSchemaType CreateAnySchema<TSchemaType>()
            where TSchemaType : JsonSchema, new()
        {
            return new TSchemaType();
        }

        /// <summary>Gets the NJsonSchema toolchain version.</summary>
#if LEGACY
        public static string ToolchainVersion => typeof(JsonSchema).Assembly.GetName().Version +
                                                 " NET40 (Newtonsoft.Json v" + typeof(JToken).Assembly.GetName().Version + ")";
#else
        public static string ToolchainVersion => typeof(JsonSchema).GetTypeInfo().Assembly.GetName().Version +
                                                 " (Newtonsoft.Json v" + typeof(JToken).GetTypeInfo().Assembly.GetName().Version + ")";
#endif

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new JsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new JsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(type);
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from sample JSON data.</summary>
        /// <returns>The JSON Schema.</returns>
        public static JsonSchema FromSampleJson(string data)
        {
            var generator = new SampleJsonSchemaGenerator();
            return generator.Generate(data);
        }

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">Cancellation token instance</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await FromFileAsync(filePath, factory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static async Task<JsonSchema> FromFileAsync(string filePath, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var data = DynamicApis.FileReadAllText(filePath);
            return await FromJsonAsync(data, filePath, referenceResolverFactory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<JsonSchema> FromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await FromUrlAsync(url, factory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<JsonSchema> FromUrlAsync(string url, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var data = await DynamicApis.HttpGetAsync(url, cancellationToken).ConfigureAwait(false); 
            return await FromJsonAsync(data, url, referenceResolverFactory,cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromJsonAsync(string data, CancellationToken cancellationToken = default)
        {
            return await FromJsonAsync(data, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromJsonAsync(string data, string documentPath, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await FromJsonAsync(data, documentPath, factory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromJsonAsync(string data, string documentPath, Func<JsonSchema, 
            JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            return await JsonSchemaSerialization.FromJsonAsync(data, SerializationSchemaType, documentPath, referenceResolverFactory, ContractResolver.Value, cancellationToken).ConfigureAwait(false);
        }

        internal static JsonSchema FromJsonWithCurrentSettings(object obj)
        {
            var json = JsonConvert.SerializeObject(obj, JsonSchemaSerialization.CurrentSerializerSettings);
            return JsonConvert.DeserializeObject<JsonSchema>(json, JsonSchemaSerialization.CurrentSerializerSettings);
        }

        /// <summary>Gets a value indicating whether the schema is binary (file or binary format).</summary>
        [JsonIgnore]
        public bool IsBinary
        {
            get
            {
                return Type.HasFlag(JsonObjectType.File) ||
                    (Type.HasFlag(JsonObjectType.String) && Format == JsonFormatStrings.Binary);
            }
        }

        /// <summary>Gets the inherited/parent schema (most probable base schema in allOf).</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
        public JsonSchema InheritedSchema
        {
            get
            {
                if (AllOf == null || AllOf.Count == 0 || HasReference)
                {
                    return null;
                }

                if (AllOf.Count == 1)
                {
                    return AllOf.First().ActualSchema;
                }

                if (AllOf.Any(s => s.HasReference))
                {
                    return AllOf.First(s => s.HasReference).ActualSchema;
                }

                if (AllOf.Any(s => s.Type.HasFlag(JsonObjectType.Object)))
                {
                    return AllOf.First(s => s.Type.HasFlag(JsonObjectType.Object)).ActualSchema;
                }

                return AllOf.FirstOrDefault()?.ActualSchema;
            }
        }

        /// <summary>Gets the inherited/parent schema which may also be inlined 
        /// (the schema itself if it is a dictionary or array, otherwise <see cref="InheritedSchema"/>).</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
        public JsonSchema InheritedTypeSchema
        {
            get
            {
                if (ActualTypeSchema.IsDictionary || ActualTypeSchema.IsArray || ActualTypeSchema.IsTuple)
                {
                    return ActualTypeSchema;
                }

                return InheritedSchema;
            }
        }

        /// <summary>Gets the list of all inherited/parent schemas.</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
#if !LEGACY
        public IReadOnlyCollection<JsonSchema> AllInheritedSchemas
#else
        public ICollection<JsonSchema> AllInheritedSchemas
#endif
        {
            get
            {
                var inheritedSchema = this.InheritedSchema != null ?
                    new List<JsonSchema> { this.InheritedSchema } :
                    new List<JsonSchema>();

                return inheritedSchema.Concat(inheritedSchema.SelectMany(s => s.AllInheritedSchemas)).ToList();
            }
        }

        /// <summary>Determines whether the given schema is the parent schema of this schema (i.e. super/base class).</summary>
        /// <param name="schema">The possible subtype schema.</param>
        /// <returns>true or false</returns>
        public bool Inherits(JsonSchema schema)
        {
            schema = schema.ActualSchema;
            return InheritedSchema?.ActualSchema == schema || InheritedSchema?.Inherits(schema) == true;
        }

        /// <summary>Gets the discriminator or discriminator of an inherited schema (or null).</summary>
        [JsonIgnore]
        public OpenApiDiscriminator ResponsibleDiscriminatorObject =>
            ActualDiscriminatorObject ?? InheritedSchema?.ActualSchema.ResponsibleDiscriminatorObject;

        /// <summary>Gets all properties of this schema (i.e. all direct properties and properties from the schemas in allOf which do not have a type).</summary>
        /// <remarks>Used for code generation.</remarks>
        /// <exception cref="InvalidOperationException" accessor="get">Some properties are defined multiple times.</exception>
        [JsonIgnore]
#if !LEGACY
        public IReadOnlyDictionary<string, JsonSchemaProperty> ActualProperties
#else
        public IDictionary<string, JsonSchemaProperty> ActualProperties
#endif
        {
            get
            {
                var properties = Properties
                    .Union(AllOf.Where(s => s.ActualSchema != InheritedSchema)
                    .SelectMany(s => s.ActualSchema.ActualProperties))
                    .ToList();

                var duplicatedProperties = properties
                    .GroupBy(p => p.Key)
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (duplicatedProperties.Any())
                {
                    throw new InvalidOperationException("The properties " + string.Join(", ", duplicatedProperties.Select(g => "'" + g.Key + "'")) + " are defined multiple times.");
                }

#if !LEGACY
                return new ReadOnlyDictionary<string, JsonSchemaProperty>(properties.ToDictionary(p => p.Key, p => p.Value));
#else
                return new Dictionary<string, JsonSchemaProperty>(properties.ToDictionary(p => p.Key, p => p.Value));
#endif
            }
        }

        /// <summary>Gets or sets the schema. </summary>
        [JsonProperty("$schema", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 1)]
        public string SchemaVersion { get; set; }

        /// <summary>Gets or sets the id. </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 2)]
        public string Id { get; set; }

        /// <summary>Gets or sets the title. </summary>
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 3)]
        public string Title { get; set; }

        /// <summary>Gets a value indicating whether the schema title can be used as type name.</summary>
        [JsonIgnore]
        public bool HasTypeNameTitle => !string.IsNullOrEmpty(Title) && Regex.IsMatch(Title, "^[a-zA-Z0-9_]*$");

        /// <summary>Gets or sets the description. </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual string Description { get; set; }

        /// <summary>Gets the object types (as enum flags). </summary>
        [JsonIgnore]
        public JsonObjectType Type
        {
            get => _type; set
            {
                _type = value;
                ResetTypeRaw();
            }
        }

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public JsonSchema ParentSchema => Parent as JsonSchema;

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public virtual object Parent { get; set; }

        /// <summary>Gets or sets the format string. </summary>
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Format { get; set; }

        /// <summary>Gets or sets the default value. </summary>
        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Default { get; set; }

        /// <summary>Gets or sets the required multiple of for the number value.</summary>
        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? MultipleOf { get; set; }

        /// <summary>Gets or sets the maximum allowed value.</summary>
        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Maximum { get; set; }

        /// <summary>Gets or sets the exclusive maximum value (v6).</summary>
        [JsonIgnore]
        public decimal? ExclusiveMaximum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded (v4).</summary>
        [JsonIgnore]
        public bool IsExclusiveMaximum { get; set; }

        /// <summary>Gets or sets the minimum allowed value. </summary>
        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Minimum { get; set; }

        /// <summary>Gets or sets the exclusive minimum value (v6).</summary>
        [JsonIgnore]
        public decimal? ExclusiveMinimum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded (v4).</summary>
        [JsonIgnore]
        public bool IsExclusiveMinimum { get; set; }

        /// <summary>Gets or sets the maximum length of the value string. </summary>
        [JsonProperty("maxLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MaxLength { get; set; }

        /// <summary>Gets or sets the minimum length of the value string. </summary>
        [JsonProperty("minLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MinLength { get; set; }

        /// <summary>Gets or sets the validation pattern as regular expression. </summary>
        [JsonProperty("pattern", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Pattern { get; set; }

        /// <summary>Gets or sets the maximum length of the array. </summary>
        [JsonProperty("maxItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxItems { get; set; }

        /// <summary>Gets or sets the minimum length of the array. </summary>
        [JsonProperty("minItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinItems { get; set; }

        /// <summary>Gets or sets a value indicating whether the items in the array must be unique. </summary>
        [JsonProperty("uniqueItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool UniqueItems { get; set; }

        /// <summary>Gets or sets the maximal number of allowed properties in an object. </summary>
        [JsonProperty("maxProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxProperties { get; set; }

        /// <summary>Gets or sets the minimal number of allowed properties in an object. </summary>
        [JsonProperty("minProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether the schema is deprecated (native in Open API 'deprecated', custom in Swagger/JSON Schema 'x-deprecated').</summary>
        [JsonProperty("x-deprecated", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsDeprecated { get; set; }

        /// <summary>Gets or sets a message indicating why the schema is deprecated (custom extension, sets 'x-deprecatedMessage').</summary>
        [JsonProperty("x-deprecatedMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeprecatedMessage { get; set; }

        /// <summary>Gets or sets a value indicating whether the type is abstract, i.e. cannot be instantiated directly (x-abstract).</summary>
        [JsonProperty("x-abstract", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsAbstract { get; set; }

        /// <summary>Gets or sets a value indicating whether the schema is nullable (native in Open API 'nullable', custom in Swagger 'x-nullable').</summary>
        [JsonProperty("x-nullable", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool? IsNullableRaw { get; set; }

        /// <summary>Gets or sets the example (Swagger and Open API only).</summary>
        [JsonProperty("x-example", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Example { get; set; }

        /// <summary>Gets or sets a value indicating this is an bit flag enum (custom extension, sets 'x-enumFlags', default: false).</summary>
        [JsonProperty("x-enumFlags", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsFlagEnumerable { get; set; }

        /// <summary>Gets the collection of required properties. </summary>
        [JsonIgnore]
        public ICollection<object> Enumeration { get; internal set; }

        /// <summary>Gets a value indicating whether this is enumeration.</summary>
        [JsonIgnore]
        public bool IsEnumeration => Enumeration.Count > 0;

        /// <summary>Gets the collection of required properties. </summary>
        /// <remarks>This collection can also be changed through the <see cref="JsonSchemaProperty.IsRequired"/> property. </remarks>>
        [JsonIgnore]
        public ICollection<string> RequiredProperties { get; internal set; }

        #region Child JSON schemas

        /// <summary>Gets or sets the dictionary key schema (x-dictionaryKey, only enum schemas are allowed).</summary>
        [JsonProperty("x-dictionaryKey", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema DictionaryKey
        {
            get { return _dictionaryKey; }
            set
            {
                _dictionaryKey = value;
                if (_dictionaryKey != null)
                {
                    _dictionaryKey.Parent = this;
                }
            }
        }

        /// <summary>Gets the properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchemaProperty> Properties
        {
            get { return _properties; }
            internal set
            {
                if (_properties != value)
                {
                    RegisterProperties(_properties, value);
                    _properties = value;
                }
            }
        }

        /// <summary>Gets the xml object of the schema (used in Swagger specifications). </summary>
        [JsonProperty("xml", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonXmlObject Xml
        {
            get { return _xmlObject; }
            set
            {
                _xmlObject = value;

                if (_xmlObject != null)
                {
                    _xmlObject.ParentSchema = this;
                }
            }
        }

        [JsonIgnore]
        private JsonXmlObject _xmlObject;

        /// <summary>Gets the pattern properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchemaProperty> PatternProperties
        {
            get { return _patternProperties; }
            internal set
            {
                if (_patternProperties != value)
                {
                    RegisterSchemaDictionary(_patternProperties, value);
                    _patternProperties = value;
                }
            }
        }

        /// <summary>Gets or sets the schema of an array item. </summary>
        [JsonIgnore]
        public JsonSchema Item
        {
            get { return _item; }
            set
            {
                if (_item != value)
                {
                    _item = value;
                    if (_item != null)
                    {
                        _item.Parent = this;
                        Items.Clear();
                    }
                }
            }
        }

        /// <summary>Gets or sets the schemas of the array's tuple values.</summary>
        [JsonIgnore]
        public ICollection<JsonSchema> Items
        {
            get { return _items; }
            internal set
            {
                if (_items != value)
                {
                    RegisterSchemaCollection(_items, value);
                    _items = value;

                    if (_items != null)
                    {
                        Item = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema which must not be valid. </summary>
        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema Not
        {
            get { return _not; }
            set
            {
                _not = value;
                if (_not != null)
                {
                    _not.Parent = this;
                }
            }
        }

        /// <summary>Gets the other schema definitions of this schema. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchema> Definitions
        {
            get { return _definitions; }
            internal set
            {
                if (_definitions != value)
                {
                    RegisterSchemaDictionary(_definitions, value);
                    _definitions = value;
                }
            }
        }

        /// <summary>Gets the collection of schemas where each schema must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> AllOf
        {
            get { return _allOf; }
            internal set
            {
                if (_allOf != value)
                {
                    RegisterSchemaCollection(_allOf, value);
                    _allOf = value;
                }
            }
        }

        /// <summary>Gets the collection of schemas where at least one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> AnyOf
        {
            get { return _anyOf; }
            internal set
            {
                if (_anyOf != value)
                {
                    RegisterSchemaCollection(_anyOf, value);
                    _anyOf = value;
                }
            }
        }

        /// <summary>Gets the collection of schemas where exactly one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> OneOf
        {
            get { return _oneOf; }
            internal set
            {
                if (_oneOf != value)
                {
                    RegisterSchemaCollection(_oneOf, value);
                    _oneOf = value;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional items are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalItemsSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalItems
        {
            get { return _allowAdditionalItems; }
            set
            {
                if (_allowAdditionalItems != value)
                {
                    _allowAdditionalItems = value;
                    if (!_allowAdditionalItems)
                    {
                        AdditionalItemsSchema = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional items. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalItems"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema AdditionalItemsSchema
        {
            get { return _additionalItemsSchema; }
            set
            {
                if (_additionalItemsSchema != value)
                {
                    _additionalItemsSchema = value;
                    if (_additionalItemsSchema != null)
                    {
                        AllowAdditionalItems = true;
                    }
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional properties are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalPropertiesSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalProperties
        {
            get { return _allowAdditionalProperties; }
            set
            {
                if (_allowAdditionalProperties != value)
                {
                    _allowAdditionalProperties = value;
                    if (!_allowAdditionalProperties)
                    {
                        AdditionalPropertiesSchema = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional properties. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalProperties"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema AdditionalPropertiesSchema
        {
            get { return _additionalPropertiesSchema; }
            set
            {
                if (_additionalPropertiesSchema != value)
                {
                    _additionalPropertiesSchema = value;
                    if (_additionalPropertiesSchema != null)
                    {
                        AllowAdditionalProperties = true;
                    }
                }
            }
        }

        /// <summary>Gets a value indicating whether the schema describes an object.</summary>
        [JsonIgnore]
        public bool IsObject => Type.HasFlag(JsonObjectType.Object);

        /// <summary>Gets a value indicating whether the schema represents an array type (an array where each item has the same type).</summary>
        [JsonIgnore]
        public bool IsArray => Type.HasFlag(JsonObjectType.Array) && (Items == null || Items.Count == 0);

        /// <summary>Gets a value indicating whether the schema represents an tuple type (an array where each item may have a different type).</summary>
        [JsonIgnore]
        public bool IsTuple => Type.HasFlag(JsonObjectType.Array) && Items?.Any() == true;

        /// <summary>Gets a value indicating whether the schema represents a dictionary type (no properties and AdditionalProperties or PatternProperties contain a schema).</summary>
        [JsonIgnore]
        public bool IsDictionary => Type.HasFlag(JsonObjectType.Object) &&
                                    ActualProperties.Count == 0 &&
                                    (AdditionalPropertiesSchema != null || PatternProperties.Any());

        /// <summary>Gets a value indicating whether this is any type (e.g. any in TypeScript or object in CSharp).</summary>
        [JsonIgnore]
        public bool IsAnyType => (Type.HasFlag(JsonObjectType.Object) || Type == JsonObjectType.None) &&
                                 Reference == null &&
                                 AllOf.Count == 0 &&
                                 AnyOf.Count == 0 &&
                                 OneOf.Count == 0 &&
                                 ActualProperties.Count == 0 &&
                                 PatternProperties.Count == 0 &&
                                 AdditionalPropertiesSchema == null &&
                                 MultipleOf == null &&
                                 IsEnumeration == false;

        #endregion

        /// <summary>Gets a value indicating whether the validated data can be null.</summary>
        /// <param name="schemaType">The schema type.</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(SchemaType schemaType)
        {
            if (IsNullableRaw == true)
            {
                return true;
            }

            if (IsEnumeration && Enumeration.Contains(null))
            {
                return true;
            }

            if (Type.HasFlag(JsonObjectType.Null))
            {
                return true;
            }

            if ((Type == JsonObjectType.None || Type.HasFlag(JsonObjectType.Null)) && OneOf.Any(o => o.IsNullable(schemaType)))
            {
                return true;
            }

            if (ActualSchema != this && ActualSchema.IsNullable(schemaType))
            {
                return true;
            }

            if (ActualTypeSchema != this && ActualTypeSchema.IsNullable(schemaType))
            {
                return true;
            }

            return false;
        }

        /// <summary>Serializes the <see cref="JsonSchema" /> to a JSON string.</summary>
        /// <returns>The JSON string.</returns>
        public string ToJson()
        {
            return ToJson(Formatting.Indented);
        }

        /// <summary>Serializes the <see cref="JsonSchema" /> to a JSON string.</summary>
        /// <param name="formatting">The formatting.</param>
        /// <returns>The JSON string.</returns>
        public string ToJson(Formatting formatting)
        {
            var oldSchema = SchemaVersion;
            SchemaVersion = "http://json-schema.org/draft-04/schema#";

            var json = JsonSchemaSerialization.ToJson(this, SerializationSchemaType, ContractResolver.Value, formatting);

            SchemaVersion = oldSchema;
            return json;
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from sample JSON data.</summary>
        /// <returns>The JSON Schema.</returns>
        public JToken ToSampleJson()
        {
            var generator = new SampleJsonDataGenerator();
            return generator.Generate(this);
        }

        ///// <summary>Serializes the <see cref="JsonSchema4" /> to a JSON string and removes externally loaded schemas.</summary>
        ///// <returns>The JSON string.</returns>
        //[Obsolete("Not ready yet as it has side-effects on the schema.")]
        //public string ToJsonWithExternalReferences()
        //{
        //    // TODO: Copy "this" schema first (high-prio)
        //    var oldSchema = SchemaVersion;
        //    SchemaVersion = "http://json-schema.org/draft-04/schema#";
        //    JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(this, true);
        //    var json = JsonSchemaReferenceUtilities.ConvertPropertyReferences(JsonConvert.SerializeObject(this, Formatting.Indented));
        //    SchemaVersion = oldSchema;
        //    return json;
        //}

        /// <summary>Gets a value indicating whether this schema inherits from the given parent schema.</summary>
        /// <param name="parentSchema">The parent schema.</param>
        /// <returns>true or false.</returns>
        public bool InheritsSchema(JsonSchema parentSchema)
        {
            return parentSchema != null && ActualSchema
                .AllInheritedSchemas.Concat(new List<JsonSchema> { this })
                .Any(s => s.ActualSchema == parentSchema.ActualSchema) == true;
        }

        /// <summary>Validates the given JSON data against this schema.</summary>
        /// <param name="jsonData">The JSON data to validate. </param>
        /// <param name="customValidators">Custom validators to validate the JSON.</param>
        /// <exception cref="JsonReaderException">Could not deserialize the JSON data.</exception>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(string jsonData, params IFormatValidator[] customValidators)
        {
            var validator = new JsonSchemaValidator(customValidators);
            return validator.Validate(jsonData, ActualSchema);
        }

        /// <summary>Validates the given JSON token against this schema.</summary>
        /// <param name="token">The token to validate. </param>
        /// <param name="customValidators">Custom validators to validate the token.</param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(JToken token, params IFormatValidator[] customValidators)
        {
            var validator = new JsonSchemaValidator(customValidators);
            return validator.Validate(token, ActualSchema);
        }

        private static JsonObjectType ConvertStringToJsonObjectType(string value)
        {
            // Section 3.5:
            // http://json-schema.org/latest/json-schema-core.html#anchor8
            // The string must be one of the 7 primitive types

            switch (value)
            {
                case "array":
                    return JsonObjectType.Array;
                case "boolean":
                    return JsonObjectType.Boolean;
                case "integer":
                    return JsonObjectType.Integer;
                case "number":
                    return JsonObjectType.Number;
                case "null":
                    return JsonObjectType.Null;
                case "object":
                    return JsonObjectType.Object;
                case "string":
                    return JsonObjectType.String;
                case "file": // used for NSwag (special Swagger type)
                    return JsonObjectType.File;
                default:
                    return JsonObjectType.None;
            }
        }

        private void Initialize()
        {
            if (Items == null)
            {
                Items = new ObservableCollection<JsonSchema>();
            }

            if (Properties == null)
            {
                Properties = new ObservableDictionary<string, JsonSchemaProperty>();
            }

            if (PatternProperties == null)
            {
                PatternProperties = new ObservableDictionary<string, JsonSchemaProperty>();
            }

            if (Definitions == null)
            {
                Definitions = new ObservableDictionary<string, JsonSchema>();
            }

            if (RequiredProperties == null)
            {
                RequiredProperties = new ObservableCollection<string>();
            }

            if (AllOf == null)
            {
                AllOf = new ObservableCollection<JsonSchema>();
            }

            if (AnyOf == null)
            {
                AnyOf = new ObservableCollection<JsonSchema>();
            }

            if (OneOf == null)
            {
                OneOf = new ObservableCollection<JsonSchema>();
            }

            if (Enumeration == null)
            {
                Enumeration = new Collection<object>();
            }

            if (EnumerationNames == null)
            {
                EnumerationNames = new Collection<string>();
            }
            
            if (EnumerationMetaData == null)
            {
                EnumerationMetaData = new Collection<EnumerationMetaData>();
            }
        }
    }
}