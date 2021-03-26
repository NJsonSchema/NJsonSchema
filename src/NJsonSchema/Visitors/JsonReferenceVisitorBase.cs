//-----------------------------------------------------------------------
// <copyright file="JsonReferenceVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema"/> objects.</summary>
    public abstract class JsonReferenceVisitorBase
    {
        private readonly IContractResolver _contractResolver;

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceVisitorBase"/> class. </summary>
        protected JsonReferenceVisitorBase()
            : this(new DefaultContractResolver())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceVisitorBase"/> class. </summary>
        /// <param name="contractResolver">The contract resolver.</param>
        protected JsonReferenceVisitorBase(IContractResolver contractResolver)
        {
            _contractResolver = contractResolver;
        }

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <returns>The task.</returns>
        public virtual void Visit(object obj)
        {
            Visit(obj, "#", null, new HashSet<object>(), o => throw new NotSupportedException("Cannot replace the root."));
        }

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected abstract IJsonReference VisitJsonReference(IJsonReference reference, string path, string typeNameHint);

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <param name="path">The path</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="checkedObjects">The checked objects.</param>
        /// <param name="replacer">The replacer.</param>
        /// <returns>The task.</returns>
        protected virtual void Visit(object obj, string path, string typeNameHint, ISet<object> checkedObjects, Action<object> replacer)
        {
            if (obj == null || checkedObjects.Contains(obj))
            {
                return;
            }

            checkedObjects.Add(obj);

            if (obj is IJsonReference reference)
            {
                var newReference = VisitJsonReference(reference, path, typeNameHint);
                if (newReference != reference)
                {
                    replacer(newReference);
                    return;
                }
            }

            if (obj is JsonSchema schema)
            {
                if (schema.Reference != null)
                {
                    Visit(schema.Reference, path, null, checkedObjects, o => schema.Reference = (JsonSchema)o);
                }

                if (schema.AdditionalItemsSchema != null)
                {
                    Visit(schema.AdditionalItemsSchema, path + "/additionalItems", null, checkedObjects, o => schema.AdditionalItemsSchema = (JsonSchema)o);
                }

                if (schema.AdditionalPropertiesSchema != null)
                {
                    Visit(schema.AdditionalPropertiesSchema, path + "/additionalProperties", null, checkedObjects, o => schema.AdditionalPropertiesSchema = (JsonSchema)o);
                }

                if (schema.Item != null)
                {
                    Visit(schema.Item, path + "/items", null, checkedObjects, o => schema.Item = (JsonSchema)o);
                }

                for (var i = 0; i < schema.Items.Count; i++)
                {
                    var index = i;
                    Visit(schema.Items.ElementAt(i), path + "/items[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.Items, index, (JsonSchema)o));
                }

                for (var i = 0; i < schema.AllOf.Count; i++)
                {
                    var index = i;
                    Visit(schema.AllOf.ElementAt(i), path + "/allOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.AllOf, index, (JsonSchema)o));
                }

                for (var i = 0; i < schema.AnyOf.Count; i++)
                {
                    var index = i;
                    Visit(schema.AnyOf.ElementAt(i), path + "/anyOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.AnyOf, index, (JsonSchema)o));
                }

                for (var i = 0; i < schema._oneOf.Count; i++)
                {
                    var index = i;
                    Visit(schema._oneOf.ElementAt(i), path + "/oneOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema._oneOf, index, (JsonSchema)o));
                }

                if (schema.Not != null)
                {
                    Visit(schema.Not, path + "/not", null, checkedObjects, o => schema.Not = (JsonSchema)o);
                }

                if (schema.DictionaryKey != null)
                {
                    Visit(schema.DictionaryKey, path + "/x-dictionaryKey", null, checkedObjects, o => schema.DictionaryKey = (JsonSchema)o);
                }

                if (schema.DiscriminatorRaw != null)
                {
                    Visit(schema.DiscriminatorRaw, path + "/discriminator", null, checkedObjects, o => schema.DiscriminatorRaw = o);
                }

                foreach (var p in schema.Properties.ToArray())
                {
                    Visit(p.Value, path + "/properties/" + p.Key, p.Key, checkedObjects, o => schema.Properties[p.Key] = (JsonSchemaProperty)o);
                }

                foreach (var p in schema.PatternProperties.ToArray())
                {
                    Visit(p.Value, path + "/patternProperties/" + p.Key, null, checkedObjects, o => schema.PatternProperties[p.Key] = (JsonSchemaProperty)o);
                }

                foreach (var p in schema.Definitions.ToArray())
                {
                    Visit(p.Value, path + "/definitions/" + p.Key, p.Key, checkedObjects, o =>
                    {
                        if (o != null)
                        {
                            schema.Definitions[p.Key] = (JsonSchema)o;
                        }
                        else
                        {
                            schema.Definitions.Remove(p.Key);
                        }
                    });
                }
            }

            if (!(obj is string) && !(obj is JToken) && obj.GetType() != typeof(JsonSchema)) // Reflection fallback
            {
                if (_contractResolver.ResolveContract(obj.GetType()) is JsonObjectContract contract)
                {
                    foreach (var property in contract.Properties.Where(p =>
                    {
                        bool isJsonSchemaProperty = obj is JsonSchema && JsonSchema.JsonSchemaPropertiesCache.Contains(p.UnderlyingName);
                        return !isJsonSchemaProperty && !p.Ignored &&
                                p.ShouldSerialize?.Invoke(obj) != false;
                    }))
                    {
                        var value = property.ValueProvider.GetValue(obj);
                        if (value != null)
                        {
                            Visit(value, path + "/" + property.PropertyName, property.PropertyName, checkedObjects, o => property.ValueProvider.SetValue(obj, o));
                        }
                    }
                }
                else if (obj is IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys.OfType<object>().ToArray())
                    {
                        Visit(dictionary[key], path + "/" + key, key.ToString(), checkedObjects, o =>
                        {
                            if (o != null)
                            {
                                dictionary[key] = (JsonSchema)o;
                            }
                            else
                            {
                                dictionary.Remove(key);
                            }
                        });
                    }

                    // Custom dictionary type with additional properties (OpenApiPathItem)
                    var contextualType = obj.GetType().ToContextualType();
                    if (contextualType.TypeAttributes.OfType<JsonConverterAttribute>().Any())
                    {
                        foreach (var property in contextualType.Type.GetContextualProperties()
                            .Where(p => p.MemberInfo.DeclaringType == contextualType.Type &&
                                        !p.ContextAttributes.OfType<JsonIgnoreAttribute>().Any()))
                        {
                            var value = property.GetValue(obj);
                            if (value != null)
                            {
                                Visit(value, path + "/" + property.Name, property.Name, checkedObjects, o => property.SetValue(obj, o));
                            }
                        }
                    }
                }
                else if (obj is IList list)
                {
                    var items = list.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        var index = i;
                        Visit(items[i], path + "[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(list, index, o));
                    }
                }
                else if (obj is IEnumerable enumerable)
                {
                    var items = enumerable.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        Visit(items[i], path + "[" + i + "]", null, checkedObjects, o => throw new NotSupportedException("Cannot replace enumerable item."));
                    }
                }
            }
        }

        private void ReplaceOrDelete<T>(ICollection<T> collection, int index, T obj)
        {
            ((Collection<T>)collection).RemoveAt(index);
            if (obj != null)
            {
                ((Collection<T>)collection).Insert(index, obj);
            }
        }

        private void ReplaceOrDelete(IList collection, int index, object obj)
        {
            collection.RemoveAt(index);
            if (obj != null)
            {
                collection.Insert(index, obj);
            }
        }
    }
}
