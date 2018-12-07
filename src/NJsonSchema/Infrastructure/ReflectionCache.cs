//-----------------------------------------------------------------------
// <copyright file="ReflectionCache.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides cached reflection APIs for better performance.</summary>
    public static class ReflectionCache
    {
        private static readonly Dictionary<Type, IList<PropertyOrField>> PropertyCacheByType = new Dictionary<Type, IList<PropertyOrField>>();

        private static readonly Dictionary<Type, Attribute> DataContractAttributeCacheByType = new Dictionary<Type, Attribute>();

        /// <summary>Gets the properties and fields of a given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The properties.</returns>
        public static IEnumerable<PropertyOrField> GetPropertiesAndFields(Type type)
        {
            lock (PropertyCacheByType)
            {
                if (!PropertyCacheByType.ContainsKey(type))
                {
#if !LEGACY
                    var declaredProperties = type.GetRuntimeProperties().Where(p => p.GetMethod?.IsStatic != true);
                    var declaredFields = type.GetRuntimeFields().Where(f => f.IsPublic && !f.IsStatic);
#else
                    var declaredProperties = type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetGetMethod(true)?.IsAssembly == true || p.GetGetMethod(true)?.IsPublic == true ||
                                    p.GetSetMethod(true)?.IsAssembly == true || p.GetSetMethod(true)?.IsPublic == true);
                    var declaredFields = type.GetTypeInfo().GetFields(BindingFlags.Instance | BindingFlags.Public);
#endif

                    var properties = declaredProperties.OfType<MemberInfo>().Concat(declaredFields)
                        .Select(p => new PropertyOrField(p, GetCustomAttributes(p))).ToList();

                    PropertyCacheByType[type] = properties;
                }

                return PropertyCacheByType[type];
            }
        }

        private static CustomAttributes GetCustomAttributes(MemberInfo property)
        {
            JsonIgnoreAttribute jsonIgnoreAttribute = null;
            JsonPropertyAttribute jsonPropertyAttribute = null;
            Attribute dataMemberAttribute = null;

            foreach (var attribute in property.GetCustomAttributes(true).OfType<Attribute>())
            {
                if (attribute is JsonIgnoreAttribute)
                    jsonIgnoreAttribute = attribute as JsonIgnoreAttribute;
                else if (attribute is JsonPropertyAttribute)
                    jsonPropertyAttribute = attribute as JsonPropertyAttribute;
                else if (attribute.GetType().Name == "DataMemberAttribute")
                    dataMemberAttribute = attribute;
            }

            return new CustomAttributes(jsonIgnoreAttribute, jsonPropertyAttribute, GetDataContractAttribute(property.DeclaringType), dataMemberAttribute);
        }

        /// <summary>Gets the data contract attribute of a given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The DataContractAttribute.</returns>
        public static Attribute GetDataContractAttribute(Type type)
        {
            lock (DataContractAttributeCacheByType)
            {
                if (!DataContractAttributeCacheByType.ContainsKey(type))
                {
                    var attribute = type.GetTypeInfo().GetCustomAttributes().SingleOrDefault(a => a.GetType().Name == "DataContractAttribute");
                    DataContractAttributeCacheByType[type] = attribute;
                }

                return DataContractAttributeCacheByType[type];
            }
        }

        /// <summary>A property or field.</summary>
        public class PropertyOrField
        {
            /// <summary>Initializes a new instance of the <see cref="PropertyOrField"/> class.</summary>
            /// <param name="memberInfo">The member information.</param>
            /// <param name="customAttributes">The custom attributes.</param>
            public PropertyOrField(MemberInfo memberInfo, CustomAttributes customAttributes)
            {
                MemberInfo = memberInfo;
                CustomAttributes = customAttributes;
            }

            /// <summary>Gets the member information.</summary>
            public MemberInfo MemberInfo { get; }

            /// <summary>Gets the custom attributes.</summary>
            public CustomAttributes CustomAttributes { get; }

            /// <summary>Gets a value indicating whether this instance can read.</summary>
            public bool CanRead => (MemberInfo as PropertyInfo)?.CanRead ?? true;

            /// <summary>Gets a value indicating whether this instance is indexer.</summary>
            public bool IsIndexer => MemberInfo is PropertyInfo && ((PropertyInfo)MemberInfo).GetIndexParameters().Length > 0;

            /// <summary>Gets the value of the property or field.</summary>
            /// <param name="obj">The object.</param>
            /// <returns>The value.</returns>
            public object GetValue(object obj)
            {
                if (MemberInfo is PropertyInfo)
                    return ((PropertyInfo)MemberInfo).GetValue(obj);
                else
                    return ((FieldInfo)MemberInfo).GetValue(obj);
            }

            /// <summary>Gets the value of the property or field.</summary>
            /// <param name="obj">The object.</param>
            /// <param name="value">The value.</param>
            /// <returns>The value.</returns>
            public void SetValue(object obj, object value)
            {
                if (MemberInfo is PropertyInfo)
                    ((PropertyInfo)MemberInfo).SetValue(obj, value);
                else
                    ((FieldInfo)MemberInfo).SetValue(obj, value);
            }

            /// <summary>Gets the name of the property for JSON serialization.</summary>
            /// <returns>The name.</returns>
            public string GetName()
            {
                if (CustomAttributes.JsonPropertyAttribute != null && !string.IsNullOrEmpty(CustomAttributes.JsonPropertyAttribute.PropertyName))
                    return CustomAttributes.JsonPropertyAttribute.PropertyName;

                if (CustomAttributes.DataContractAttribute != null)
                {
                    if (CustomAttributes.DataMemberAttribute != null && !string.IsNullOrEmpty(CustomAttributes.DataMemberAttribute.Name))
                        return CustomAttributes.DataMemberAttribute.Name;
                }

                return MemberInfo.Name;
            }
        }

        /// <summary>The custom attributes.</summary>
        public class CustomAttributes
        {
            /// <summary>Initializes a new instance of the <see cref="CustomAttributes"/> class.</summary>
            /// <param name="jsonIgnoreAttribute">The json ignore attribute.</param>
            /// <param name="jsonPropertyAttribute">The json property attribute.</param>
            /// <param name="dataContractAttribute">The data contract attribute.</param>
            /// <param name="dataMemberAttribute">The data member attribute.</param>
            public CustomAttributes(
                JsonIgnoreAttribute jsonIgnoreAttribute,
                JsonPropertyAttribute jsonPropertyAttribute,
                Attribute dataContractAttribute,
                Attribute dataMemberAttribute)
            {
                JsonIgnoreAttribute = jsonIgnoreAttribute;
                JsonPropertyAttribute = jsonPropertyAttribute;
                DataContractAttribute = dataContractAttribute;
                DataMemberAttribute = dataMemberAttribute;
            }

            /// <summary>Gets the json ignore attribute.</summary>
            public JsonIgnoreAttribute JsonIgnoreAttribute { get; }

            /// <summary>Gets the json property attribute.</summary>
            public JsonPropertyAttribute JsonPropertyAttribute { get; }

            /// <summary>Gets the data contract attribute.</summary>
            public Attribute DataContractAttribute { get; }

            /// <summary>Gets the data member attribute.</summary>
            public dynamic DataMemberAttribute { get; }
        }
    }
}