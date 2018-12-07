//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    [JsonConverter(typeof(ExtensionDataDeserializationConverter))]
    public partial class JsonSchema4 : IJsonExtensionObject
    {
        /// <summary>Creates the serializer contract resolver based on the <see cref="SchemaType"/>.</summary>
        /// <param name="schemaType">The schema type.</param>
        /// <returns>The settings.</returns>
        public static PropertyRenameAndIgnoreSerializerContractResolver CreateJsonSerializerContractResolver(SchemaType schemaType)
        {
            var resolver = new IgnoreEmptyCollectionsContractResolver();

            if (schemaType == SchemaType.OpenApi3)
            {
                resolver.RenameProperty(typeof(JsonProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonProperty), "x-writeOnly", "writeOnly");

                resolver.RenameProperty(typeof(JsonSchema4), "x-nullable", "nullable");
                resolver.RenameProperty(typeof(JsonSchema4), "x-example", "example");
            }
            else if (schemaType == SchemaType.Swagger2)
            {
                resolver.RenameProperty(typeof(JsonProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonSchema4), "x-example", "example");
            }
            else
            {
                resolver.RenameProperty(typeof(JsonProperty), "x-readOnly", "readonly");
            }

            return resolver;
        }

        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by JSON Schema).</summary>
        [JsonExtensionData]
        public IDictionary<string, object> ExtensionData { get; set; }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            Initialize();
        }

        /// <summary>Gets or sets the discriminator property (Swagger only).</summary>
        [JsonIgnore]
        public string Discriminator
        {
            get => DiscriminatorObject?.PropertyName;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DiscriminatorObject = new OpenApiDiscriminator
                    {
                        PropertyName = value
                    };
                }
                else
                    DiscriminatorObject = null;
            }
        }

        /// <summary>Gets or sets the discriminator (OpenApi only).</summary>
        [JsonIgnore]
        public OpenApiDiscriminator DiscriminatorObject { get; set; }

        /// <summary>Gets or sets the discriminator.</summary>
        [JsonProperty("discriminator", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 5)]
        internal object DiscriminatorRaw
        {
            get
            {
                if (JsonSchemaSerialization.CurrentSchemaType != SchemaType.Swagger2)
                    return DiscriminatorObject;
                else
                    return Discriminator;
            }
            set
            {
                if (value is String)
                    Discriminator = (string)value;
                else if (value != null)
                    DiscriminatorObject = ((JObject)value).ToObject<OpenApiDiscriminator>();
            }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonIgnore]
        public Collection<string> EnumerationNames { get; set; }

        /// <summary>Gets or sets a value indicating whether the maximum value is excluded. </summary>
        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object ExclusiveMaximumRaw
        {
            get { return ExclusiveMaximum ?? (IsExclusiveMaximum ? (object)true : null); }
            set
            {
                if (value is bool)
                    IsExclusiveMaximum = (bool)value;
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                    IsExclusiveMaximum = value.Equals("true");
                else if (value != null)
                    ExclusiveMaximum = Convert.ToDecimal(value);
            }
        }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded. </summary>
        [JsonProperty("exclusiveMinimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object ExclusiveMinimumRaw
        {
            get { return ExclusiveMinimum ?? (IsExclusiveMinimum ? (object)true : null); }
            set
            {
                if (value is bool)
                    IsExclusiveMinimum = (bool)value;
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                    IsExclusiveMinimum = value.Equals("true");
                else if (value != null)
                    ExclusiveMinimum = Convert.ToDecimal(value);
            }
        }

        [JsonProperty("additionalItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object AdditionalItemsRaw
        {
            get
            {
                if (AdditionalItemsSchema != null)
                    return AdditionalItemsSchema;
                if (!AllowAdditionalItems)
                    return false;
                return null;
            }
            set
            {
                if (value is bool)
                    AllowAdditionalItems = (bool)value;
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                    AllowAdditionalItems = value.Equals("true");
                else if (value != null)
                    AdditionalItemsSchema = FromJsonWithoutReferenceHandling(value.ToString());
            }
        }

        [JsonProperty("additionalProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object AdditionalPropertiesRaw
        {
            get
            {
                if (AdditionalPropertiesSchema != null)
                    return AdditionalPropertiesSchema;
                if (!AllowAdditionalProperties)
                    return false;
                return null;
            }
            set
            {
                if (value is bool)
                    AllowAdditionalProperties = (bool)value;
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                    AllowAdditionalProperties = value.Equals("true");
                else if (value != null)
                    AdditionalPropertiesSchema = FromJsonWithoutReferenceHandling(value.ToString());
            }
        }

        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object ItemsRaw
        {
            get
            {
                if (Item != null)
                    return Item;
                if (Items.Count > 0)
                    return Items;
                return null;
            }
            set
            {
                if (value is JArray)
                    Items = new ObservableCollection<JsonSchema4>(((JArray)value).Select(t => FromJsonWithoutReferenceHandling(t.ToString())));
                else if (value != null)
                    Item = FromJsonWithoutReferenceHandling(value.ToString());
            }
        }

        private Lazy<object> _typeRaw;

        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 3)]
        internal object TypeRaw
        {
            get
            {
                if (_typeRaw == null)
                    ResetTypeRaw();

                return _typeRaw.Value;
            }
            set
            {
                if (value is JArray)
                    Type = ((JArray)value).Aggregate(JsonObjectType.None, (type, token) => type | ConvertStringToJsonObjectType(token.ToString()));
                else
                    Type = ConvertStringToJsonObjectType(value as string);

                ResetTypeRaw();
            }
        }

        private void ResetTypeRaw()
        {
            _typeRaw = new Lazy<object>(() =>
            {
                var flags = Enum.GetValues(Type.GetType())
                    .OfType<JsonObjectType>()
                    .Where(v => Type.HasFlag(v))
                    .Where(v => v != JsonObjectType.None)
                    .ToArray();

                if (flags.Length > 1)
                    return new JArray(flags.Select(f => new JValue(f.ToString().ToLowerInvariant())));

                if (flags.Length == 1)
                    return new JValue(flags[0].ToString().ToLowerInvariant());

                return null;
            });
        }

        [JsonProperty("required", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<string> RequiredPropertiesRaw
        {
            get { return RequiredProperties != null && RequiredProperties.Count > 0 ? RequiredProperties : null; }
            set { RequiredProperties = value; }
        }

        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> PropertiesRaw
        {
            get
            {
                return Properties != null && Properties.Count > 0 ?
                    Properties.ToDictionary(p => p.Key, p => (JsonSchema4)p.Value) : null;
            }
            set
            {
                Properties = value != null ?
                    new ObservableDictionary<string, JsonProperty>(value.ToDictionary(p => p.Key, p => JsonProperty.FromJsonSchema(p.Key, p.Value))) :
                    new ObservableDictionary<string, JsonProperty>();
            }
        }

        [JsonProperty("patternProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> PatternPropertiesRaw
        {
            get
            {
                return PatternProperties != null && PatternProperties.Count > 0 ?
                    PatternProperties.ToDictionary(p => p.Key, p => (JsonSchema4)p.Value) : null;
            }
            set
            {
                PatternProperties = value != null ?
                    new ObservableDictionary<string, JsonSchema4>(value.ToDictionary(p => p.Key, p => p.Value)) :
                    new ObservableDictionary<string, JsonSchema4>();
            }
        }

        [JsonProperty("definitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> DefinitionsRaw
        {
            get { return Definitions != null && Definitions.Count > 0 ? Definitions : null; }
            set { Definitions = value != null ? new ObservableDictionary<string, JsonSchema4>(value) : new ObservableDictionary<string, JsonSchema4>(); }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonProperty("x-enumNames", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Collection<string> EnumerationNamesRaw
        {
            get { return EnumerationNames != null && EnumerationNames.Count > 0 ? EnumerationNames : null; }
            set { EnumerationNames = value != null ? new ObservableCollection<string>(value) : new ObservableCollection<string>(); }
        }

        [JsonProperty("enum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<object> EnumerationRaw
        {
            get { return Enumeration != null && Enumeration.Count > 0 ? Enumeration : null; }
            set { Enumeration = value != null ? new ObservableCollection<object>(value) : new ObservableCollection<object>(); }
        }

        [JsonProperty("allOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> AllOfRaw
        {
            get { return AllOf != null && AllOf.Count > 0 ? AllOf : null; }
            set { AllOf = value != null ? new ObservableCollection<JsonSchema4>(value) : new ObservableCollection<JsonSchema4>(); }
        }

        [JsonProperty("anyOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> AnyOfRaw
        {
            get { return AnyOf != null && AnyOf.Count > 0 ? AnyOf : null; }
            set { AnyOf = value != null ? new ObservableCollection<JsonSchema4>(value) : new ObservableCollection<JsonSchema4>(); }
        }

        [JsonProperty("oneOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> OneOfRaw
        {
            get { return OneOf != null && OneOf.Count > 0 ? OneOf : null; }
            set { OneOf = value != null ? new ObservableCollection<JsonSchema4>(value) : new ObservableCollection<JsonSchema4>(); }
        }

        private void RegisterProperties(IDictionary<string, JsonProperty> oldCollection, IDictionary<string, JsonProperty> newCollection)
        {
            if (oldCollection != null)
                ((ObservableDictionary<string, JsonProperty>)oldCollection).CollectionChanged -= InitializeSchemaCollection;

            if (newCollection != null)
            {
                ((ObservableDictionary<string, JsonProperty>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaDictionary(IDictionary<string, JsonSchema4> oldCollection, IDictionary<string, JsonSchema4> newCollection)
        {
            if (oldCollection != null)
                ((ObservableDictionary<string, JsonSchema4>)oldCollection).CollectionChanged -= InitializeSchemaCollection;

            if (newCollection != null)
            {
                ((ObservableDictionary<string, JsonSchema4>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaCollection(ICollection<JsonSchema4> oldCollection, ICollection<JsonSchema4> newCollection)
        {
            if (oldCollection != null)
                ((ObservableCollection<JsonSchema4>)oldCollection).CollectionChanged -= InitializeSchemaCollection;

            if (newCollection != null)
            {
                ((ObservableCollection<JsonSchema4>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void InitializeSchemaCollection(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableDictionary<string, JsonProperty>)
            {
                var properties = (ObservableDictionary<string, JsonProperty>)sender;
                foreach (var property in properties)
                {
                    property.Value.Name = property.Key;
                    property.Value.Parent = this;
                }
            }
            else if (sender is ObservableCollection<JsonSchema4>)
            {
                var collection = (ObservableCollection<JsonSchema4>)sender;
                foreach (var item in collection)
                    item.Parent = this;
            }
            else if (sender is ObservableDictionary<string, JsonSchema4>)
            {
                var collection = (ObservableDictionary<string, JsonSchema4>)sender;
                foreach (var item in collection.Values)
                    item.Parent = this;
            }
        }
    }
}
